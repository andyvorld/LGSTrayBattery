#pragma once
#include <hidapi.h>

#ifdef _DEBUG
#include "_hid_io_debug.hpp"

#define hid_write(...) hid_debug::hid_write(__VA_ARGS__) 
#define hid_read(...) hid_debug::hid_read(__VA_ARGS__) 
#define hid_read_timeout(...) hid_debug::hid_read_timeout(__VA_ARGS__) 

#endif
