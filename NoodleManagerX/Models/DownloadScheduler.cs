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

        private static int queueCountBefore = 0;

        static DownloadScheduler()
        {
            queue.CollectionChanged += QueueChanged;
        }

        private static void QueueChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (queue.Count > 0 && queueCountBefore == 0)
            {
                MainViewModel.Log("Started queue check");
                Task.Run(async () =>
                {
                    while (queue.Count > 0)
                    {
                        if (queue.Count > 0 && downloading.Count < downloadTasks)
                        {
                            if (queue[0] != null && !queue[0].downloaded && !queue[0].downloading)
                            {
                                queue[0].Download();
                                downloading.Add(queue[0]);
                            }
                            queue.Remove(queue[0]);
                        }
                        await Task.Delay(500);
                    }
                    MainViewModel.Log("Stopped queue check");
                });
            }
            queueCountBefore = queue.Count;
        }
    }
}
