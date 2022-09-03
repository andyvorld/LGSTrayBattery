#pragma once

#include <hidapi.h>
#include "hid_io_debug.hpp"

#include <mutex>
#include <memory>
#include <thread>
#include <unordered_map>
#include "LogiDevice.hpp"

namespace LGSTrayHID {
	class HIDDevice {
	private:
		std::atomic_bool cancellationToken = false;

		std::shared_ptr<hid_device> _short_dev = NULL;
		std::shared_ptr<hid_device> _long_dev = NULL;

		std::unique_ptr<std::thread> _short_reader = NULL;
		std::unique_ptr<std::thread> _long_reader = NULL;

		void _check_if_ready();
	public:
		std::mutex devices_map_mutex;
		std::unordered_map<int, std::shared_ptr<LogiDevice>> devices;
		const std::string container_name;

		HIDDevice(std::string container_name);
		~HIDDevice();

		void assign_short(std::shared_ptr<hid_device> short_dev);
		void assign_long(std::shared_ptr<hid_device> short_dev);
	};
}