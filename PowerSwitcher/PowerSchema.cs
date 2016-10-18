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
        public string Name{get{return name;} set { name = value; RaisePropertyChangedEvent(nameof(Name)); } }

        bool isActive;
        public bool IsActive { get { return isActive; } set { isActive = value; RaisePropertyChangedEvent(nameof(IsActive)); } }

        public PowerSchema(string name, Guid guid) : this(name, guid, false) { }

        public PowerSchema(string name, Guid guid, bool isActive)
        {
            this.Name = name;
            this.Guid = guid;
            this.IsActive = isActive;
        }
    }
}
