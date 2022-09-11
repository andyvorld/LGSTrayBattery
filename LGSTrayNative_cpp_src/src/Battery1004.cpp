#include "Battery1004.hpp"

namespace LGSTrayHID {
	using namespace Battery;

	std::tuple<int, Power_supply_status> Battery1004_parse(const HIDPPMsg_20& msg) {
		using enum Power_supply_status;

		int capacity = msg.get_result_data()[0];
		Power_supply_status status = POWER_SUPPLY_STATUS_UNKNOWN;

		switch (msg.get_result_data()[2]) {
		case 0: /* discharging */
			status = POWER_SUPPLY_STATUS_DISCHARGING;
			break;
		case 1: /* charging */
		case 2: /* charging slow */
			status = POWER_SUPPLY_STATUS_CHARGING;
			break;
		case 3: /* complete */
			status = POWER_SUPPLY_STATUS_FULL;
			break;
		default:
			status = POWER_SUPPLY_STATUS_NOT_CHARGING;
			break;
		}

		return { capacity, status };
	}
}