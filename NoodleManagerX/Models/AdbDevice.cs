using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    internal class AdbDevice
    {
        /*
       public void ExtractResources()
       {
           Task.Run(() =>
           {
               try
               {
                   string platform = "linux";
                   string extension = "";

                   if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                   {
                       platform = "windows";
                       extension = ".exe";
                   }
                   else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                   {
                       platform = "osx";
                   }

                   string location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                   string resources = Path.Combine(location, "Resources");
                   string adbExe = Path.Combine(resources, "ADB", "adb" + extension);

                   using (Stream resourceFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("NoodleManagerX.Resources." + platform + ".zip"))
                   using (ZipArchive archive = new ZipArchive(resourceFile))
                   {
                       string newVersion = "";
                       string oldVersion = "0";
                       ZipArchiveEntry versionEntry = archive.GetEntry(@"Resources/version.txt");

                       if (versionEntry != null)
                       {
                           using (StreamReader sr = new StreamReader(versionEntry.Open()))
                           {
                               newVersion = sr.ReadToEnd();
                           }
                       }
                       string versionFile = Path.Combine(resources, "version.txt");
                       if (File.Exists(versionFile))
                       {
                           using (StreamReader sr = new StreamReader(versionFile))
                           {
                               oldVersion = sr.ReadToEnd();
                           }
                       }

                       bool update = oldVersion != newVersion && newVersion != "";

                       if (update)
                       {
                           Log("Updating resources from version " + oldVersion + " to " + newVersion + " at " + location);
                           bool writeable = true;
                           if (Directory.Exists(resources))
                           {
                               try//check if all files can be opened
                               {
                                   foreach (string file in Directory.GetFiles(resources, "*", SearchOption.AllDirectories))
                                   {
                                       using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite))
                                       {
                                           stream.Close();
                                       }
                                   }
                                   Directory.Delete(resources, true);
                               }
                               catch (Exception e)
                               {
                                   writeable = false;
                                   MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                               }
                           }
                           if (writeable)
                           {
                               foreach (ZipArchiveEntry entry in archive.Entries)
                               {
                                   if (!Path.EndsInDirectorySeparator(entry.FullName))
                                   {
                                       string path = Path.GetFullPath(Path.Combine(location, entry.FullName));
                                       string directory = Path.GetDirectoryName(path);
                                       Directory.CreateDirectory(directory);
                                       entry.ExtractToFile(path, true);
                                   }
                               }
                           }
                       }
                       if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                       {
                           Shell("chmod 755 " + adbExe);
                       }
                   }
                   StartAdbServer(adbExe);
               }
               catch (Exception e)
               {
                   MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
               }
           });
       }

       public void StartAdbServer(string path)
       {
           Task.Run(() =>
           {
               try
               {
                   Log("Looking for adb executable at " + path);
                   var result = adbServer.StartServer(path, restartServerIfNewer: false);
                   MainViewModel.Log("Adb server " + result);

                   deviceMonitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
                   deviceMonitor.DeviceConnected += this.OnDeviceConnected;
                   deviceMonitor.DeviceDisconnected += this.OnDevicDisconnected;
                   deviceMonitor.Start();
               }
               catch (Exception e)
               {
                   MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
               }
           });
       }

       public string Shell(string cmd)
       {
           var escapedArgs = cmd.Replace("\"", "\\\"");

           var process = new Process()
           {
               StartInfo = new ProcessStartInfo
               {
                   FileName = "/bin/sh",
                   Arguments = $"-c \"{escapedArgs}\"",
                   RedirectStandardOutput = true,
                   UseShellExecute = false,
                   CreateNoWindow = true,
               }
           };
           process.Start();
           string result = process.StandardOutput.ReadToEnd();
           process.WaitForExit();
           return result;
       }

       private void OnDeviceConnected(object sender, DeviceDataEventArgs e)
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
                                   Log("Quest connected " + questSerial);
                               }
                               else
                               {
                                   Log("Multiple quests connected");
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

       private void OnDevicDisconnected(object sender, DeviceDataEventArgs e)
       {
           Task.Run(() =>
           {
               if (questSerial == e.Device.Serial)
               {
                   Log("Quest disconnected " + questSerial);
                   questSerial = "";
               }
           });
       }

               public static List<string> QuestDirectoryGetFiles(string path)
       {
           List<string> ret = new List<string>();
           if (QuestPathExists(path))
           {

               var quests = MainViewModel.s_instance.adbClient.GetDevices().Where(x => x.Serial == MainViewModel.s_instance.questSerial);
               if (quests.Count() > 0)
               {
                   DeviceData device = quests.First();

                   var receiver = new ConsoleOutputReceiver();

                   MainViewModel.s_instance.adbClient.ExecuteRemoteCommand("ls sdcard/Android/data/com.kluge.SynthRiders/files/" + path, device, receiver);
                   ret.AddRange(receiver.ToString().Split(new char[] { '\n' }));
               }
           }
           return ret;
       }

       public static bool QuestPathExists(string path)
       {
           List<string> ret = new List<string>();
           var quests = MainViewModel.s_instance.adbClient.GetDevices().Where(x => x.Serial == MainViewModel.s_instance.questSerial);
           if (quests.Count() > 0)
           {
               DeviceData device = quests.First();

               var receiver = new ConsoleOutputReceiver();

               MainViewModel.s_instance.adbClient.ExecuteRemoteCommand("ls sdcard/Android/data/com.kluge.SynthRiders/files", device, receiver);

               return receiver.ToString().Contains(path);
           }
           return false;
       }
*/
    }
}
