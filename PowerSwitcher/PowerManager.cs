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
            var currSchemaGuid = powerWraper.GetActiveGuid();
            var newSchemas = powerWraper.GetCurrentSchemas();

            //Add and update new / changed schemas
            foreach (var newSchema in newSchemas)
            {
                var originalSchema = Schemas.FirstOrDefault(sch => sch.Guid == newSchema.Guid);
                if (originalSchema == null) { insertNewSchema(newSchemas, newSchema); }
               
                if (newSchema.Guid == currSchemaGuid) { handleCurrentSchema(newSchema, originalSchema); }

                if (newSchema.Guid != currSchemaGuid && originalSchema?.IsActive == true)
                { updateSchema(originalSchema, false); }
                
                if (originalSchema != null && originalSchema.Name != newSchema.Name)
                { ((PowerSchema)originalSchema).Name = newSchema.Name; }
            }

            //remove old schemas
            foreach(var oldSchema in Schemas)
            {
                if (newSchemas.FirstOrDefault(sch => sch.Guid == oldSchema.Guid) == null)
                { Schemas.Remove(oldSchema); }
            }
        }


        private void handleCurrentSchema(PowerSchema newSchema, IPowerSchema originalSchema)
        {
            if (originalSchema == null) { updateSchema(newSchema, true); }
            else if (originalSchema.IsActive == false) { updateSchema(originalSchema, true); }
        }

        private void insertNewSchema(List<PowerSchema> newSchemas, PowerSchema newSchema)
        {
            var insertToIndex = Math.Min(newSchemas.IndexOf(newSchema), Schemas.Count);
            Schemas.Insert(insertToIndex, newSchema);
        }

        private void updateSchema(IPowerSchema schema, bool isActive)
        {
            ((PowerSchema)schema).IsActive = isActive; RaisePropertyChangedEvent(nameof(CurrentSchema));
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



