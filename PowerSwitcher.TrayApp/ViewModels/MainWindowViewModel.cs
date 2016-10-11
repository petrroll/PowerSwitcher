using PowerSwitcher.TrayApp.Configuration;
using PowerSwitcher.TrayApp.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PowerSwitcher.TrayApp.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {

        private IPowerManager pwrManager;
        private ConfigurationInstance<PowerSwitcherSettings> config;
        private bool filterOnlyDefaultSchemas;

        public ObservableCollection<IPowerSchema> Schemas { get; private set; }
        public IPowerSchema ActiveSchema
        {
            get { return Schemas.FirstOrDefault(sch => sch.IsActive); }
            set { if (value != null) { pwrManager.SetPowerSchema(value); } }
        }

        public MainWindowViewModel()
        {
            App currApp = System.Windows.Application.Current as App;
            if (currApp == null) { return; }

            this.pwrManager = currApp.PowerManager;
            this.config = currApp.Configuration;

            pwrManager.PropertyChanged += PwrManager_PropertyChanged;
            config.Data.PropertyChanged += SettingsData_PropertyChanged;

            filterOnlyDefaultSchemas = config.Data.ShowOnlyDefaultSchemas;
            Schemas = new ObservableCollection<IPowerSchema>();

            updateCurrentSchemas();
        }

        private void SettingsData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PowerSwitcherSettings.ShowOnlyDefaultSchemas))
            {
                //TODO: Do better binding, this's ugly
                filterOnlyDefaultSchemas = config.Data.ShowOnlyDefaultSchemas;
                updateCurrentSchemas();
            }
        }

        private void PwrManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //TODO: Do better binding do underlying model
            if(e.PropertyName == nameof(IPowerManager.PowerSchemas))
            {
                updateCurrentSchemas();
            }
            else
            {
                throw new InvalidOperationException("Invalid property changed on IPowerManager");
            }
        }

        private Guid[] defaultGuids = { new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"), new Guid("a1841308-3541-4fab-bc81-f71556f20b4a") };
        private void updateCurrentSchemas()
        {
            Schemas.Clear();

            var currSchemas = pwrManager.PowerSchemas;
            currSchemas = (filterOnlyDefaultSchemas) ? currSchemas.Where(sch => (sch.IsActive || defaultGuids.Contains(sch.Guid))) : currSchemas;
            currSchemas.ForEach(sch => Schemas.Add(sch));

            RaisePropertyChangedEvent(nameof(ActiveSchema));
        }

        public void SetGuidAsActive(Guid guid)
        {
            pwrManager.SetPowerSchema(guid);
        }

        public void Refresh()
        {
            pwrManager.UpdateSchemas();
        }
    }
}
