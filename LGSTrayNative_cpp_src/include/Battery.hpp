#pragma once

namespace LGSTrayHID {
	namespace Battery {
		enum class Power_supply_status : uint8_t {
			POWER_SUPPLY_STATUS_CHARGING,
			POWER_SUPPLY_STATUS_FULL,
			POWER_SUPPLY_STATUS_NOT_CHARGING,
			POWER_SUPPLY_STATUS_DISCHARGING,
			POWER_SUPPLY_STATUS_UNKNOWN
		};
	}
}