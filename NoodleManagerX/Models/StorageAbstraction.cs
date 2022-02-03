using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{

    class StorageAbstraction
    {

        public static void WriteFile(Stream stream, string path)
        {
            Task.Run(async () =>
            {
                if (MtpDevice.connected)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        MtpDevice.device.UploadFile(ms, Path.Combine(MtpDevice.path, path));
                    }
                }
                else
                {
                    using (FileStream file = new FileStream(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path), FileMode.Create, FileAccess.Write))
                    {
                        await stream.CopyToAsync(file);
                    }
                }
                stream.Close();
            });
        }

        public static Stream ReadFile(string path)
        {
            if (MtpDevice.connected)
            {
                Stream ms = new MemoryStream();
                MtpDevice.device.DownloadFile(Path.Combine(MtpDevice.path, path), ms);
                return ms;
            }
            else
            {
                return new FileStream(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path), FileMode.Open, FileAccess.Read);
            }
        }

        public static bool DirectoryExists(string path)
        {
            if (MtpDevice.connected)
            {
                return MtpDevice.device.DirectoryExists(Path.Combine(MtpDevice.path, path));
            }
            else
            {
                return Directory.Exists(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
            }
        }
    }
}
