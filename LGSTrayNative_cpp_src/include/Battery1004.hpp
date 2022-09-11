#pragma once

#include <tuple>
#include "Battery.hpp"
#include "HIDPPMsg.hpp"

namespace LGSTrayHID {
	std::tuple<int, Battery::Power_supply_status> Battery1004_parse(const HIDPPMsg_20& msg);
}