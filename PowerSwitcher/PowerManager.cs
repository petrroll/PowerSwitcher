using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace PowerSwitcher
{
    public interface IPowerManager : INotifyPropertyChanged, IDisposable
    {
        event Action<PowerPlugStatus> PowerSourceChanged;
        PowerPlugStatus GetCurrentPowerPlugStatus();

        IEnumerable<IPowerSchema> PowerSchemas { get; }
        void UpdateSchemas();

        void SetPowerSchema(IPowerSchema schema);
        void SetPowerSchema(Guid guid);
    }

    public class PowerManager : IPowerManager
    {
        PowProfWrapper powerWraper;
        BatteryInfoWrapper batteryWrapper;

        public IEnumerable<IPowerSchema> PowerSchemas { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<PowerPlugStatus> PowerSourceChanged;

        public PowerManager()
        {
            powerWraper = new PowProfWrapper();
            batteryWrapper = new BatteryInfoWrapper(powerChangedEvent);

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
            SetPowerSchema(schema.Guid);
        }

        public void SetPowerSchema(Guid guid)
        {
            powerWraper.SetActiveGuid(guid);
            UpdateSchemas();
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

            //No destructor so isn't required (yet)            
            // GC.SuppressFinalize(this); 
        }

        public PowerPlugStatus GetCurrentPowerPlugStatus()
        {
            return batteryWrapper.GetCurrentChargingStatus();
        }

        #endregion

    }

}



