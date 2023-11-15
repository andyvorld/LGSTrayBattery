# LGS Tray Battery -- V3 Preview

A rewrite/combination of my two programs [LGSTrayBattery](https://github.com/andyvorld/LGSTrayBattery) and [LGSTrayBattery_GHUB](https://github.com/andyvorld/LGSTrayBattery_GHUB), which should allow for interaction via both the native HID and Logitech Gaming Hub websockets.

## Changes from V2
*When migrating from earlier versions, device ids may have changed.*
- Moved to .Net 8
- Realtime reactive icons
    - Light/Dark theme is now reactive in realtime
- Rewritten to use hidapi directly for hotplug support
    - Wired/Wireless devices like the G403 should behave like a single device
- Multi-device mode
- Numerical Icons
- HID.NET manager has been deprecated

## Features
### Tray Indicator
![image](https://user-images.githubusercontent.com/24492062/138280300-6966b6a4-ff6d-46e6-9698-d2c8d612eb11.png)

Battery percentage and voltage (if supported) in a tray tooltip with notification icon.

Right-click for more options.

### Multiple Icons
![image](https://i.imgur.com/h1UUpeX.png)

Depending on the number of devices selected in the context menu, multiple devices can be seen simultatniously

### Numerical Icons
![image](https://i.imgur.com/DNiCGz1.png)

Display the current battery percentage as a number.

*In numerical display mode, charging status will not be displayed*

### Reactive Icons
![image](https://user-images.githubusercontent.com/24492062/138284660-95949372-c59a-4569-9545-0cfe0506d1fb.png)

Icon changes to match devices type (Current supported: mouse, keyboard and headsets)

![image](https://user-images.githubusercontent.com/24492062/138285048-ad229703-5c4e-430e-b107-c50eb341e46b.png)

Icon changes to match light/dark system theme

![image](https://i.imgur.com/351EEX0.png)

Icon changes to reflect current charging status

### Http/Web "server" api
By default the running of the http server is disabled, to enable modify `appsettings.ini` and change `serverEnable = false` to `serverEnable = true`. The IP address and port used for bindings are under `tcpAddr` and `tcpPort` respectively with the defaults being `localhost` and `12321`.

`tcpAddr` accepts either a hostname (`DESKTOP-1234`) or an IP address (`127.0.0.1`) to bind to, if you are not sure use `localhost` or if you have admin permission `0.0.0.0` to allow for external access to the devices. If an invalid hostname is provided, the server will fall back to binding on `127.0.0.1`.

![image](https://user-images.githubusercontent.com/24492062/138280886-1929b49b-b4a3-454d-8371-80fd41df8e66.png)

Send a HTTP/GET request to `{tcpAddr}:{tcpPort}/devices`, for the list of devices currently detected by the program and the corresponding `deviceID`.

![image](https://user-images.githubusercontent.com/24492062/138281030-f40ba805-69bf-48ac-a126-6f58f9ca7828.png)

With the `deviceID`, a HTTP/GET request to `{tcpAddr}:{tcpPort}/device/{deviceID}`, will result in an xml document of the name and battery status of the device. Devices that do not support `battery_voltage` will report 0.00.

Device ids starting with `dev` originates from tapping into Logitech GHUB's own drivers, while random numbers are from the natively implement HID++ code. Thus, there are some fields that different between the two,

|                 | GHUB* | Native  |
|-----------------|-------|---------|
| device_id       | ✔️   | ✔️     |
| device_name     | ✔️   | ✔️     |
| device_type     | ✔️   | ✔️     |
| battery_percent | ✔️   | ✔️     |
| battery_voltage | ❌   | ✔️**   |
| mileage***      | ✔️   | ❌     |
| charging        | ✔️   | ✔️     |

\* - Need Logitech G Hub Installed

\** - Depends on the device

\*** - Logitech G Hub's metric of estimated life left on the battery

## HID++ Device Sources
As of v3.0.0, there are 2 sources in which the program will pull battery status,

- Logitech G HUB via Websockets
- Native HID, hidapi via PInvoke (Called "Native" in settings)

These sources can be individually disabled/enabled before runtime via `appsettings.ini`, in the `DeviceManager` section,

```
[DeviceManager]
GHUB = true
Native = true
```

*GHUB is Logitech G HUB, Native is hidapi*

## Known Issues
### Common
- Native HID and GHUB do not provide similar percentages, this is due to how native and GHUB calculates percentages from the device's voltages. Native uses an average curve of a 3.7V lipo battery, while GHUB will use a lookup table specific to the device.

### Native HID (hidapi)
- Certain wired devices like the G403 when in wired mode does not report the number of HID devices connected and will respond to all request. Causing battery polls to occur 6x per request.
- Device and protocol discovery changed from the previous method, some devices like the G533 headsets might not be detected, try the GHUB based manager.

### GHUB
- Future GHUB version may change IPC protocol/endpoints (currently websocket)

## Working with
- G403 Wireless
- MX Anywhere 2
### Community Tested
*HID Backend has changed, would need restesting of devices, please raise a PR to add to this list*


## How to Build project
TBA

## Acknowledgements
This project began as a task with me messing around with my mouse for battery tracking.

- [Solaar](https://github.com/pwr-Solaar/Solaar), for the source code to base the HID++ paramters and reverse engineering of the protocol.
- [XB1ControllerBatteryIndicator](https://github.com/NiyaShy/XB1ControllerBatteryIndicator), for the idea and base of the icons
- [The Noun Project](https://thenounproject.com/), for base icons
    - Mouse, By projecthayat, ID, In the Technology & computer hardware Collection
    - Keyboard, By HideMaru, ID, In the Electronic BL.2 Collection
    - Headphones, By Peter Lakenbrink, DE, In the School and Online Learning Glyph Collection
