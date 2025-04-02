# DTI Window Plugin
vatSys Plugin that creates a window to record passing of Directed Traffic Information

## Installation
1. Download the dti-window.zip file from the [latest release](https://github.com/alphadelta332/dti-window/releases).
2. Unzip the file and copy to the base vatSys plugins directory: "[vatSys installation folder]\bin\Plugins". Do NOT install in My Documents\vatSys Files.
3. Verify it has been installed in vatSys by checking "Info > About".

## How to use
### Adding a Traffic Pairing
Add a traffic pairing when you identify that directed traffic information will need to be passed between two aircraft at some point.

1. `Designate` an aircraft
2. Hold `F7`
3. `Designate` another aircraft
4. This will create a traffic pairing between these two aircraft in the DTI Window.

### Record whether the traffic has been passed
Keep track of what traffic information has been passed, and what is outstanding.

1. `Left click` the indented callsign to change it from **white** to **blue**. This signifies that the traffic has been passed on this aircraft.
2. `Right click` the indented callsign to change it from **blue** to **white**. This signifies that the traffic has **not yet** been passed on this aircraft.

### Delete aircraft from the window
Clean up the window when traffic information is no longer required between two aircraft.

`Middle click` indented aircraft to remove them from the window. Deleting an aircraft will **not** delete the reciprocal pairing (ie, deleting DEF from ABC **will not** delete ABC from DEF).