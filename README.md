# PRH Computer Info

A lightweight WPF desktop application that displays local computer and user information, and provides quick-access tools for Pullman Regional Hospital staff.

## Features
- Display computer name, IP addresses, logged-in user, OS, default printer, and reboot time
- One-click buttons to:
  - Copy system info to clipboard
  - Auto-generate email to IT with system info prefilled
  - Access Epic help resources
- PRH-branded look and feel

## Requirements
- Windows 10 or later (x64)
- [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)

Install the runtime with:

```powershell
winget install Microsoft.DotNet.DesktopRuntime.8 --architecture x64
