# Directed Traffic Information Window Plugin
vatSys Plugin that creates a window to record passing of Directed Traffic Information

## Installation
1. Download the **dti-window.zip** file from the [latest release](https://github.com/alphadelta332/dti-window/releases/latest).
2. Unzip the file and copy to the base vatSys plugins directory: *"[vatSys installation folder]\bin\Plugins"*. Do NOT install in My Documents\vatSys Files.
3. Verify it has been installed in vatSys by checking "Info > About".

## How to use
### Adding a Traffic Pairing
Add a traffic pairing when you identify that directed traffic information will need to be passed between two aircraft at some point.

1. `Designate` an aircraft
2. Hold the Traffic Pair key. By default, this is `F7`*
3. `Designate` another aircraft
4. This will create a traffic pairing between these two aircraft in the Traffic Info Window.

*The Keybind for creating Traffic Pairs can be modified in the vatSys Keyboard Settings Menu.

https://github.com/user-attachments/assets/aa6c9e22-3839-4931-ae04-a11f1860b023

### Record whether the traffic has been passed
Keep track of what traffic information has been passed, and what is outstanding.

1. `Left click` the indented callsign to change it from **white** to **blue**. This signifies that the traffic has been passed on this aircraft.
2. `Right click` the indented callsign to change it from **blue** to **white**. This signifies that the traffic has **not yet** been passed on this aircraft.

### Delete aircraft from the window
Clean up the window when traffic information is no longer required between two aircraft.

`Middle click` indented aircraft to remove them from the window. Deleting an aircraft will **not** delete the reciprocal pairing (ie, deleting DEF from ABC **will not** delete ABC from DEF).

https://github.com/user-attachments/assets/aaa33b7e-8eaf-4331-8fe9-5ffb7416b4e8

---

## Contributing

### Prerequisites
- Visual Studio 2022 (or JetBrains Rider)
- [.NET Framework 4.8.1 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481)
- vatSys installed locally

### First-time setup
1. Clone the repository
2. Copy `LocalOverrides.props.example` to `LocalOverrides.props` (gitignored) and set `VatSysPath` to your vatSys installation directory:
    ```xml
    <VatSysPath>C:\vatSys</VatSysPath>
    ```
3. Build the project — the plugin DLL is output directly to `[VatSysPath]\bin\Plugins\dti-window\`, and `Properties\launchSettings.json` is generated automatically for the debug profile.

That's it. Press **F5** in Visual Studio to launch vatSys with the plugin loaded.

### Project structure
```
Events/          — keyboard and mouse input handling, vatSys event hooks
Integration/     — plugin entry point, crash logging, error reporting
Models/          — Aircraft, ChildAircraft, Pairings, AircraftManager
UI/              — main window, colour helpers, keyboard settings injection
Scripts/         — build-time scripts (version stamping)
```

### Versioning
The version is defined in `Version.json` as `Major.Minor.Build`. `Properties/VersionInfo.g.cs` is generated automatically at build time from this file — do not edit it manually. The plugin checks GitHub for a newer version on startup using this same file.

To bump the version for a release, update `Version.json` and commit it alongside your changes.

### Gitignored files
The following files are local to each contributor and should never be committed:
| File | Purpose |
|---|---|
| `LocalOverrides.props` | Sets `VatSysPath` for your machine |
| `Properties/launchSettings.json` | VS debug launch profile (auto-generated on first build) |
