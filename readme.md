# LGS Tray Battery (WIP Revamp Branch)
~~A tray app used to track battery levels of wireless Logitech mouse.~~

~~**Does not support mice that uses "Unified Battery Reporting" (newer than 2020), I currently do not have access to the hardware and hence will not be able to implement it. If needed I have an alternative program at https://github.com/andyvorld/LGSTrayBattery_GHUB, which requires G HUB running in the background (more specifically `lghub_agent.exe`).**~~

A rewrite/combination of my two programs [LGSTrayBattery](https://github.com/andyvorld/LGSTrayBattery) and [LGSTrayBattery_GHUB](https://github.com/andyvorld/LGSTrayBattery_GHUB), which should allow for interaction via both the native HID and Logitech Gaming Hub websockets.

## New Features
- New reactive Icons
    - Now reacts to light/dark theme
    - Now reacts to what type of device you currently have selected (Supports mouse, keyboards and headsets)
- Native HID Battery percentages now uses a formula rather old xml files
    - Assuming they use 3.7V lipo batteries, if you are getting weird battery percentages or voltage readings < 3.5V, please open an issue
- Smarter polling (Planned)
    - When successful don't update for a long delay, else check back frequently till an update

## Known Missing features
- Force update battery percentage
- Force rescan of devices
- Reselect last device on startup
## Features
### Tray Indicator
![](https://i.imgur.com/g5e3jsz.png)

Battery percentage and voltage (if supported) in a tray tooltip with notification icon.

Double click for rescan/refresh.

Right-click for more options.

### Http/Web "server" api
By default the running of the http server is disabled, to enable modify `HttpConfig.ini` and change `serverEnable = false` to `serverEnable = true`. The IP address and port used for bindings are under `tcpAddr` and `tcpPort` respectively with the defaults being `localhost` and `12321`.

`tcpAddr` accepts either a hostname (`DESKTOP-1234`) or an IP address (`127.0.0.1`) to bind to, if you are not sure use `localhost` or if you have admin permission `0.0.0.0` to allow for external access to the devices. If an invalid hostname is provided, the server will fall back to binding on `127.0.0.1`.

![](https://i.imgur.com/IH4YKHl.png)

Send a GET/HTTP request to `{tcpAddr}:{tcpPort}/devices`, for the list of devices currently detected by the program and the corresponding `deviceID`.

![](https://i.imgur.com/hFIlh0o.png)

With the `deviceID`, a GET/HTTP request to `{tcpAddr}:{tcpPort}/device/{deviceID}`, will result in an xml document of the name and battery status of the device. Devices that do not support `battery_voltage` will report 0.00.

## Known Issues
- Logitech gaming mouses do not natively have a way of reporting battery level, but rather voltage levels. A voltage to percentage lookup table is available for some mouses from Logitech Gaming Software and are included in `PowerModel`. However newer mice have their files embedded within Logitech G Hub and it is not possible to retrieve them without owning said mice. It is possible to dump an `.xml` file within `PowerModel` for support. [Refer to this issue in libratbag.](https://github.com/libratbag/piper/issues/222#issuecomment-487557251)

    - Without the `.xml` file, the tray will display `?` with the tooltip giving `NaN%` with a valid voltage.

    - Using this [automated tool](https://github.com/andyvorld/LGSTrayBattery_GHUB_dump) for automated extraction of `.xml` files. (Working as of 24/02/2021)

- Does not support mice that uses HID++ 1.0 protocols. (Older than 2012?)

- Does not work on mice that uses "Unified Battery Reporting". (Newer than late 2020?)

- ~~No working refresh/rescan code for connected/disconnect devices. Current work around is to restart the program.~~ Rescan device is done manually within the right-click context menu.

- Currently there are some weird interactions with mouses that can be wired or wireless (e.g. G403) when running in wired mode.

- For unifying receiver devices, currently the program only polls the first device.

## Working with
- G403 Wireless
- MX Anywhere 2S
### Community Tested
- G604 Lightspeed Wireless
- MX Vertical
- G703 Lightspeed Wireless

## Acknowledgements
This project began as a task with me messing around with my mouse for battery tracking.

- [Solaar](https://github.com/pwr-Solaar/Solaar), for the source code to base the HID++ paramters and reverse engineering of the protocol.
- [XB1ControllerBatteryIndicator](https://github.com/NiyaShy/XB1ControllerBatteryIndicator), for the idea and base of the icons
- [The Noun Project](https://thenounproject.com/), for base icons
    - empty battery, By Adrien Coquet, FR, In the Battery Collection
    - Mouse, By projecthayat, ID, In the Technology & computer hardware Collection
    - Keyboard, By HideMaru, ID, In the Electronic BL.2 Collection
    - Headphones, By Peter Lakenbrink, DE, In the School and Online Learning Glyph Collection