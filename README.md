# MidiRenamer
## Rename MIDI devices in Windows
By default, MIDI input/output devices in Windows get their names automatically and the names cannot be changed. If you have more than one MIDI port, it can however be difficult to remember which instrument is connected to which port, especially with devices like the ESI M4U eX which has 8 midi ports named "M4U eX 1" to "M4U eX 8". 

In Windows, the name (or "FriendlyName") of a device is stored in the registry, like for instance:
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceClasses\{6994ad04-93ef-11d0-a3cc-00a0c9223196}\##?#USB#VID_2573&PID_004A#no_serial_number#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\#ESI_MIDI1\Device Parameters\FriendlyName
```
(The identifiers `VID_2573&PID_004A` are for a ESI M4U eX device)

The name is reset at every reboot and if the device is moved to a different USB port, so changing the name manually is not a viable solution. 

__MidiRenamer__ is a tool for automatically changing the name of MIDI devices on demand or on startup. 

## Disclaimer
This software makes changes to the windows registry and has only been tested (with no ill effects) on one computer running Windows 10. I take no responsibility for any problems that may arise from using the software.

# How to use
Command line arguments (all optional):
- `-test` Start __MidiRenamer__ in test mode, where no changes are written to the registry.
- `-waitBeforeExit x` After running rename procedures, wait 'x' ms before exiting (i.e. to see the results before cmd quits)
- `<path-to-config-file(s)>` Paths to the config XML file(s) to load. Defaults to "MidiRenamerConfig.xml" if no other paths are specified.

A config XML file must exist specifying which device you wish to change, and which values you want to rename, like in the following example (or the example file in the source):

```
<midi-renamer device-partial-name="USB#VID_2573&amp;PID_004">
	<rename>
		<from>M4U eX 1</from>
		<to>0 - Prophet 5</to>
	</rename>
	<rename>
		<from>M4U eX 2</from>
		<to>1 - Wavestation</to>
	</rename>
</midi-renamer>
```

`device-partial-name` is the name of the MIDI device which you want to rename. As the name implies, the name can be partial (for instance to rename both "PID_004A" and "PID_004B" ports to the same names). Each `rename` node contains a `from` node with the original name which the __MidiRenamer__ looks for, which will be changed to the name in the `to` node. If `from` cannot be found, the requested name change will be ignored.

To rename the port(s) of more than one MIDI device, create a config XML file for each device and start __MidiRenamer__ with the paths to each file as arguments (see above).

## Limitations
__MidiRenamer__ only looks for "Device Parameters\FriendlyName" one level below the device key, but ignores the key name, e.g. `\##?#USB#VID_2573&PID_004A#no_serial_number#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\<key-name-ignored>\Device Parameters\FriendlyName`. If your MIDI device stores "FriendlyName" in a deeper key structure, you must change the source code and compile your own version.

## Other uses
Despite the "MIDI" name, this program can also rename other device types below "CurrentControlSet", given that the "FriendlyName" can be found at the same key level as typical for MIDI devices.
