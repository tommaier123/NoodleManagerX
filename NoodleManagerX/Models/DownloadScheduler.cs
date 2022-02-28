using System;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    static class DownloadScheduler
    {
        public static ObservableCollection<GenericItem> queue = new ObservableCollection<GenericItem>();
        public static ObservableCollection<GenericItem> downloading = new ObservableCollection<GenericItem>();

        public const int downloadTasks = 4;
        public const int maxDownloadAttempts = 3;
        public static int toDownload = 0;

        private static bool running = false;

        static DownloadScheduler()
        {
            queue.CollectionChanged += QueueChanged;
        }

        public static void Download(GenericItem item)
        {
            if (item.itemType == ItemType.Map && !item.blacklisted)
            {
                toDownload++;
                item.downloadAttempts = 1;
                queue.Add(item);
            }
        }

        public static void Requeue(GenericItem item)
        {
            if (item.downloadAttempts < maxDownloadAttempts)
            {
                MainViewModel.Log("Requeueing " + item.filename);
                item.downloadAttempts++;
                queue.Add(item);
            }
            else
            {
                MainViewModel.Log("Timeout " + item.filename);
            }
        }

        public static void Remove(GenericItem item)
        {
            downloading.Remove(item);
            if (downloading.Count == 0 && !MainViewModel.s_instance.closing)//if the database is saved while closing it gets corrupted
            {
                _ = MainViewModel.s_instance.SaveLocalItems();
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainViewModel.s_instance.progress = 0;
                    MainViewModel.s_instance.progressText = null;
                });
            }
            else
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainViewModel.s_instance.progress = Math.Min(100 - (int)((queue.Count + downloading.Count) / (toDownload * 0.01f)), 1);
                    MainViewModel.s_instance.progressText = "Downloading: " + MainViewModel.s_instance.progress + "% (" + (toDownload - queue.Count - downloading.Count) + "/" + toDownload + ")";
                });
            }
        }

        private static void QueueChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (queue.Count > 0 && !running)
                {
                    running = true;
                    await Task.Delay(100);//not sure why this is necessary for requeueing. Seems like queuecount is !=0 but the queue is empty?
                    MainViewModel.Log("Started queue check");
                    while (queue.Count > 0)
                    {
                        if (downloading.Count < downloadTasks)
                        {
                            if (queue[0] != null && !queue[0].downloading)
                            {
                                _ = Task.Run(queue[0].Download);
                                downloading.Add(queue[0]);
                            }
                            queue.Remove(queue[0]);


                        }
                        else
                        {
                            await Task.Delay(500);
                        }
                    }
                    running = false;
                    MainViewModel.Log("Stopped queue check");
                }
            });
        }
    }
}
