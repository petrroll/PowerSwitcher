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
        public IPowerSchema CurrentSchema { get; private set; }

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
                if (originalSchema == null) { insertNewSchema(newSchemas, newSchema); originalSchema = newSchema; }
               
                if (newSchema.Guid == currSchemaGuid && originalSchema?.IsActive != true)
                { setNewCurrSchema(originalSchema); }
                
                if (originalSchema?.Name != newSchema.Name)
                { ((PowerSchema)originalSchema).Name = newSchema.Name; }
            }

            if(!Schemas.Any(sch => currSchemaGuid == sch.Guid))
            {
                noSchemaIsActive();
            }

            //remove old schemas
            var schemasToBeRemoved = new List<IPowerSchema>();
            foreach (var oldSchema in Schemas)
            {
                if (newSchemas.FirstOrDefault(sch => sch.Guid == oldSchema.Guid) == null)
                { schemasToBeRemoved.Add(oldSchema); }
            }
            schemasToBeRemoved.ForEach(sch => Schemas.Remove(sch));
        }

        private void noSchemaIsActive()
        {
            var oldActive = Schemas.FirstOrDefault(sch => sch.IsActive);
            if (oldActive != null)
            {
                ((PowerSchema)oldActive).IsActive = false;

                CurrentSchema = null;
                RaisePropertyChangedEvent(nameof(CurrentSchema));
            }
        }

        private void insertNewSchema(List<PowerSchema> newSchemas, PowerSchema newSchema)
        {
            var insertToIndex = Math.Min(newSchemas.IndexOf(newSchema), Schemas.Count);
            Schemas.Insert(insertToIndex, newSchema);
        }

        private void setNewCurrSchema(IPowerSchema newActiveSchema)
        {
            var oldActiveSchema = Schemas.FirstOrDefault(sch => sch.IsActive);

            ((PowerSchema)newActiveSchema).IsActive = true;
            CurrentSchema = newActiveSchema;
            RaisePropertyChangedEvent(nameof(CurrentSchema));

            //can cause change change of curr power schema: http://stackoverflow.com/questions/42703092/remove-selection-when-selected-item-gets-deleted-from-listbox
            if (oldActiveSchema != null) { ((PowerSchema)oldActiveSchema).IsActive = false; }
        }

        public void SetPowerSchema(IPowerSchema schema)
        {
            SetPowerSchema(schema.Guid);
        }

        public void SetPowerSchema(Guid guid)
        {
            try { powerWraper.SetActiveGuid(guid); } catch (PowerSwitcherWrappersException) { }
            UpdateSchemas();
        }

        private void powerChangedEvent(PowerPlugStatus newStatus)
        {
            if(newStatus == CurrentPowerStatus) { return; }

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



