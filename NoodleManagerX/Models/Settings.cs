using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NoodleManagerX.Models
{
    [DataContract]
    class Settings : ReactiveObject
    {
        [Reactive] [DataMember] public string synthDirectory { get; set; } = "";
    }
}
