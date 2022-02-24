using System;
using System.Diagnostics;
using System.IO;

namespace UpdateHelper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                string oldPath = Path.Combine(args[0], "NoodleManagerX.exe");
                string tmpPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "NoodleManagerX.exe");
                File.Move(tmpPath, oldPath, true);

                Process proc = new Process();
                proc.StartInfo.FileName = oldPath;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
        }
    }
}
