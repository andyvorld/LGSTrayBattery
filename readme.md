# LGS Tray Battery
A tray app used to track battery levels of wireless Logitech mouse.

## Known Issues
Logitech gaming mouses do not natively have a way of reporting battery level, but rather voltage levels. A voltage to percentage lookup table is available for some mouses from Logitech Gaming Software and are included in `PowerModel`. However newer mice have their files embedded within Logitech G Hub and it is not possible to retrieve them without owning said mice. It is possible to dump an `.xml` file within `PowerModel` for support. [Refer to this issue in libratbag.](https://github.com/libratbag/piper/issues/222#issuecomment-487557251)

Without the `.xml` file, the tray will display `?` with the tooltip giving `NaN%` with a valid voltage.

Does not support mice that uses HID++ 1.0 protocols. (Older than 2012?)

No working refresh/rescan code for connected/disconnect devices. Current work around is to restart the program.

Currently there are some weird interactions with mouses that can be wired or wireless (e.g. G403) when running in wired mode.

## Working with
- G403 Wireless
- MX Anywhere 2S

## Acknowledgements
This project began as a task with me messing around with my mouse for battery tracking.

- [Solaar](https://github.com/pwr-Solaar/Solaar), for the source code to base the HID++ paramters and reverse engineering of the protocol.
- [XB1ControllerBatteryIndicator](https://github.com/NiyaShy/XB1ControllerBatteryIndicator), for the idea and base of the icons