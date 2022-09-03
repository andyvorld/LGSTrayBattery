#pragma once

#include <tuple>

#include "HIDPPMsg.hpp"
#include "Battery.hpp"

namespace LGSTrayHID {
	std::tuple<int, Battery::Power_supply_status> Battery1000_parse(const HIDPPMsg_20& msg);
}