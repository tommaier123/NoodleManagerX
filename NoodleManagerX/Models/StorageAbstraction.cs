using System;
using System.IO;
using System.Linq;
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
                if (await DirectoryExists(Path.GetDirectoryName(path)))
                {
                    if (await FileExists(path))
                    {
                        await DeleteFile(path);
                    }

                    stream.Position = 0;

                    if (MtpDevice.connected)
                    {//remove temp file once media devices works properely with memory stream
                        string tempFile = Path.GetTempFileName();
                        using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite))
                        {
                            await stream.CopyToAsync(fs);
                            fs.Position = 0;

                            lock (MtpDeviceLock)
                            {
                                MtpDevice.device.UploadFile(fs, Path.Combine(MtpDevice.path, path));
                            }
                        }
                        try { File.Delete(tempFile); } catch { }//really doesn't matter if something goes wrong, windows will clean temp files automatically
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

        public static Task<Stream> ReadFile(string path)
        {
            return Task.Run(() =>
            {
                if (MtpDevice.connected)
                {
                    lock (MtpDeviceLock)
                    {
                        Stream ms = new MemoryStream();
                        MtpDevice.device.DownloadFile(Path.Combine(MtpDevice.path, path), ms);
                        ms.Position = 0;
                        return ms;
                    }
                }
                else
                {
                    return new FileStream(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path), FileMode.Open, FileAccess.Read);
                }
            });
        }

        public static Task<bool> FileExists(string path)
        {
            return Task.Run(() =>
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
            });
        }

        public static Task DeleteFile(string path)
        {
            return Task.Run(() =>
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
            });
        }

        public static Task<bool> DirectoryExists(string path)
        {
            return Task.Run(() =>
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
            });
        }

        public static Task<string[]> GetFilesInDirectory(string path)
        {
            return Task.Run(async () =>
            {
                if (await DirectoryExists(path))
                {
                    if (MtpDevice.connected)
                    {
                        lock (MtpDeviceLock)
                        {
                            return MtpDevice.device.GetFiles(Path.Combine(MtpDevice.path, path)).Select(x => Path.Combine(path, Path.GetFileName(x))).ToArray();
                        }
                    }
                    else
                    {
                        return Directory.GetFiles(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path)).Select(x => Path.Combine(path, Path.GetFileName(x))).ToArray();
                    }
                }
                else return new string[0];
            });
        }

        public static Task<DateTime> GetLastWriteTime(string path)
        {
            return Task.Run(() =>
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
            });
        }

        public static Task SetLastWriteTime(DateTime timestamp, string path)
        {
            return Task.Run(() =>
            {
                if (MtpDevice.connected)
                {
                    //no idea how to change timestamp over mtp
                }
                else
                {
                    File.SetLastWriteTime(Path.Combine(MainViewModel.s_instance.settings.synthDirectory, path), timestamp);
                }
            });
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
