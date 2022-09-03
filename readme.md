# LGS Tray Battery

A rewrite/combination of my two programs [LGSTrayBattery](https://github.com/andyvorld/LGSTrayBattery) and [LGSTrayBattery_GHUB](https://github.com/andyvorld/LGSTrayBattery_GHUB), which should allow for interaction via both the native HID and Logitech Gaming Hub websockets.

## New Features to V1
- Retargetted from .Net Framework 4.6 to .Net 6
    - Dropped 32Bit Windows
- New reactive Icons
    - Now reacts to light/dark theme
    - Now reacts to what type of device you currently have selected (Supports mouse, keyboards and headsets)
- Native HID Battery percentages now uses a look up table rather than old xml files
    - Assuming they use 3.7V lipo batteries, if you are getting weird battery percentages or voltage readings < 3.5V, please open an issue
- Updated HID backend to detect plugging in wireless devices
- Smarter polling
    - When successful don't update for a long delay, else check back frequently till an update
- "Persistant" Data
    - Device battery percentages should persists even if said device is disconnected, battery percentages will then be updated on reconnection

## Known Missing features
- Force update battery percentage (Smarter polling with force device rescan should cover this)

## Features
### Tray Indicator
![image](https://user-images.githubusercontent.com/24492062/138280300-6966b6a4-ff6d-46e6-9698-d2c8d612eb11.png)

Battery percentage and voltage (if supported) in a tray tooltip with notification icon.

Double click for rescan/refresh.

Right-click for more options.

### Reactive Icons
![image](https://user-images.githubusercontent.com/24492062/138284660-95949372-c59a-4569-9545-0cfe0506d1fb.png)

Icon changes to match devices type (Current supported: mouse, keyboard and headsets)

![image](https://user-images.githubusercontent.com/24492062/138285048-ad229703-5c4e-430e-b107-c50eb341e46b.png)

Icon changes to match light/dark system theme

### Http/Web "server" api
By default the running of the http server is disabled, to enable modify `appsettings.ini` and change `serverEnable = false` to `serverEnable = true`. The IP address and port used for bindings are under `tcpAddr` and `tcpPort` respectively with the defaults being `localhost` and `12321`.

`tcpAddr` accepts either a hostname (`DESKTOP-1234`) or an IP address (`127.0.0.1`) to bind to, if you are not sure use `localhost` or if you have admin permission `0.0.0.0` to allow for external access to the devices. If an invalid hostname is provided, the server will fall back to binding on `127.0.0.1`.

![image](https://user-images.githubusercontent.com/24492062/138280886-1929b49b-b4a3-454d-8371-80fd41df8e66.png)

Send a HTTP/GET request to `{tcpAddr}:{tcpPort}/devices`, for the list of devices currently detected by the program and the corresponding `deviceID`.

![image](https://user-images.githubusercontent.com/24492062/138281030-f40ba805-69bf-48ac-a126-6f58f9ca7828.png)

With the `deviceID`, a HTTP/GET request to `{tcpAddr}:{tcpPort}/device/{deviceID}`, will result in an xml document of the name and battery status of the device. Devices that do not support `battery_voltage` will report 0.00.

Device ids starting with `dev` originates from tapping into Logitech GHUB's own drivers, while random numbers are from the natively implement HID++ code. Thus, there are some fields that different between the two,

|                 | GHUB | Native (HID.NET) | Native (HIDPP_Bat_Mon) |
|-----------------|------|------------------|------------------------|
| device_id       | ✔️   | ✔️              | ✔️                    |
| device_name     | ✔️   | ✔️              | ✔️                    |
| device_type     | ✔️   | ✔️              | ✔️                    |
| battery_percent | ✔️   | ✔️              | ✔️                    |
| battery_voltage | ❌   | ✔️*             | ✔️                    |
| mileage         | ✔️   | ❌              | ❌                    |
| charging        | ✔️   | ❌              | ✔️**                  |

\* - Depends on the device

\** - Device ID will change, (potentially useless) as some devices switches to a wired mode creating a new device ID (Tested with G403)

## HID++ Device Sources
As of v2.0.8, there are now 3 sources in which the program will pull battery status,

- Logitech G HUB via Websockets
- Native HID, C#/HID.NET (Might be broken in Windows 11?, default disabled)
- Native HID, C++/hidapi via PInvoke (Called "Native" in settings)

These sources can be individually disabled/enabled before runtime via `appsettings.ini`, in the `DeviceManager` section,

```
[DeviceManager]
GHUB = true
HID_NET = false
Native = true
```

*GHUB is Logitech G HUB, HID_NET is C# with HID.NET, Native is C++/hidapi*

### Differences between HID.NET and hidapi
The hidapi backend is rewritten in C++, and it differs from HID.NET in the following ways,
- Hot plugging is not supported, currently requires a manual selection of rescan devices to trigger a scan of newly connected devices
- Ability to see more than 1 devices per unifying reciever
    - Tested with a KB+M on a unifying dongle
- Ability to parse Unifying Receiver Battery Reporting (1004) (Coded in, have not been tested)
- Detect if device is charging (No notification tray UI changes, and as mention aboved may be useless as some devices in charging move switches device ID, the webserver xml output should show it)

## Known Issues
### Common
- Light/Dark theme reaction is not fast, need to wait until a battery update or selected device swap

- Native HID and GHUB do not provide similar percentages, this is due to how native and GHUB calculates percentages from the device's voltages. Native uses an average curve of a 3.7V lipo battery, while GHUB will use a lookup table specific to the device.

### Native HID (HID.NET)
- If the device is sleeping or disconnected, the program will not detect it even if the reciever is plugged in, usually occuring at boot
    - Current fix, restart the program or force a rescan via a double click or context menu

- Does not support mice that uses HID++ 1.0 protocols. (Older than 2012?)

- Does not work on mice that uses "Unified Battery Reporting". (Newer than late 2020?)

- For unifying receiver devices, currently the program only polls the first device.

### Native HID (C++/hidapi)
- Hotplug not supported
- Device and protocol discovery changed from the HID.NET method, some devices like the G533 headsets might not be detected.

### GHUB
- Future GHUB version may change IPC protocol/endpoints (current websocket)

## Working with
- G403 Wireless
- MX Anywhere 2
- Wireless Keyboard Dell KB714
### Community Tested
- G604 Lightspeed Wireless
- MX Vertical
- G703 Lightspeed Wireless

## Acknowledgements
This project began as a task with me messing around with my mouse for battery tracking.

- [Solaar](https://github.com/pwr-Solaar/Solaar), for the source code to base the HID++ paramters and reverse engineering of the protocol.
- [XB1ControllerBatteryIndicator](https://github.com/NiyaShy/XB1ControllerBatteryIndicator), for the idea and base of the icons
- [The Noun Project](https://thenounproject.com/), for base icons
    - Mouse, By projecthayat, ID, In the Technology & computer hardware Collection
    - Keyboard, By HideMaru, ID, In the Electronic BL.2 Collection
    - Headphones, By Peter Lakenbrink, DE, In the School and Online Learning Glyph Collection
