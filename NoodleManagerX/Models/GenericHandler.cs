using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;


namespace NoodleManagerX.Models
{
    class GenericHandler
    {
        public virtual ItemType itemType { get; set; }

        public const int pagecount = 6;
        public const int pagesize = 10;

        public virtual void LoadLocalItems() { }

        public void GetPage(bool download = false)
        {
            Clear();
            Task.Run(() => PageTaskFunction(MainViewModel.s_instance.apiRequestCounter, download));

        }

        public void GetAll()
        {
            Task.Run(() => GetAllTaskFunction());
        }

        public virtual async void PageTaskFunction(int requestID, bool download = false) { }

        public virtual async void GetAllTaskFunction() { }

        public void Clear()
        {
            var tmp = MainViewModel.s_instance.items.Where(x => x.itemType != itemType);
            MainViewModel.s_instance.items.Clear();
            MainViewModel.s_instance.items.Add(tmp);
        }
    }
}
