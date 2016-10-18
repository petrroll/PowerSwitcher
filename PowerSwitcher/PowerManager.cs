using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;


namespace PowerSwitcher
{
    public interface IPowerManager : INotifyPropertyChanged, IDisposable
    {
        ObservableCollection<IPowerSchema> Schemas { get; }
        IPowerSchema CurrentSchema { get; }

        PowerPlugStatus CurrentPowerStatus { get; }

        void UpdateSchemas();

        void SetPowerSchema(IPowerSchema schema);
        void SetPowerSchema(Guid guid);
    }

    public class PowerManager : IPowerManager
    {
        PowProfWrapper powerWraper;
        BatteryInfoWrapper batteryWrapper;

        public ObservableCollection<IPowerSchema> Schemas{ get; private set; }
        public IPowerSchema CurrentSchema { get { return Schemas.FirstOrDefault(sch => sch.IsActive); } }

        public PowerPlugStatus CurrentPowerStatus { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public PowerManager()
        {
            powerWraper = new PowProfWrapper();
            batteryWrapper = new BatteryInfoWrapper(powerChangedEvent);

            Schemas = new ObservableCollection<IPowerSchema>();

            powerChangedEvent(batteryWrapper.GetCurrentChargingStatus());
            UpdateSchemas();
        }

        public void UpdateSchemas()
        {
            var newSchemas = powerWraper.GetCurrentSchemas();
            foreach (var newSchema in newSchemas)
            {
                var originalSchema = Schemas.FirstOrDefault(sch => sch.Guid == newSchema.Guid);
                if (originalSchema == null) { Schemas.Insert(newSchemas.IndexOf(newSchema), newSchema); continue; }

                if (originalSchema.IsActive != newSchema.IsActive) { ((PowerSchema)originalSchema).IsActive = newSchema.IsActive; raisePropertyChanged(nameof(CurrentSchema)); }
                if (originalSchema.Name != newSchema.Name) { ((PowerSchema)originalSchema).Name = newSchema.Name; }
            }

            var currentActiveSchema = getCurrentSchemaWithoutUpdate();
            if (currentActiveSchema.IsActive) { return; }

            currentActiveSchema.IsActive = true;
            raisePropertyChanged(nameof(CurrentSchema));
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
            currSchema = (PowerSchema)Schemas.Where(s => s.Guid == currSchemaGuid).FirstOrDefault();

            if (currSchema == null) { throw new NotImplementedException("Schemas relaoding not supported yet."); }

            return currSchema;
        }

        private void powerChangedEvent(PowerPlugStatus newStatus)
        {
            CurrentPowerStatus = newStatus;
            raisePropertyChanged(nameof(CurrentPowerStatus));
        }

        private void raisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        #endregion

    }

}



