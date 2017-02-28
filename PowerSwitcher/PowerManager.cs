using Petrroll.Helpers;
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

    public class PowerManager : ObservableObject, IPowerManager
    {
        Win32PowSchemasWrapper powerWraper;
        BatteryInfoWrapper batteryWrapper;

        public ObservableCollection<IPowerSchema> Schemas{ get; private set; }
        public IPowerSchema CurrentSchema { get { return Schemas.FirstOrDefault(sch => sch.IsActive); } }

        public PowerPlugStatus CurrentPowerStatus { get; private set; }

        public PowerManager()
        {
            powerWraper = new Win32PowSchemasWrapper();
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
                if (originalSchema == null)
                {
                    var insertToIndex = Math.Min(newSchemas.IndexOf(newSchema), Schemas.Count);
                    Schemas.Insert(insertToIndex, newSchema); continue;
                }

                if (originalSchema.IsActive != newSchema.IsActive) { ((PowerSchema)originalSchema).IsActive = newSchema.IsActive; RaisePropertyChangedEvent(nameof(CurrentSchema)); }
                if (originalSchema.Name != newSchema.Name) { ((PowerSchema)originalSchema).Name = newSchema.Name; }
            }

            var currentActiveSchema = getCurrentSchemaWithoutUpdate();
            if (currentActiveSchema.IsActive) { return; }

            currentActiveSchema.IsActive = true;
            RaisePropertyChangedEvent(nameof(CurrentSchema));
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
            currSchema = (PowerSchema)Schemas.FirstOrDefault(s => s.Guid == currSchemaGuid);

            if (currSchema == null) { throw new NotImplementedException("Schemas relaoding not supported yet."); }

            return currSchema;
        }

        private void powerChangedEvent(PowerPlugStatus newStatus)
        {
            CurrentPowerStatus = newStatus;
            RaisePropertyChangedEvent(nameof(CurrentPowerStatus));
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



