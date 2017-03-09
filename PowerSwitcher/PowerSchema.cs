using Petrroll.Helpers;
using System;
using System.ComponentModel;

namespace PowerSwitcher
{
    public enum PowerPlugStatus { Online, Offline }

    public interface IPowerSchema : INotifyPropertyChanged
    {
        string Name { get; }
        Guid Guid { get; }
        bool IsActive { get; }
    }

    public class PowerSchema : ObservableObject, IPowerSchema
    {
        public Guid Guid { get; }

        string name;
        public string Name{get{return name;} set { if (name == value) { return; } name = value; RaisePropertyChangedEvent(nameof(Name)); } }

        bool isActive;
        public bool IsActive {
            get { return isActive; }
            set { if (isActive == value) { return; } isActive = value; RaisePropertyChangedEvent(nameof(IsActive)); }
        }

        public PowerSchema(string name, Guid guid) : this(name, guid, false) { }

        public PowerSchema(string name, Guid guid, bool isActive)
        {
            this.Name = name;
            this.Guid = guid;
            this.IsActive = isActive;
        }

        public override string ToString()
        {
            return $"{Name}:{IsActive}";
        }
    }
}
