using System;
using System.IO;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    //toDo
    //prevent write while busy

    class StorageAbstraction
    {
        public static Task WriteFile(MemoryStream stream, string path)
        {
            return Task.Run(async () =>
            {
                if (File.Exists(path)) await DeleteFile(path);

                stream.Seek(0, SeekOrigin.Begin);

                if (MtpDevice.connected)
                {
                    await stream.CopyToAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    MtpDevice.device.UploadFile(stream, Path.Combine(MtpDevice.path, path));
                }
                else
                {
                    using (FileStream file = new FileStream(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path), FileMode.Create, FileAccess.Write))
                    {
                        await stream.CopyToAsync(file);
                    }
                }
                stream.Close();

                while (!File.Exists(path))
                {
                    await Task.Delay(100);
                }
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

        public static bool FileExists(string path)
        {
            if (MtpDevice.connected)
            {
                return MtpDevice.device.FileExists(Path.Combine(MtpDevice.path, path));
            }
            else
            {
                return File.Exists(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
            }
        }

        public static Task DeleteFile(string path)
        {
            return Task.Run(async () =>
            {
                if (MtpDevice.connected)
                {
                    MtpDevice.device.DeleteFile(Path.Combine(MtpDevice.path, path));
                }
                else
                {
                    File.Delete(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
                }
                while (FileExists(path))
                {
                    await Task.Delay(100);
                }
            });
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

        public static string[] GetFilesInDirectory(string path)
        {
            Console.WriteLine("getting files in " + path);
            if (MtpDevice.connected)
            {
                return MtpDevice.device.GetFiles(Path.Combine(MtpDevice.path, path));
            }
            else
            {
                return Directory.GetFiles(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
            }
        }

        public static DateTime GetLastWriteTime(string path)
        {
            if (MtpDevice.connected)
            {
                return MtpDevice.device.GetFileInfo(Path.Combine(MtpDevice.path, path)).LastWriteTime.Value;
            }
            else
            {
                return File.GetLastWriteTime(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
            }
        }

        public static bool CanDownload(bool silent = false)
        {
            if (MtpDevice.connected) return true;
            else
            {
                return MainViewModel.s_instance.CheckDirectory(MainViewModel.s_instance.settings.synthDirectory, !silent);
            }
        }
    }
}
