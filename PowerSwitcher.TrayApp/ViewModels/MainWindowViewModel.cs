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

        public ObservableCollection<IPowerSchema> Schemas { get { return pwrManager.Schemas; } }
        public IPowerSchema ActiveSchema
        {
            get { return pwrManager.CurrentSchema; }
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

            //Doesn't work ATM
            filterOnlyDefaultSchemas = config.Data.ShowOnlyDefaultSchemas;
        }

        private void SettingsData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PowerSwitcherSettings.ShowOnlyDefaultSchemas)) { }
        }

        private void PwrManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPowerManager.CurrentSchema)) { RaisePropertyChangedEvent(nameof(ActiveSchema)); }
        }

        private Guid[] defaultGuids = { new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"), new Guid("a1841308-3541-4fab-bc81-f71556f20b4a") };

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
