using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PowerSwitcher
{
    public enum PowerPlugStatus { Online, Offline }

    public interface IPowerSchema
    {
        string Name { get; }
        Guid Guid { get; }
        bool IsActive { get; }
    }

    public interface IPowerManager : INotifyPropertyChanged, IDisposable
    {
        event Action<PowerPlugStatus> PowerSourceChanged;
        PowerPlugStatus GetCurrentPowerPlugStatus();

        IEnumerable<IPowerSchema> PowerSchemas { get; }
        void UpdateSchemas();

        void SetPowerSchema(IPowerSchema schema);
        void SetPowerSchema(Guid guid);
    }

    public class PowerSchema : IPowerSchema
    {
        public string Name { get; }
        public Guid Guid { get; }
        public bool IsActive { get; set; }

        public PowerSchema(string name, Guid guid) : this(name, guid, false) { }

        public PowerSchema(string name, Guid guid, bool isActive)
        {
            this.Name = name;
            this.Guid = guid;
            this.IsActive = isActive;
        }
    }
}
