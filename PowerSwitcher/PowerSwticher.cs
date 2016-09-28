using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace PowerSwitcher
{
    public enum PowerPlugStatus { Online, Offline }
    public class PowerSchema
    {
        public string Name { get; }
        public Guid Guid { get; }

        public PowerSchema(string name, Guid guid)
        {
            this.Name = name;
            this.Guid = guid;
        }
    }

    public interface IPowerManager : INotifyPropertyChanged
    {
        event Action<PowerPlugStatus> PowerSourceChanged;

        List<PowerSchema> PowerSchemas { get; }
        void UpdateSchemas();

        void SetPowerSchema(PowerSchema schema);
        PowerSchema GetCurrentSchema();
    }

    public class PowerManager : IPowerManager
    {
        PowProfWrapper powerWraper;
        BatteryInfoWrapper batteryWrapper;
        WmiPowerSchemesWrapper powerSchemesWrapper;


        public List<PowerSchema> PowerSchemas { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<PowerPlugStatus> PowerSourceChanged;

        public PowerManager()
        {
            powerWraper = new PowProfWrapper();
            batteryWrapper = new BatteryInfoWrapper(powerChangedEvent);
            powerSchemesWrapper = new WmiPowerSchemesWrapper();

            UpdateSchemas();
        }

        public void UpdateSchemas()
        {
            PowerSchemas = powerSchemesWrapper.GetCurrentSchemas();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PowerSchemas)));
        }

        public void SetPowerSchema(PowerSchema schema)
        {
            powerWraper.SetActiveGuid(schema.Guid);
        }

        public PowerSchema GetCurrentSchema()
        {
            Guid currSchemaGuid;
            PowerSchema currSchema = null;

            UpdateSchemas();
            currSchemaGuid = powerWraper.GetActiveGuid();
            currSchema = PowerSchemas.Where(s => s.Guid == currSchemaGuid).FirstOrDefault();

            if(currSchema == null) { throw new NotImplementedException("Schemas relaoding not supported yet."); }

            return currSchema;

        }

        private void powerChangedEvent(PowerPlugStatus newStatus)
        {
            PowerSourceChanged?.Invoke(newStatus);
        }


    }




}



