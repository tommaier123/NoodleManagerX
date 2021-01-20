using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NoodleManagerX.Models
{
    static class DownloadScheduler
    {
        public static ObservableCollection<GenericItem> queue;

        public static int downloading = 0;

        static DownloadScheduler()
        {

        }
    }
}
