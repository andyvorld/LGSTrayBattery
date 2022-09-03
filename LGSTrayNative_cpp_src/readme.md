# HID++ Battery Monitor
Logitech HID++ device battery monitor for use with [LGSTrayBattery](https://github.com/andyvorld/LGSTrayBattery).

## Features
The program replicates a small subset of the Logitech GHUB's websocket API, utilizing a json payload system.

Example payload,
```
{
    "msgId": "1234-5678-90",
    "origin": "backend",
    "verb": "GET",
    "path", "/devices/list"
}
```

## Build Instructions
Boost 1.79.0+ required to be built and installed.

Other pre-requisites will be downloaded as part of the CMake configuration step.

Built with Visual Studio 2022, native CMake project.