using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    [DataContract]
    class Settings : ReactiveObject
    {
        [Reactive][DataMember] public string synthDirectory { get; set; } = "";
        [Reactive][DataMember] public bool skipDirectoryCheck { get; set; } = false;
        [Reactive][DataMember] public bool allowConverts { get; set; } = false;
        [Reactive][DataMember] public bool ignoreUpdates { get; set; } = false;
        [Reactive][DataMember] public bool getBetas { get; set; } = false;
        [Reactive][DataMember] public int previewVolume { get; set; } = 50;
    }
}
