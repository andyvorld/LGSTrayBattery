#pragma once

#include <string>
#include <memory>
#include <functional>

#include <hidapi.h>
#include "hid_io_debug.hpp"

#include "Battery.hpp"

namespace LGSTrayHID {
	enum class LogiDeviceType : uint8_t {
		Keyboard = 0,
		RemoteControl,
		Numpad,
		Mouse,
		Touchpad,
		Trackball,
		Presenter,
		Receiver
	};		

	inline const char* LogiDeviceTypeEnumToString(LogiDeviceType type) {
		using enum LogiDeviceType;

		switch (type)
		{
		case Keyboard:
			return "KEYBOARD";
		case RemoteControl:
			return "REMOTECONTROL";
		case Numpad:
			return "NUMPAD";
		case Mouse:
			return "MOUSE";
		case Touchpad:
			return "TOUCHPAD";
		case Trackball:
			return "TRACKBALL";
		case Presenter:
			return "PRESENTER";
		case Receiver:
			return "RECEIVER";
		default:
			return "MOUSE";
		}
	}

	class LogiDevice : public std::enable_shared_from_this<LogiDevice> {
	public:
		typedef std::function<void(const LogiDevice&)> Battery_update_cb;
		typedef std::function<void(const LogiDevice&)> Device_ready_cb;

	private:
		uint8_t dev_idx = 0;

		std::shared_ptr<hid_device> _short_dev;
		std::shared_ptr<hid_device> _long_dev;

		uint8_t _device_name_idx = 0;
		uint8_t _device_name_step = 0;
		uint8_t _device_name_count = 0;

		uint8_t _battery_step = 0;
		uint8_t _battery_1000_idx = 0;
		uint8_t _battery_1001_idx = 0;
		uint8_t _battery_1004_idx = 0;

		bool _ready = false;

		std::function<void(std::unique_ptr<uint8_t[]>)> _write_cb;

		LogiDevice(uint8_t dev_idx, std::string container_name, const std::shared_ptr<hid_device>& short_dev, const std::shared_ptr<hid_device>& long_dev);
		
		void check_proto_ver_cb(std::unique_ptr<uint8_t[]> buf);
		void feature_set_cb(std::unique_ptr<uint8_t[]> buf);
		void device_name_cb(std::unique_ptr<uint8_t[]> buf);
		void battery_status_cb(std::unique_ptr<uint8_t[]> buf);

		static inline Battery_update_cb battery_update_cb;
		static inline Device_ready_cb device_ready_cb;
		static std::string gen_dev_id(uint8_t dev_idx, std::string container_name);

	public:
		std::string device_name;
		const std::string dev_id;
		LogiDeviceType device_type;

		std::atomic<int> battery_voltage = -1;
		std::atomic<int> battery_percentage = -1;
		std::atomic<Battery::Power_supply_status> battery_status;

		[[nodiscard]] static std::shared_ptr<LogiDevice> make_shared(uint8_t dev_idx, std::string container_name, const std::shared_ptr<hid_device>& short_dev, const std::shared_ptr<hid_device>& long_dev);
		~LogiDevice() = default;

		void battery_summary() const;
		void device_summary() const;

		static void Register_battery_update_cb(Battery_update_cb cb);
		static void Register_device_ready_cb(Device_ready_cb cb);

		void update_battery();
		void invoke_response(std::unique_ptr<uint8_t[]> buf);
	};
}