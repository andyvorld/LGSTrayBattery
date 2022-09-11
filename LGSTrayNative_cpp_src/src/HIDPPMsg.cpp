#include "HIDPPMsg.hpp"

#include <type_traits>

namespace LGSTrayHID {
	HIDPPMsg::HIDPPMsg(HIDPP_length hidpp_length) : _buffer(new uint8_t[hidpp_enum_length(hidpp_length)])
	{
	}
	HIDPPMsg::HIDPPMsg(std::unique_ptr <uint8_t[]> buffer) : _buffer(std::move(buffer)) {

	}
	HIDPPMsg::~HIDPPMsg()
	{
	}
	uint8_t* HIDPPMsg::data()
	{
		return this->_buffer.get();
	}
	std::unique_ptr<uint8_t[]> HIDPPMsg::move_ptr()
	{
		return std::move(_buffer);
	}
	HIDPP_length HIDPPMsg::get_length()
	{
		return static_cast<HIDPP_length>(this->_buffer[0]);
	}
	uint8_t HIDPPMsg::get_device_idx()
	{
		return this->_buffer[1];
	}
	const uint8_t* HIDPPMsg::get_result_data() const
	{
		return this->_buffer.get() + 4;
	}
	uint8_t HIDPPMsg_20::get_feature_index()
	{
		return this->_buffer[2];
	}
	uint8_t HIDPPMsg_20::get_function_id()
	{
		return (this->_buffer[3] & 0xF0) >> 4;
	}
	uint8_t HIDPPMsg_20::get_sw_id()
	{
		return (this->_buffer[3] & 0x0F);
	}
	uint8_t HIDPPMsg_10::get_sub_id()
	{
		return this->_buffer[2];
	}
	uint8_t HIDPPMsg_10::get_address()
	{
		return this->_buffer[3];
	}
}

// Bug with intellisense, failing static_assert
#ifndef __INTELLISENSE__
using namespace LGSTrayHID;
static_assert(std::is_layout_compatible_v<HIDPPMsg, HIDPPMsg_10>);
static_assert(std::is_layout_compatible_v<HIDPPMsg, HIDPPMsg_20>);
#endif