using System;
using System.Runtime.InteropServices;
using System.Linq;
using MediaDevices;
using Path = System.IO.Path;
using System.Threading.Tasks;

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
                        string dir = Path.Combine(directory, "SynthRidersUC");
                        if (d.DirectoryExists(dir))
                        {
                            MainViewModel.Log("Synth Riders device found " + d.Description);
                            device = d;
                            path = dir;
                            connected = true;
                            d.DeviceRemoved += DeviceRemoved;
                            return;
                        }
                    }
                    d.Disconnect();
                }
                catch { }
            }
        }

        private static void DeviceRemoved(object sender, MediaDeviceEventArgs e)
        {
            connected = false;
            MainViewModel.s_instance.ReloadLocalSources();
        }

        public static void Disconnect()
        {
            if (connected) device.Disconnect();
        }
    }
}
