using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using static System.Environment;

namespace UpdateHelper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length >= 1)
                {
                    bool isElevated;
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    {
                        WindowsPrincipal principal = new WindowsPrincipal(identity);
                        isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                    }
                    if (isElevated) Log("Running as administrator");
                    else Log("Running as user");

                    string targetPath = args[0].Trim('\"');

                    string tmpPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "NoodleManagerX.exe");
                    Log("Updating from " + tmpPath + " to " + targetPath);

                    bool canWrite = false;
                    int attempts = 0;
                    while (!canWrite && attempts < 20)
                    {
                        using (var fs = new FileStream(targetPath, FileMode.Open))
                        {
                            canWrite = fs.CanWrite;
                        }
                        if (!canWrite)
                        {
                            Thread.Sleep(200);
                            attempts++;
                        }
                    }

                    File.Move(tmpPath, targetPath, true);

                    Process proc = new Process();
                    proc.StartInfo.FileName = targetPath;
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();

                    Log("Updated successful");
                }
            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
        }

        public static void Log(MethodBase m, Exception e)
        {
            Log("Error " + m.Name + " " + e.Message + Environment.NewLine + e.TargetSite + Environment.NewLine + e.StackTrace);
        }

        public static void Log(string message)
        {
            try
            {
                Console.WriteLine(message);

                string directory = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManagerX");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (StreamWriter sw = File.AppendText(Path.Combine(directory, "UpdateLog.txt")))
                {
                    sw.Write(DateTime.Now.ToString("dd'.'MM HH':'mm':'ss") + "     " + message + Environment.NewLine);
                }
            }
            catch { }
        }
    }
}
