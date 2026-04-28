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

<img width="700" alt="vatSys_7oV7ErF06N" src="https://github.com/user-attachments/assets/97ccc46c-c244-436b-a8b7-666c1d413816" />

### Record whether the traffic has been passed
Keep track of what traffic information has been passed, and what is outstanding.

1. `Left click` the indented callsign to change it from **white** to **blue**. This signifies that the traffic has been passed on this aircraft.
2. `Right click` the indented callsign to change it from **blue** to **white**. This signifies that the traffic has **not yet** been passed on this aircraft.

<img width="700" alt="vatSys_y3omzuJyHW" src="https://github.com/user-attachments/assets/9593e14b-f6e7-4761-b4d4-538b9054affd" />

### Delete aircraft from the window
Clean up the window when traffic information is no longer required between two aircraft.

`Middle click` indented aircraft to remove them from the window. Deleting an aircraft will **not** delete the reciprocal pairing (ie, deleting DEF from ABC **will not** delete ABC from DEF).

<video src="Assets/Delete Traffic Pair.mp4" width="500" controls></video>

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