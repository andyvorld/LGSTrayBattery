#include "Battery1001.hpp"

#include <iostream>
#include <tuple>

namespace LGSTrayHID {
	using namespace Battery;
	
	std::tuple<int, int, Power_supply_status> Battery1001_parse(const HIDPPMsg_20& msg) {
		using enum Power_supply_status;

		int battery_mvolt = (msg.get_result_data()[0] << 8) + (msg.get_result_data()[1]);
		int battery_capacity = Battery1001_mv_to_capacity(battery_mvolt);

		uint8_t flags = msg.get_result_data()[2];

		Power_supply_status status = POWER_SUPPLY_STATUS_UNKNOWN;

		if (flags & 0x80) {
			switch (flags & 0x07) {
			case 0:
				status = POWER_SUPPLY_STATUS_CHARGING;
				break;
			case 1:
				status = POWER_SUPPLY_STATUS_FULL;
				break;
			case 2:
				status = POWER_SUPPLY_STATUS_NOT_CHARGING;
				break;
			default:
				status = POWER_SUPPLY_STATUS_UNKNOWN;
				break;
			}
		}
		else {
			status = POWER_SUPPLY_STATUS_DISCHARGING;
		}

		return { battery_mvolt, battery_capacity, status };
	}
}