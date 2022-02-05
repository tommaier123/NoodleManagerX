using System;
using System.Runtime.InteropServices;
using System.Linq;
using MediaDevices;
using System.IO;

namespace NoodleManagerX.Models
{
    static class MtpDevice
    {
        public static MediaDevice device;
        public static string path = "";
        public static bool connected = false;

        public static void Connect()
        {
            var devices = MediaDevice.GetDevices();
            MainViewModel.Log(devices.Count() + " mtp devices connected");
            foreach (MediaDevice d in devices)
            {
                try
                {
                    d.Connect();
                    var directories = d.GetDirectories(@"\");
                    foreach (string directory in directories)
                    {
                        var subdirectories = d.GetDirectories(directory);
                        foreach (string subsubdirectory in subdirectories)
                        {
                            if (Path.GetFileName(subsubdirectory) == "SynthRidersUC")
                            {
                                MainViewModel.Log("Synth Riders device found " + d.Description);
                                device = d;
                                path = subsubdirectory;
                                connected = true;
                                return;
                            }
                        }
                    }
                    d.Disconnect();
                }
                catch { }
            }
        }
    }
}
