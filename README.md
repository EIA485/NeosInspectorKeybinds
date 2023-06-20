# InspectorKeybinds

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that adds some keybinds to the inspector


config options to change each bind exist. to disable a keybind clear all it's keys. the dicts state what keys must be in what state for the bind to trigger. the first element of the dict is the one that will trigger the rest of the states to be checked.
## defaults:
- `c` create child under selected obj
- `alt + c` create child under current root
- `j` create parent
- `j + ctrl` parent under world root
- `j + alt` parent under local user space
- `j` create parent
- `g` duplicate
- `y` object root
- `u` up one object
- `h` focus
- `backspace` delete
- `alt + backspace` delete no preserve assets
- `v` open component attacher
- `b` bring to
- `b + alt` jump to
- `r + alt` reset position
- `r + alt + ctrl` reset rotation
- `r + ctrl` reset scale
- `p` create pivot

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
1. Place [InspectorKeybinds.dll](https://github.com/eia485/NeosInspectorKeybinds/releases/latest/download/InspectorKeybinds.dll) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Neos logs.
