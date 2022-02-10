using System;
using System.IO;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{

    class StorageAbstraction
    {
        static object MtpDeviceLock = new object();

        public static Task WriteFile(MemoryStream stream, string path)
        {
            return Task.Run(async () =>
            {
                if (DirectoryExists(Path.GetDirectoryName(path)))
                {
                    if (File.Exists(path)) await DeleteFile(path);

                    stream.Seek(0, SeekOrigin.Begin);

                    if (MtpDevice.connected)
                    {
                        try
                        {
                            lock (MtpDeviceLock)
                            {
                                MtpDevice.device.UploadFile(stream, Path.Combine(MtpDevice.path, path));
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error writing to " + path + ": " + e.Message);
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
                }
            });
        }

        public static Stream ReadFile(string path)
        {
            if (MtpDevice.connected)
            {
                lock (MtpDeviceLock)
                {
                    Stream ms = new MemoryStream();
                    MtpDevice.device.DownloadFile(Path.Combine(MtpDevice.path, path), ms);
                    return ms;
                }
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
                lock (MtpDeviceLock)
                {
                    return MtpDevice.device.FileExists(Path.Combine(MtpDevice.path, path));
                }
            }
            else
            {
                return File.Exists(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
            }
        }

        public static Task DeleteFile(string path)
        {
            return Task.Run(() =>
            {
                if (FileExists(path))
                {
                    if (MtpDevice.connected)
                    {
                        lock (MtpDeviceLock)
                        {
                            MtpDevice.device.DeleteFile(Path.Combine(MtpDevice.path, path));
                        }
                    }
                    else
                    {
                        File.Delete(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
                    }
                }
            });
        }

        public static bool DirectoryExists(string path)
        {
            if (MtpDevice.connected)
            {
                lock (MtpDeviceLock)
                {
                    return MtpDevice.device.DirectoryExists(Path.Combine(MtpDevice.path, path));
                }
            }
            else
            {
                return Directory.Exists(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
            }
        }

        public static string[] GetFilesInDirectory(string path)
        {
            Console.WriteLine("getting files in " + path);
            if (DirectoryExists(path))
            {
                if (MtpDevice.connected)
                {
                    lock (MtpDeviceLock)
                    {
                        return MtpDevice.device.GetFiles(Path.Combine(MtpDevice.path, path));
                    }
                }
                else
                {
                    return Directory.GetFiles(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
                }
            }
            else return new string[0];
        }

        public static DateTime GetLastWriteTime(string path)
        {
            if (FileExists(path))
            {
                if (MtpDevice.connected)
                {
                    lock (MtpDeviceLock)
                    {
                        return MtpDevice.device.GetFileInfo(Path.Combine(MtpDevice.path, path)).LastWriteTime.Value;
                    }
                }
                else
                {
                    return File.GetLastWriteTime(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path));
                }
            }
            else return new DateTime();
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
