#include "Battery1000.hpp"

namespace LGSTrayHID {
	using namespace Battery;

	std::tuple<int, Power_supply_status> LGSTrayHID::Battery1000_parse(const HIDPPMsg_20& msg) {
		using enum Power_supply_status;

		int capacity = msg.get_result_data()[0];
		Power_supply_status status = POWER_SUPPLY_STATUS_UNKNOWN;

		switch (msg.get_result_data()[2]) {
		case 0: /* discharging (in use) */
			status = POWER_SUPPLY_STATUS_DISCHARGING;
			break;
		case 1: /* recharging */
			status = POWER_SUPPLY_STATUS_CHARGING;
			break;
		case 2: /* charge in final stage */
			status = POWER_SUPPLY_STATUS_CHARGING;
			break;
		case 3: /* charge complete */
			status = POWER_SUPPLY_STATUS_FULL;
			break;
		case 4: /* recharging below optimal speed */
			status = POWER_SUPPLY_STATUS_CHARGING;
			break;
		default:
			status = POWER_SUPPLY_STATUS_NOT_CHARGING;
			break;
		}

		return { capacity, status };
	}
}