# Key2Xbox.Rewrite (.NET C#)

Dibuat untuk fleksibilitas penggunaan keyboard sebagai pengontrol kamera PTZ di aplikasi vMix. Operator dapat dengan mudah mengubah input keyboard menjadi sinyal Virtual Xbox 360, memungkinkan pergerakan kamera yang lebih fleksibel.

> Rewrite dari [Python](https://github.com/yeftakun/ketPTZ) ke .NET C# (WinForms + System Tray)

- Keyboard mapping ke virtual Xbox 360 controller
- Dukungan gamepad fisik + virtual secara bersamaan
- Modifier key, boost key, boost multiplier
- Hold/Cruise control
- Editor config & profile dari tray

## Release
```powershell
dotnet publish Key2Xbox.Rewrite.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=false /p:PublishTrimmed=false /p:DebugType=None /p:DebugSymbols=false -o release/win-x64-small-safe
```

## Penggunaan

Khusus ketika membuat shortcut xbox untuk kamera PTZ di vmix, set persentase analog & trigger ke 100%.

⚠️Jalankan sebelum vmix; Jangan exit sebelum vmix dimatikan. 

## File Data
Folder `%LOCALAPPDATA%\\keyPTZ-desktop`.

- `config.json`
- `profiles/*.json`