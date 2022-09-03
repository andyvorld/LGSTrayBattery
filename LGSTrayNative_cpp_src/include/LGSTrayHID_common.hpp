#pragma once

#include <cinttypes>

namespace LGSTrayHID {
	enum HIDPP_length : uint8_t {
		HIDPP_SHORT = 0x10,
		HIDPP_LONG = 0x11
	};

	constexpr uint8_t SW_ID = 0X0A;

	constexpr uint8_t HIDPP_SHORT_SIZE = 7;
	constexpr uint8_t HIDPP_LONG_SIZE = 20;

	size_t hidpp_enum_length(HIDPP_length hidpp_length);
}