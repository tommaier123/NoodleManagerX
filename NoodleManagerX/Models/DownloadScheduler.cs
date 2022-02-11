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
        public const int maxDownloadAttempts = 3;//1 fore debugging
        public static int toDownload = 0;

        private static bool running = false;

        static DownloadScheduler()
        {
            queue.CollectionChanged += QueueChanged;
        }

        public static void Download(GenericItem item)
        {
            if (item.itemType == ItemType.Map)
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
                                queue[0].Download();
                                downloading.Add(queue[0]);
                            }
                            queue.Remove(queue[0]);

                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                MainViewModel.s_instance.progress = 100 - (int)((queue.Count) / (toDownload * 0.01f));
                            });
                        }
                        else
                        {
                            await Task.Delay(500);
                        }
                    }
                    running = false;
                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        MainViewModel.s_instance.progress = 0;
                    });
                    MainViewModel.Log("Stopped queue check");
                }
            });
        }
    }
}
