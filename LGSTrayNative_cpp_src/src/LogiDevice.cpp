#include "LogiDevice.hpp"

#include "HIDPPMsg.hpp"
#include <iostream>

#include <thread>
#include <chrono>

#include "MapWrapper.hpp"

#include "Battery1000.hpp"
#include "Battery1001.hpp"
#include "Battery1004.hpp"

using namespace std::placeholders;

namespace LGSTrayHID {
	void LogiDevice::check_proto_ver_cb(std::unique_ptr<uint8_t[]> buf)
	{
		HIDPPMsg_20 msg(std::move(buf));

		if (msg.get_feature_index() != 0x00) {
			std::cerr << "Device is not HID++ 2.0" << std::endl;
			return;
		}

		std::cout << "Device is HID++ 2.0+" << std::endl;


		this->_write_cb = [this](std::unique_ptr<uint8_t[]> buf) { this->feature_set_cb(std::move(buf)); };

		uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, 0x00, 0x00 | SW_ID, 0x00, 0x05, 0x00 };
		hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
	}

	void LogiDevice::feature_set_cb(std::unique_ptr<uint8_t[]> buf)
	{
		HIDPPMsg_20 msg(std::move(buf));

		if (msg.get_feature_index() != 0x00) {
			// Not IRoot feature index ignore
			return;
		}

		if (msg.get_sw_id() != SW_ID) {
			// Ignore if not tagged by SW_ID
			return;
		}

		if (_device_name_idx == 0) {
			_device_name_idx = msg.get_result_data()[0];

			this->_write_cb = [this](std::unique_ptr<uint8_t[]> buf) { this->device_name_cb(std::move(buf)); };

			uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, _device_name_idx, 0x00 | SW_ID, 0x00, 0x05, 0x00 };
			hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
		}
		else if (_battery_step == 0) {
			_battery_1000_idx = msg.get_result_data()[0];

			if (_battery_1000_idx == 0) {
				_battery_step++;

				uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, 0x00, 0x00 | SW_ID, 0x10, 0x01, 0x00 };
				hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
			}
			else {
				_ready = true;
			}
		}
		else if (_battery_step == 1) {
			_battery_1001_idx = msg.get_result_data()[0];

			if (_battery_1001_idx == 0) {
				_battery_step++;

				uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, 0x00, 0x00 | SW_ID, 0x10, 0x01, 0x00 };
				hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
			}
			else {
				_ready = true;
			}
		}
		else if (_battery_step == 2) {
			_battery_1004_idx = msg.get_result_data()[0];

			if (_battery_1004_idx == 0) {
				std::cerr << "No Battery info" << std::endl;

				this->_write_cb = nullptr;
			}
			else {
				_ready = true;
			}
		}

		if (_ready) {
			std::cout << device_name << std::endl;
			std::cout << (int) device_type << std::endl;
			std::cout << (int)_battery_1000_idx << std::endl;
			std::cout << (int)_battery_1001_idx << std::endl;
			std::cout << (int)_battery_1004_idx << std::endl;

			this->_write_cb = [this](std::unique_ptr<uint8_t[]> buf) { this->battery_status_cb(std::move(buf)); };

			MapWrapper::add_LogiDevice(this->dev_id, this->shared_from_this());

			this->update_battery();

			if (device_ready_cb) {
				device_ready_cb(*this);
			}

			//std::cout << "{" << MapWrapper::to_json() << "}" << std::endl;
		}
	}

	void LogiDevice::device_name_cb(std::unique_ptr<uint8_t[]> buf) {
		HIDPPMsg_20 msg(std::move(buf));

		if (msg.get_feature_index() != _device_name_idx) {
			// Ignore if not device name response
			return;
		}

		if (msg.get_sw_id() != SW_ID) {
			// Ignore if not tagged by SW_ID
			return;
		}

		if (_device_name_step == 0) {
			_device_name_count = msg.get_result_data()[0];
			_device_name_step++;

			uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, _device_name_idx, 0x10 | SW_ID, 0x00, 0x00, 0x00 };
			hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
		}
		else if (_device_name_step == 1) {
			bool device_name_fin = false;
			for (size_t i = 0; i < 16; i++) {
				device_name.push_back(msg.get_result_data()[i]);

				if (device_name.length() == _device_name_count) {
					device_name_fin = true;
					break;
				}
			}

			if (!device_name_fin) {
				uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, _device_name_idx, 0x10 | SW_ID, (uint8_t) device_name.length(), 0x00, 0x00 };
				hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
			}
			else {
				_device_name_step++;

				uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, _device_name_idx, 0x20 | SW_ID, 0x00, 0x00, 0x00 };
				hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
			}
		}
		else if (_device_name_step == 2) {
			device_type = static_cast<LogiDeviceType>(msg.get_result_data()[0]);

			this->_write_cb = [this](std::unique_ptr<uint8_t[]> buf) { this->feature_set_cb(std::move(buf)); };

			uint8_t wbuf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, 0x00, 0x00 | SW_ID, 0x10, 0x00, 0x00 };
			hid_write(_long_dev.get(), wbuf, HIDPP_LONG_SIZE);
		}
	}

	void LogiDevice::battery_status_cb(std::unique_ptr<uint8_t[]> buf) {
		HIDPPMsg_20 msg(std::move(buf));

		int battery_voltage = -1;
		int battery_percentage = -1;
		Battery::Power_supply_status battery_status = Battery::Power_supply_status::POWER_SUPPLY_STATUS_UNKNOWN;

		if ((_battery_1000_idx > 0) && (msg.get_feature_index() == _battery_1000_idx)) {
			std::tie(battery_percentage, battery_status) = Battery1000_parse(msg);
		}
		else if ((_battery_1001_idx > 0) && (msg.get_feature_index() == _battery_1001_idx)) {
			std::tie(battery_voltage, battery_percentage, battery_status) = Battery1001_parse(msg);

			std::cout << (double)battery_percentage << std::endl;
			std::cout << (double)battery_status << std::endl;
		}
		else if ((_battery_1004_idx > 0) && (msg.get_feature_index() == _battery_1004_idx)) {
			std::tie(battery_percentage, battery_status) = Battery1004_parse(msg);
		}
		else {
			// STUB: Battery protocol not known
			return;
		}

		bool _updated = false;

		if (this->battery_percentage != battery_percentage) {
			this->battery_percentage = battery_percentage;
			_updated = true;
		}

		if (this->battery_status != battery_status) {
			this->battery_status = battery_status;
			_updated = true;
		}

		if (this->battery_voltage != battery_voltage) {
			this->battery_voltage = battery_voltage;
			_updated = true;
		}

		_updated = true;
		if (_updated && battery_update_cb) {
			battery_update_cb(*this);
		}
	}

	LogiDevice::LogiDevice(uint8_t dev_idx, std::string container_name, const std::shared_ptr<hid_device>& short_dev, const std::shared_ptr<hid_device>& long_dev) :
		dev_idx(dev_idx), _short_dev(short_dev), _long_dev(long_dev), dev_id(gen_dev_id(dev_idx, container_name)) {
		this->_write_cb = [this](std::unique_ptr<uint8_t[]> buf) { this->check_proto_ver_cb(std::move(buf)); };
	}

	std::string LogiDevice::gen_dev_id(uint8_t dev_idx, std::string container_name)
	{
		return "dev" + container_name + std::to_string(dev_idx);
	}

	std::shared_ptr<LogiDevice> LogiDevice::make_shared(uint8_t dev_idx, std::string container_name, const std::shared_ptr<hid_device>& short_dev, const std::shared_ptr<hid_device>& long_dev)
	{
		return std::shared_ptr<LogiDevice>(new LogiDevice(dev_idx, container_name, short_dev, long_dev));
	}

	void LogiDevice::battery_summary() const
	{
		//{
		//	{"deviceId", this->dev_id},
		//	{ "percentage", this->battery_percentage.load() },
		//	{ "charging", this->battery_status.load() == LGSTrayHID::Battery::Power_supply_status::POWER_SUPPLY_STATUS_CHARGING },
		//	{ "mileage", -1.0 },
		//	{ "voltage", this->battery_voltage.load() }
		//}
		return;
	}

	void LogiDevice::device_summary() const
	{
		//nlohmann::json ret =
		//{
		//	{"deviceType", LogiDeviceTypeEnumToString(this->device_type)},
		//	{"id", this->dev_id},
		//	{"extendedDisplayName", this->device_name}
		//};
		//ret["capabilities"]["hasBatteryStatus"] = true;

		return;
	}

	void LogiDevice::Register_battery_update_cb(Battery_update_cb cb)
	{
		battery_update_cb = cb;
	}

	void LogiDevice::Register_device_ready_cb(Device_ready_cb cb) {
		device_ready_cb = cb;
	}

	void LogiDevice::update_battery()
	{
		uint8_t battery_idx = _battery_1000_idx | _battery_1001_idx | _battery_1004_idx;

		if (battery_idx == 0) {
			// STUB: Battery protocol not known
			return;
		}

		uint8_t buf[HIDPP_LONG_SIZE] = { HIDPP_LONG, dev_idx, battery_idx,  0x00 | SW_ID };
		hid_write(this->_long_dev.get(), buf, HIDPP_LONG_SIZE);
	}

	void LogiDevice::invoke_response(std::unique_ptr<uint8_t[]> buf)
	{
		if (_write_cb) {
			_write_cb(std::move(buf));
			//std::this_thread::sleep_for(std::chrono::milliseconds(10));
		}
	}
}