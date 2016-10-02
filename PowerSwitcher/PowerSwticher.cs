using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


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
        IPowerSchema GetCurrentSchema();
        IPowerSchema GetSchemaToGuid(Guid guid);
    }

    public class PowerSchema : IPowerSchema
    {
        public string Name { get; }
        public Guid Guid { get; }
        public bool IsActive { get; set; }

        public PowerSchema(string name, Guid guid)
        {
            this.Name = name;
            this.Guid = guid;
            this.IsActive = false;
        }
    }



    public class PowerManager : IPowerManager
    {
        PowProfWrapper powerWraper;
        BatteryInfoWrapper batteryWrapper;
        //WmiPowerSchemesWrapper powerSchemesWrapper;


        public IEnumerable<IPowerSchema> PowerSchemas { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<PowerPlugStatus> PowerSourceChanged;

        public PowerManager()
        {
            powerWraper = new PowProfWrapper();
            batteryWrapper = new BatteryInfoWrapper(powerChangedEvent);
            //powerSchemesWrapper = new WmiPowerSchemesWrapper();

            UpdateSchemas();
        }

        public void UpdateSchemas()
        {
            PowerSchemas = powerWraper.GetCurrentSchemas();
            getCurrentSchemaWithoutUpdate().IsActive = true;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PowerSchemas)));
        }

        public void SetPowerSchema(IPowerSchema schema)
        {
            powerWraper.SetActiveGuid(schema.Guid);
        }

        public IPowerSchema GetCurrentSchema()
        {
            UpdateSchemas();
            return getCurrentSchemaWithoutUpdate();
        }

        public IPowerSchema GetSchemaToGuid(Guid guid)
        {
            UpdateSchemas();
            return PowerSchemas.Where(sch => sch.Guid == guid).FirstOrDefault();
        }

        private PowerSchema getCurrentSchemaWithoutUpdate()
        {
            Guid currSchemaGuid;
            PowerSchema currSchema = null;

            currSchemaGuid = powerWraper.GetActiveGuid();
            currSchema = (PowerSchema)PowerSchemas.Where(s => s.Guid == currSchemaGuid).FirstOrDefault();

            if (currSchema == null) { throw new NotImplementedException("Schemas relaoding not supported yet."); }

            return currSchema;
        }

        private void powerChangedEvent(PowerPlugStatus newStatus)
        {
            PowerSourceChanged?.Invoke(newStatus);
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) { return; }
            if (disposing)
            {
                batteryWrapper.Dispose();
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this); //No destructor so isn't required (yet)
        }

        public PowerPlugStatus GetCurrentPowerPlugStatus()
        {
            return batteryWrapper.GetCurrentChargingStatus();
        }
        #endregion


    }




}



