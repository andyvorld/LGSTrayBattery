#pragma once
#define EXTERN_DLL_EXPORT extern "C" __declspec(dllexport)

// Device_ID, Battery Percent, Charing_Flag, Milage, Battery Voltage
typedef void (*Battery_update_callback)(const char*, int, bool, double, int);

// Device_ID, Device Type, Device Name
typedef void (*Device_ready_callback)(const char*, int, const char*);

EXTERN_DLL_EXPORT int DLLHELLO();

EXTERN_DLL_EXPORT void Register_battery_update_cb(Battery_update_callback cb);
EXTERN_DLL_EXPORT void Register_device_ready_cb(Device_ready_callback cb);

EXTERN_DLL_EXPORT void Load_devices();

EXTERN_DLL_EXPORT void Update_device_battery(const char* dev_id);