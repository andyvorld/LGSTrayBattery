#pragma once

#include <memory>

#include <hidapi.h>
#include "hid_io_debug.hpp"

#include "LGSTrayHID_common.hpp"

namespace LGSTrayHID {
	class HIDPPMsg {
	protected:
		std::unique_ptr<uint8_t[]> _buffer;
	public:
		HIDPPMsg(HIDPP_length hidpp_length);
		HIDPPMsg(std::unique_ptr<uint8_t[]> buffer);
		~HIDPPMsg();

		uint8_t* data();
		std::unique_ptr<uint8_t[]> move_ptr();

		HIDPP_length get_length();
		uint8_t get_device_idx();

		const uint8_t* get_result_data() const;
	};

	class HIDPPMsg_10 : public HIDPPMsg {
	public:
		using HIDPPMsg::HIDPPMsg;
		uint8_t get_sub_id();
		uint8_t get_address();
	};

	class HIDPPMsg_20 : public HIDPPMsg {
	public:
		using HIDPPMsg::HIDPPMsg;
		uint8_t get_feature_index();
		uint8_t get_function_id();
		uint8_t get_sw_id();
	};
}