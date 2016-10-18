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

    public class PowerSchema : IPowerSchema, INotifyPropertyChanged
    {
        public Guid Guid { get; }

        string name;
        public string Name{get{return name;} set { name = value; invokePropertyChanged(nameof(Name)); } }

        bool isActive;
        public bool IsActive { get { return isActive; } set { isActive = value; invokePropertyChanged(nameof(IsActive)); } }

        public PowerSchema(string name, Guid guid) : this(name, guid, false) { }

        public PowerSchema(string name, Guid guid, bool isActive)
        {
            this.Name = name;
            this.Guid = guid;
            this.IsActive = isActive;
        }

        private void invokePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
