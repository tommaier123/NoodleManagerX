using MediaDevices;
using System;
using System.Linq;
using Path = System.IO.Path;

namespace NoodleManagerX.Models
{
    static class MtpDevice
    {
        public static MediaDevice device;
        public static string path = "";
        public static bool connected = false;

        public static void Connect(bool isCommand = false)
        {
            if (!connected)
            {
                var devices = MediaDevice.GetDevices();
                MainViewModel.Log(devices.Count() + " mtp devices connected");
                foreach (MediaDevice d in devices)
                {
                    try
                    {
                        d.Connect();
                        string[] directories = d.GetDirectories(@"\");
                        foreach (string directory in directories)
                        {
                            string dir = Path.Combine(directory, "SynthRidersUC");
                            if (d.DirectoryExists(dir))
                            {
                                MainViewModel.Log("Synth Riders device found " + d.FriendlyName);
                                device = d;
                                path = dir;
                                connected = true;
                                MainViewModel.s_instance.questConnected = true;
                                d.DeviceRemoved += DeviceRemoved;
                                if (isCommand) MainViewModel.s_instance.ReloadLocalSources();
                                return;
                            }
                        }
                        d.Disconnect();
                    }
                    catch { }
                }
                if (isCommand)
                {
                    if (devices.Count() > 0)
                    {
                        MainViewModel.s_instance.OpenErrorDialog("Found " + devices.Count() + " MTP devices but none of them had the SynthRidersUC folder:" + Environment.NewLine + String.Join(Environment.NewLine, devices.Select(x => x.FriendlyName + " (" + x.Description + ")")) + Environment.NewLine + "If your device is not listed above make sure to allow storage access on the headset");
                    }
                    else
                    {
                        MainViewModel.s_instance.OpenErrorDialog("No MTP devices found" + Environment.NewLine + "Make sure to allow storage access on the headset");
                    }
                }
            }
        }

        private static void DeviceRemoved(object sender, MediaDeviceEventArgs e)
        {
            Disconnected();
        }

        private static void Disconnected()
        {
            MainViewModel.Log("Quest disconnected");
            device.DeviceRemoved -= DeviceRemoved;
            connected = false;
            MainViewModel.s_instance.questConnected = false;
            MainViewModel.s_instance.ReloadLocalSources();
        }

        public static void Disconnect()
        {
            if (connected)
            {
                device.Disconnect();
                if (!MainViewModel.s_instance.closing) Disconnected();
            }
        }
    }
}
