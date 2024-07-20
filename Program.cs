
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MidiRenamer
{
    internal class Program
    {
        static string deviceClassesRootPath = @"SYSTEM\CurrentControlSet\Control\DeviceClasses\";
        static RegistryKey rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        static int deviceNamesReplacedCount = 0;
        static int deviceNamesAlreadyReplacedCount = 0;

        static void Main(string[] args)
        {
            MidiRenamerSettings settings = parseArgs(args);
            string[] deviceClasses = rootKey.OpenSubKey(deviceClassesRootPath).GetSubKeyNames();

            settings.ConfigFiles.ForEach(configFile =>
            {
                MidiDeviceRenameConfig renameConfig = loadConfigXml(configFile);
                if (renameConfig == null)
                {
                    return;
                }

                foreach (string midiDevicePath in getMidiDevicesPath(ref deviceClasses, renameConfig.DevicePartialId))
                {
                    foreach (string folder in rootKey.OpenSubKey(midiDevicePath).GetSubKeyNames())
                    {
                        string searchPath = Path.Combine(midiDevicePath, folder, "Device Parameters");
                        RegistryKey key = rootKey.OpenSubKey(searchPath, true);

                        if (key != null && key.GetValue("FriendlyName") != null)
                        {
                            string friendlyName = key.GetValue("FriendlyName").ToString();
                            foreach (string[] valueMap in renameConfig.ValueMaps)
                            {
                                if (friendlyName.Contains(valueMap[0]))
                                {
                                    if (!settings.TestMode)
                                    {
                                        key.SetValue("FriendlyName", valueMap[1]);
                                    }
                                    deviceNamesReplacedCount++;
                                }
                                else if (friendlyName.Contains(valueMap[1]))
                                {
                                    deviceNamesAlreadyReplacedCount++;
                                }
                            }
                        }
                    }
                }
            });

            Console.WriteLine(deviceNamesReplacedCount + " device names replaced");
            if (deviceNamesAlreadyReplacedCount > 0)
            {
                Console.WriteLine(deviceNamesAlreadyReplacedCount + " device names already OK");
            }
            System.Threading.Thread.Sleep(settings.WaitBeforeExit);
        }

        static List<string> getMidiDevicesPath(ref string[] deviceClasses, string devicePartialId)
        {
            List<string> midiDevices = new List<string>();
            foreach (string deviceClass in deviceClasses)
            {
                string[] devices = rootKey.OpenSubKey(Path.Combine(deviceClassesRootPath, deviceClass)).GetSubKeyNames();
                midiDevices.AddRange(devices.Where((device) => { return device.Contains(devicePartialId); }).ToArray().Select(midiDevice => { return Path.Combine(deviceClassesRootPath, deviceClass, midiDevice); }).ToArray());
            }
            if (midiDevices.Count == 0)
            {
                writeError("No device with partial name " + devicePartialId + " could be found");
            }
            else
            {
                Console.WriteLine("Matching devices found: " + midiDevices.Count);
            }
            return midiDevices;
        }

        static MidiRenamerSettings parseArgs(string[] args)
        {
            List<string> configFiles = new List<string>();
            bool testMode = false;
            int waitBeforeExit = 0;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-test":
                        testMode = true;
                        break;
                    case "-waitBeforeExit":
                        try
                        {
                            i++;
                            waitBeforeExit = int.Parse(args[i]);
                        }
                        catch
                        {
                            writeError("Argument '-waitBeforeExit' must be followed by an integer", true);
                        }
                        break;
                    default:
                        configFiles.Add(args[i]);
                        break;
                }
            }

            if (configFiles.Count == 0) 
            {
                configFiles.Add("MidiRenamerConfig.xml");
            }
            if(testMode)
            {
                Console.WriteLine("Test mode enabled - No changes will be written to the registry");
            }

            return new MidiRenamerSettings(testMode, waitBeforeExit, configFiles);
        }

        static MidiDeviceRenameConfig loadConfigXml(string configFileName)
        {
            if(!File.Exists(configFileName)) 
            {
                writeError("Config file '"+ configFileName + "' not found");
                return null;
            }

            try
            {
                XDocument configXml = XDocument.Parse(File.ReadAllText(configFileName));

                string devicePartialId = configXml.Root.Attributes().First(attributes => attributes.Name == "device-partial-name").Value;

                List<string[]> valueMaps = configXml.Root.Elements().Where(element => element.Name == "rename").Select(element =>
                {
                    return new string[] { element.Element("from").Value, element.Element("to").Value };
                }).ToList();

                return new MidiDeviceRenameConfig(valueMaps, devicePartialId);
            }
            catch
            {
                writeError("Config file  '"+ configFileName + "' could not be parsed");
                return null;
            }
        }

        static void writeError(string errorText, bool exit = false)
        {
            Console.WriteLine("ERROR: "+ errorText);
            if (exit)
            {
                Environment.Exit(0);
            }
        }
    }

    class MidiDeviceRenameConfig
    {
        public List<string[]> ValueMaps;
        public string DevicePartialId;

        public MidiDeviceRenameConfig(List<string[]> valueMaps, string devicePartialId)
        {
            ValueMaps = valueMaps;
            DevicePartialId = devicePartialId;
        }
    }

    class MidiRenamerSettings
    {
        public bool TestMode = false;
        public int WaitBeforeExit = 0;
        public List<string> ConfigFiles;

        public MidiRenamerSettings(bool testMode, int waitBeforeExit, List<string> configFiles)
        {
            TestMode = testMode;
            WaitBeforeExit = waitBeforeExit;
            ConfigFiles = configFiles;
        }
    }
}
