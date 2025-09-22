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
```

## License

This project is licensed under the [GNU General Public License v3.0 (GPL-3.0)](LICENSE).

### Additional Terms
In accordance with Section 7 of GPL-3.0:

- **Logos and Trademarks**  
  The Pullman Regional Hospital (PRH) name, logo(s), and any associated trademarks are *not* licensed under GPL-3.0.  
  These marks remain the exclusive property of PRH.  

- **Redistribution**  
  Any redistribution of this software outside of PRH must remove or replace the PRH name and logo(s).  
  The assets in the `Assets/` directory are provided for internal PRH use only and may not be redistributed or reused.  

- **No Endorsement**  
  The presence of PRH branding in this software does not imply endorsement, certification, or responsibility for modifications made by third parties.  
