#include "LGSTrayHID_common.hpp"

namespace LGSTrayHID {
	size_t hidpp_enum_length(HIDPP_length hidpp_length) {
		if (hidpp_length == HIDPP_SHORT) {
			return HIDPP_SHORT_SIZE;
		}
		else if (hidpp_length == HIDPP_LONG) {
			return HIDPP_LONG_SIZE;
		}

		return -1;
	}
}