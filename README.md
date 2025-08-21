# VirtualDesktopTray

A lightweight tray tool for Windows 11 to quickly show and switch between **Virtual Desktops**.  
It uses [VirtualDesktopAccessor.dll](https://github.com/Ciantic/VirtualDesktopAccessor) for native API access.

## Features

- Shows the **current Virtual Desktop number** as a tray icon.
- **Context menu** in tray to jump directly to any desktop.
- **Left-click on tray icon** cycles through desktops (wrap-around when reaching the last one).
- Small and resource-friendly (single executable, ~150 KB + dependency DLL).
- Compatible with **Windows 11 only** (Windows 10 DLL not supported).

---

## Dependencies

- [.NET 9.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)  
  (unless built as self-contained publish).
- [VirtualDesktopAccessor.dll](https://github.com/Ciantic/VirtualDesktopAccessor/releases)  
  – must be placed in the **same directory** as `VirtualDesktopTray.exe`.

---

## Build

Clone this repo and build with .NET SDK 9.0+:

```
git clone https://github.com/USERNAME/VirtualDesktopTray.git
cd VirtualDesktopTray
dotnet build -c Release
```
This produces:
```
bin/Release/net9.0-windows/VirtualDesktopTray.dll
bin/Release/net9.0-windows/VirtualDesktopTray.exe
```
### Publish as single EXE (optional)
If you don’t want to require users to install the .NET runtime:
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output ./publish
```
This will generate a standalone VirtualDesktopTray.exe (~100 MB) in:
```
publish/
```
You still need to ship VirtualDesktopAccessor.dll alongside it.

## Usage

1. Place VirtualDesktopTray.exe and VirtualDesktopAccessor.dll in the same folder.
2. Run VirtualDesktopTray.exe.
3. A tray icon will appear:
   - Number shows your current desktop.
   - Left click → jump to next desktop.
   - Right click → open context menu, jump directly to any desktop, or exit.

## Notes
- Works only on Windows 11 (build 22000 or newer).
- Windows 10 builds of VirtualDesktopAccessor.dll are not compatible.
- Minimal resources: updates icon every 200 ms, practically no CPU usage.

## License
MIT – free to use and modify.

