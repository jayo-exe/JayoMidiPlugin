#Jayo's MIDI Plugin for VNyan​

A VNyan Plugin that allows Note On/Off and Control Change messages from your MIDI controller to interact with your VNyan node graphs! Use your MIDI controller as a massive VNyan control panel!

# Table of contents
2. [Installation](#installation)
3. [Usage](#usage)
    1. [Selecting a MIDI Device](#selecting-a-midi-device)
    2. [Controlling VNyan](#controlling-vnyan)
        1. [Value Parameters](#value-parameters)
        2. [Outbound Triggers](#outbound-triggers)

## Installation
1. Grab the ZIP file from the [latest release](https://github.com/jayo-exe/JayoMidiPlugin/releases/latest) of the plugin.
2. Extract the contents of the ZIP file _directly into your VNyan installation folder_.  This will add the plugin files to yor VNyan `Items\Assemblies` folder, as well as copying a managed MIKDI library to `VNyan_Data/Managed`.
4. Open the VNyan Settings window, go to the "Misc" section, and ensure that **Allow 3rd Party Mods/Plugins** is enabled. This is required for this plugin  (or any plugin) to function correctly, so if you've already got other plugins installed you can probably skip this step.
5. Restart VNyan to allow the plugin and libraries to be loaded
6. One VNyan loads, confirm that a button for the plugin now exists in your Plugins window!

## Usage

### Selecting a MIDI Device
In order for the plugin to listed to a MIDI device, you'll need to select it from the dropdown list within the plugin window.  Most MIDI devices can only support being connected to one application at a time, so ensure your desired MIDI device is not already in use.

### Controlling VNyan
When the plugin is connected, the plugin will send specially-named triggers corresponding to the notes and control changes that occur, as well as maintaining float parameters for each of the note and control values 

#### Value Parameters
This plugin will set specially-named parameters to keep track of note and control values

| Type                  | Parameter Name          | Description of Value                                      | Example Value |
|-----------------------|-------------------------|-----------------------------------------------------------|---------------|
| Note Value            | `_xjm_note_<number>`    | The last known value of the MIDI note with ID <number>    | `69`          |
| Control Value         | `_xjo_control_<number>` | The last known value of the MIDI control with ID <number> | `127`         |

#### Outbound Triggers
This plugin will also fire specially-named triggers on certain conditions.  the Midi Plugin UI proivides a way to quickly copy the triggers names for the last-used notes/controls for convenience.
Listen for these triggers in your VNyan node graphs to activate things!

| MIDI Message   | Trigger Name       | Description of Trigger                                 |
|----------------|--------------------|--------------------------------------------------------|
| Note On        | `_xjm_n1_<number>` | Activated when the note with ID <number> is turned on  |
| Note Off       | `_xjm_n0_<number>` | Activated when the note with ID <number> is turned off |
| Control Change | `_xjm_ct_<number>` | Activated when a control with ID <number> is changed   |
