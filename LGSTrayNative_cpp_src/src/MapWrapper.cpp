#include "MapWrapper.hpp"

#include <sstream>

#include <mutex>
#include <map>

namespace LGSTrayHID {
	namespace MapWrapper {
		std::mutex logiDevice_map_mutex;
		std::map<KeyType, std::weak_ptr<LogiDevice>> logiDevice_map;

		void add_LogiDevice(KeyType key, const std::shared_ptr<LogiDevice>& logiDevice) {
			const std::lock_guard<std::mutex> lock(logiDevice_map_mutex);
			logiDevice_map[key] = logiDevice;
		}

		void update_device_battery(KeyType key) {
			auto entry = logiDevice_map.find(key);

			if (entry != logiDevice_map.end()) {
				const std::shared_ptr<LogiDevice> dev = entry->second.lock();
				if (dev == nullptr) {
					return;
				}

				dev->update_battery();
			}
		}

		void clear_map() {
			const std::lock_guard<std::mutex> lock(logiDevice_map_mutex);
			logiDevice_map.clear();
		}
	}
}