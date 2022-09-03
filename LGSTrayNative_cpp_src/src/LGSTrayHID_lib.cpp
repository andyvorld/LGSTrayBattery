#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#include <windows.h>
#include <iostream>

#include <map>
#include <sstream>
#include <hidapi.h>
#include <hidapi_winapi.h>

#include "LGSTrayHID_lib.hpp"

#include "MapWrapper.hpp"
#include "LogiDevice.hpp"
#include "HIDDevice.hpp"

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
#ifdef _DEBUG
        std::cout << "DEBUG" << std::endl;
#else
        std::cout << "RELEASE" << std::endl;
#endif

        std::cout << "DLL_PROCESS_ATTACH" << std::endl;
        break;
    case DLL_THREAD_ATTACH:
        std::cout << "DLL_THREAD_ATTACH" << std::endl;
        break;
    case DLL_THREAD_DETACH:
        std::cout << "DLL_THREAD_DETACH" << std::endl;
        break;
    case DLL_PROCESS_DETACH:
        std::cout << "DLL_PROCESS_DETACH" << std::endl;
        break;
    }
    return TRUE;
}

EXTERN_DLL_EXPORT int DLLHELLO() {
    std::cout << "DLL_HELLO" << std::endl;

    return 1;
}

EXTERN_DLL_EXPORT void Register_battery_update_cb(Battery_update_callback cb) {
    LGSTrayHID::LogiDevice::Register_battery_update_cb([cb](const LGSTrayHID::LogiDevice& dev) {
        cb(
            dev.dev_id.c_str(),
            dev.battery_percentage.load(),
            dev.battery_status.load() == LGSTrayHID::Battery::Power_supply_status::POWER_SUPPLY_STATUS_CHARGING,
            -1.0,
            dev.battery_voltage.load()
        );
    });
}

EXTERN_DLL_EXPORT void Register_device_ready_cb(Device_ready_callback cb) {
    LGSTrayHID::LogiDevice::Register_device_ready_cb([cb](const LGSTrayHID::LogiDevice& dev) {
        cb(
            dev.dev_id.c_str(),
            (int) dev.device_type,
            dev.device_name.c_str()
        );
    });
}

namespace {
    enum USAGE_PAGE : uint8_t {
        SHORT = 0x01,
        LONG = 0x02,
        VERY_LONG = 0x04
    };

    std::string GUID_to_string(const GUID& guid) {
        std::ostringstream os;

        os << std::hex << guid.Data1;
        os << std::hex << guid.Data2;
        os << std::hex << guid.Data3;

        for (size_t i = 0; i < 8; i++) {
            os << std::hex << static_cast<int>(guid.Data4[i]);
        }

        return os.str();
    }

    struct GUIDComparer
    {
        bool operator()(const GUID& Left, const GUID& Right) const
        {
            // comparison logic goes here
            return memcmp(&Left, &Right, sizeof(Right)) < 0;
        }
    };
}

EXTERN_DLL_EXPORT void Load_devices() {
    static bool hid_initialised = false;
    if (!hid_initialised) {
        hid_init();
        hid_initialised = true;
    }

    auto devs = std::unique_ptr<hid_device_info, decltype(&hid_free_enumeration)>(hid_enumerate(0x046d, 0x00), &hid_free_enumeration);
    auto cur_dev = devs.get();

    static std::map<GUID, std::unique_ptr<LGSTrayHID::HIDDevice>, GUIDComparer> hid_device_map;
    hid_device_map.clear();

    while (cur_dev) {
        if ((cur_dev->usage_page & 0xFF00) == 0xFF00) {
            if ((cur_dev->usage == USAGE_PAGE::SHORT) || (cur_dev->usage == USAGE_PAGE::LONG)) {
                auto c_hid_device = std::shared_ptr<hid_device>(hid_open_path(cur_dev->path), &hid_close);
                GUID _guid;
                hid_winapi_get_container_id(c_hid_device.get(), &_guid);

                bool found = (hid_device_map.find(_guid) != hid_device_map.end());

                if (!found) {
                    hid_device_map[_guid] = std::unique_ptr<LGSTrayHID::HIDDevice>(new LGSTrayHID::HIDDevice(GUID_to_string(_guid)));
                }

                if (cur_dev->usage == USAGE_PAGE::SHORT) {
                    hid_device_map[_guid]->assign_short(c_hid_device);
                }
                else if (cur_dev->usage == USAGE_PAGE::LONG) {
                    hid_device_map[_guid]->assign_long(c_hid_device);
                }
            }
        }

        cur_dev = cur_dev->next;
    }
}

EXTERN_DLL_EXPORT void Update_device_battery(const char* dev_id) {
    LGSTrayHID::MapWrapper::update_device_battery(dev_id);
}