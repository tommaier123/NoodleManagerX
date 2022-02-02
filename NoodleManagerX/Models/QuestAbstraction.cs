using System;
using System.Runtime.InteropServices;
using System.Linq;
using MediaDevices;

namespace NoodleManagerX.Models
{
    static class QuestAbstraction
    {

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
    }
}
