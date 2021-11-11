using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    static class DownloadScheduler
    {
        public static ObservableCollection<GenericItem> queue = new ObservableCollection<GenericItem>();
        public static ObservableCollection<GenericItem> downloading = new ObservableCollection<GenericItem>();

        public const int downloadTasks = 4;

        private static bool running = false;

        static DownloadScheduler()
        {
            queue.CollectionChanged += QueueChanged;
        }

        public static void Download(GenericItem item)
        {
            item.downloadAttempts = 0;
            queue.Add(item);
        }

        private static void QueueChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (queue.Count > 0 && !running)
            {
                running = true;
                MainViewModel.Log("Started queue check");
                Task.Run(async () =>
                {
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
                        }
                        else
                        {
                            await Task.Delay(500);
                        }
                    }
                    running = false;
                    MainViewModel.Log("Stopped queue check");
                });
            }
        }
    }
}
