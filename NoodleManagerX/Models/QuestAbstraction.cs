using System;
using System.Runtime.InteropServices;
using System.Linq;
using MediaDevices;
using SharpAdbClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Threading;
using MsgBox;
using System.Diagnostics;
using System.Net;

namespace NoodleManagerX.Models
{
    static class QuestAbstraction
    {
        private static string AdbPath = "";

        private static AdbServer adbServer = new AdbServer();
        private static AdbClient adbClient = new AdbClient();
        private static DeviceMonitor deviceMonitor;
        private static string questSerial { get; set; } = "";

        //reload local items when connection status changes


        static QuestAbstraction()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var devices = MediaDevice.GetDevices();
                Console.WriteLine(devices.Count() + " devices connected");
                foreach (MediaDevice device in devices)
                {
                    try
                    {
                        device.Connect();
                        var directories = device.GetDirectories(@"\");
                        foreach (string directory in directories)
                        {
                            Console.WriteLine(directory);
                        }
                        device.Disconnect();
                    }
                    catch
                    {
                        // If it can't be read, don't worry.
                    }
                }
            }
        }

        public static void SetAdbPath(string path)
        {
            AdbPath = path;
            StartAdbServer(path);
        }

        private static void StartAdbServer(string path)
        {
            Task.Run(() =>
            {
                try
                {
                    MainViewModel.Log("Looking for adb executable at " + path);
                    var result = adbServer.StartServer(path, restartServerIfNewer: false);
                    MainViewModel.Log("Adb server " + result);

                    deviceMonitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
                    deviceMonitor.DeviceConnected += OnAdbDeviceConnected;
                    deviceMonitor.DeviceDisconnected += OnAdbDevicDisconnected;
                    deviceMonitor.Start();
                }
                catch (Exception e)
                {
                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                }
            });
        }

        private static void OnAdbDeviceConnected(object sender, DeviceDataEventArgs e)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                List<DeviceData> devices = adbClient.GetDevices();
                foreach (DeviceData device in devices)
                {
                    if (device.Serial == e.Device.Serial)
                    {
                        //Log(device.Product + " " + device.Model + " " + device.Name + " " + device.Serial);

                        if (device.Product == "vr_monterey")
                        {
                            if (questSerial != device.Serial)
                            {
                                if (questSerial == "")
                                {
                                    questSerial = device.Serial;
                                    MainViewModel.Log("Quest connected " + questSerial);
                                }
                                else
                                {
                                    MainViewModel.Log("Multiple quests connected");
                                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        _ = MessageBox.Show(MainWindow.s_instance, "There are multiple quests connected." + Environment.NewLine + "Only one can be used at a time.", "Warning", MessageBox.MessageBoxButtons.Ok);
                                    });
                                }
                            }
                        }
                    }
                }
            });
        }

        private static void OnAdbDevicDisconnected(object sender, DeviceDataEventArgs e)
        {
            Task.Run(() =>
            {
                if (questSerial == e.Device.Serial)
                {
                    MainViewModel.Log("Quest disconnected " + questSerial);
                    questSerial = "";
                }
            });
        }

        private static List<string> AdbDirectoryGetFiles(string path)
        {
            List<string> ret = new List<string>();
            if (AdbPathExists(path))
            {

                var quests = adbClient.GetDevices().Where(x => x.Serial == questSerial);
                if (quests.Count() > 0)
                {
                    DeviceData device = quests.First();

                    var receiver = new ConsoleOutputReceiver();

                    adbClient.ExecuteRemoteCommand("ls sdcard/Android/data/com.kluge.SynthRiders/files/" + path, device, receiver);
                    ret.AddRange(receiver.ToString().Split(new char[] { '\n' }));
                }
            }
            return ret;
        }

        private static bool AdbPathExists(string path)
        {
            List<string> ret = new List<string>();
            var quests = adbClient.GetDevices().Where(x => x.Serial == questSerial);
            if (quests.Count() > 0)
            {
                DeviceData device = quests.First();

                var receiver = new ConsoleOutputReceiver();

                adbClient.ExecuteRemoteCommand("ls sdcard/Android/data/com.kluge.SynthRiders/files", device, receiver);

                return receiver.ToString().Contains(path);
            }
            return false;
        }
    }
}
