# Introduction
The project is intend to provide a way to easily get the battery level of Bluetooth Low Energy peripherals.

# Help
This application automatically discover all bluetoothLE devices paired to your PC.
The battery icon, in systray, display the lowest bluetoothLE device battery level.
The battery level of all devices is displayed in systray balloon.
A notification is automatically triggered when a battery level reach 20%.

This fork shows the notification as a modal message box instead of a native Windows notification, because native Windows notifications don't show atop applications in fullscreen/windowed fullscreen.

# References
- https://github.com/MUedsa/BluetoothLEBatteryMonitor/
