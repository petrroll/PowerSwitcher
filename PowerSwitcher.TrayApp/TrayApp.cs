using Petrroll.Helpers;
using PowerSwitcher.TrayApp.Configuration;
using PowerSwitcher.TrayApp.Resources;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using WF = System.Windows.Forms;
using System.Management;
using Microsoft.VisualBasic;


namespace PowerSwitcher.TrayApp
{

    public class TrayApp
    {

        #region PrivateObjects
        readonly WF.NotifyIcon _trayIcon;
        public event Action ShowFlyout;
        IPowerManager pwrManager;
        ConfigurationInstance<PowerSwitcherSettings> configuration;
        ManagementEventWatcher startWatcher;
        ManagementEventWatcher stopWatcher;
        #endregion

        #region Contructor
        public TrayApp(IPowerManager powerManager, ConfigurationInstance<PowerSwitcherSettings> config)
        {
            this.pwrManager = powerManager;
            pwrManager.PropertyChanged += PwrManager_PropertyChanged;

            configuration = config;

            startWatcher = new ManagementEventWatcher("Select * From Win32_ProcessStartTrace");
            startWatcher.EventArrived += new EventArrivedEventHandler(startWatcher_EventArrived);
            stopWatcher = new ManagementEventWatcher("Select * From Win32_ProcessStopTrace");
            stopWatcher.EventArrived += new EventArrivedEventHandler(stopWatcher_EventArrived);
            startWatcher.Start();
            stopWatcher.Start();

            _trayIcon = new WF.NotifyIcon();
            _trayIcon.MouseClick += TrayIcon_MouseClick;

            _trayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/PowerSwitcher.TrayApp;component/Tray.ico")).Stream, WF.SystemInformation.SmallIconSize);
            _trayIcon.Text = string.Concat(AppStrings.AppName);
            _trayIcon.Visible = true;

            this.ShowFlyout += (((App)Application.Current).MainWindow as MainWindow).ToggleWindowVisibility;

            //Run automatic on-off-AC change at boot
            powerStatusChanged();
        }

        public void CreateAltMenu()
        {
            var contextMenuRoot = new WF.ContextMenu();
            contextMenuRoot.Popup += ContextMenu_Popup;

            _trayIcon.ContextMenu = contextMenuRoot;

            var contextMenuRootItems = contextMenuRoot.MenuItems;
            contextMenuRootItems.Add("-");

            var contextMenuSettings = contextMenuRootItems.Add(AppStrings.Settings);
            contextMenuSettings.Name = "settings";

            var applicationNameItem = contextMenuSettings.MenuItems.Add("");
            if (configuration.Data.AutomaticAppSwitchApp == "")
            {
                applicationNameItem.Text = $"{AppStrings.ApplicationName} - Not Enabled";
            }
            else
            {
                applicationNameItem.Text = $"{AppStrings.ApplicationName} - {configuration.Data.AutomaticAppSwitchApp}";
            }
            applicationNameItem.Name = "settingsAppName";
            applicationNameItem.Click += AppNameItem_Click;

            var settingsOnAppItem = contextMenuSettings.MenuItems.Add(AppStrings.SchemaToSwitchOnApp);
            settingsOnAppItem.Name = "settingsOnApp";
            settingsOnAppItem.Enabled = (configuration.Data.AutomaticAppSwitchApp != "");

            var settingsOffAppItem = contextMenuSettings.MenuItems.Add(AppStrings.SchemaToSwitchOffApp);
            settingsOffAppItem.Name = "settingsOffApp";
            settingsOffAppItem.Enabled = (configuration.Data.AutomaticAppSwitchApp != "");

            contextMenuSettings.MenuItems.Add("-");

            var settingsOnACItem = contextMenuSettings.MenuItems.Add(AppStrings.SchemaToSwitchOnAc);
            settingsOnACItem.Name = "settingsOnAC";

            var settingsOffACItem = contextMenuSettings.MenuItems.Add(AppStrings.SchemaToSwitchOffAc);
            settingsOffACItem.Name = "settingsOffAC";

            var automaticSwitchItem = contextMenuSettings.MenuItems.Add(AppStrings.AutomaticOnOffACSwitch);
            automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;
            automaticSwitchItem.Click += AutomaticSwitchItem_Click;

            var automaticHideItem = contextMenuSettings.MenuItems.Add(AppStrings.HideFlyoutAfterSchemaChangeSwitch);
            automaticHideItem.Checked = configuration.Data.AutomaticFlyoutHideAfterClick;
            automaticHideItem.Click += AutomaticHideItem_Click;

            var onlyDefaultSchemasItem = contextMenuSettings.MenuItems.Add(AppStrings.ShowOnlyDefaultSchemas);
            onlyDefaultSchemasItem.Checked = configuration.Data.ShowOnlyDefaultSchemas;
            onlyDefaultSchemasItem.Click += OnlyDefaultSchemas_Click;

            var enableShortcutsToggleItem = contextMenuSettings.MenuItems.Add($"{AppStrings.ToggleOnShowrtcutSwitch} ({configuration.Data.ShowOnShortcutKeyModifier} + {configuration.Data.ShowOnShortcutKey})");
            enableShortcutsToggleItem.Enabled = !(Application.Current as App).HotKeyFailed;
            enableShortcutsToggleItem.Checked = configuration.Data.ShowOnShortcutSwitch;
            enableShortcutsToggleItem.Click += EnableShortcutsToggleItem_Click;

            var aboutItem = contextMenuRootItems.Add($"{AppStrings.About} ({Assembly.GetEntryAssembly().GetName().Version})");
            aboutItem.Click += About_Click;

            var exitItem = contextMenuRootItems.Add(AppStrings.Exit);
            exitItem.Click += Exit_Click;
        }

        #endregion

        #region FlyoutRelated
        void TrayIcon_MouseClick(object sender, WF.MouseEventArgs e)
        {
            if (e.Button == WF.MouseButtons.Left)
            {
                ShowFlyout?.Invoke();
            }
        }

        #endregion

        #region SettingsTogglesRegion
        private void EnableShortcutsToggleItem_Click(object sender, EventArgs e)
        {
            WF.MenuItem enableShortcutsToggleItem = (WF.MenuItem)sender;

            configuration.Data.ShowOnShortcutSwitch = !configuration.Data.ShowOnShortcutSwitch;
            enableShortcutsToggleItem.Checked = configuration.Data.ShowOnShortcutSwitch;
            enableShortcutsToggleItem.Enabled = !(Application.Current as App).HotKeyFailed;

            configuration.Save();
        }

        private void AutomaticHideItem_Click(object sender, EventArgs e)
        {
            WF.MenuItem automaticHideItem = (WF.MenuItem)sender;

            configuration.Data.AutomaticFlyoutHideAfterClick = !configuration.Data.AutomaticFlyoutHideAfterClick;
            automaticHideItem.Checked = configuration.Data.AutomaticFlyoutHideAfterClick;

            configuration.Save();
        }

        private void OnlyDefaultSchemas_Click(object sender, EventArgs e)
        {
            WF.MenuItem onlyDefaultSchemasItem = (WF.MenuItem)sender;

            configuration.Data.ShowOnlyDefaultSchemas = !configuration.Data.ShowOnlyDefaultSchemas;
            onlyDefaultSchemasItem.Checked = configuration.Data.ShowOnlyDefaultSchemas;

            configuration.Save();
        }

        private void AutomaticSwitchItem_Click(object sender, EventArgs e)
        {
            WF.MenuItem automaticSwitchItem = (WF.MenuItem)sender;

            configuration.Data.AutomaticOnACSwitch = !configuration.Data.AutomaticOnACSwitch;
            automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;

            if (configuration.Data.AutomaticOnACSwitch) { powerStatusChanged(); }

            configuration.Save();
        }
        private void AppNameItem_Click(object sender, EventArgs e)
        {
            configuration.Data.AutomaticAppSwitchApp = Interaction.InputBox(
                "Enter process name for app switching. Set to blank to disable",
                "Enter process name for app switching",
                configuration.Data.AutomaticAppSwitchApp
            );

            if (configuration.Data.AutomaticAppSwitchApp != "")
            {
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnApp"].Enabled = true;
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffApp"].Enabled = true;
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsAppName"].Text = $"{AppStrings.ApplicationName} - {configuration.Data.AutomaticAppSwitchApp}";
            }
            else
            {
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnApp"].Enabled = false;
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffApp"].Enabled = false;
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsAppName"].Text = $"{AppStrings.ApplicationName} - Not enabled";
            }

            configuration.Save();
        }

        #endregion

        #region AutomaticOnACSwitchRelated

        private void PwrManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPowerManager.CurrentPowerStatus)) { powerStatusChanged(); }
        }

        private void powerStatusChanged()
        {
            if(!configuration.Data.AutomaticOnACSwitch) { return; }

            var currentPowerPlugStatus = pwrManager.CurrentPowerStatus;
            Guid schemaGuidToSwitch = default(Guid);

            switch (currentPowerPlugStatus)
            {
                case PowerPlugStatus.Online:
                    schemaGuidToSwitch = configuration.Data.AutomaticPlanGuidOnAC;
                    break;
                case PowerPlugStatus.Offline:
                    schemaGuidToSwitch = configuration.Data.AutomaticPlanGuidOffAC;
                    break;
                default:
                    break;
            }

            IPowerSchema schemaToSwitchTo = pwrManager.Schemas.FirstOrDefault(sch => sch.Guid == schemaGuidToSwitch);
            if(schemaToSwitchTo == null) { return; }

            pwrManager.SetPowerSchema(schemaToSwitchTo);
        }

        #endregion

        #region AppStartStopRelated
        void startWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if ((string)e.NewEvent["ProcessName"] == configuration.Data.AutomaticAppSwitchApp) { processesChanged(true); }
        }
        void stopWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if ((string)e.NewEvent["ProcessName"] == configuration.Data.AutomaticAppSwitchApp) { processesChanged(false); }
        }
        private void processesChanged(bool started)
        {
            Guid schemaGuidToSwitch = default(Guid);
            switch (started)
            {
                case true:
                    schemaGuidToSwitch = configuration.Data.AutomaticPlanGuidOnApp;
                    break;
                case false:
                    schemaGuidToSwitch = configuration.Data.AutomaticPlanGuidOffApp;
                    break;
                default:
                    break;
            }
            IPowerSchema schemaToSwitchTo = pwrManager.Schemas.FirstOrDefault(sch => sch.Guid == schemaGuidToSwitch);
            if (schemaToSwitchTo == null) { return; }
            pwrManager.SetPowerSchema(schemaToSwitchTo);
        }
        #endregion

        #region ContextMenuItemRelatedStuff

        private void ContextMenu_Popup(object sender, EventArgs e)
        {
            clearPowerSchemasInTray();

            pwrManager.UpdateSchemas();
            foreach (var powerSchema in pwrManager.Schemas)
            {
                updateTrayMenuWithPowerSchema(powerSchema);
            }
        }

        private void updateTrayMenuWithPowerSchema(IPowerSchema powerSchema)
        {
            var newItemMain = getNewPowerSchemaItem(
                powerSchema,
                (s, ea) => switchToPowerSchema(powerSchema),
                powerSchema.IsActive
                );
            _trayIcon.ContextMenu.MenuItems.Add(0, newItemMain);

            var newItemSettingsOffAC = getNewPowerSchemaItem(
                powerSchema,
                (s, ea) => setPowerSchemaAsOffAC(powerSchema),
                (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOffAC)
                );
            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffAC"].MenuItems.Add(0, newItemSettingsOffAC);

            var newItemSettingsOnAC = getNewPowerSchemaItem(
                powerSchema,
                (s, ea) => setPowerSchemaAsOnAC(powerSchema),
                (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOnAC)
                );

            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnAC"].MenuItems.Add(0, newItemSettingsOnAC);

            var newItemSettingsOffApp = getNewPowerSchemaItem(
                powerSchema,
                (s, ea) => setPowerSchemaAsOffApp(powerSchema),
                (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOffApp)
                );
            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffApp"].MenuItems.Add(0, newItemSettingsOffApp);

            var newItemSettingsOnApp = getNewPowerSchemaItem(
                powerSchema,
                (s, ea) => setPowerSchemaAsOnApp(powerSchema),
                (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOnApp)
                );

            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnApp"].MenuItems.Add(0, newItemSettingsOnApp);
        }

        private void clearPowerSchemasInTray()
        {
            for (int i = _trayIcon.ContextMenu.MenuItems.Count - 1; i >= 0; i--)
            {
                var item = _trayIcon.ContextMenu.MenuItems[i];
                if (item.Name.StartsWith("pwrScheme", StringComparison.Ordinal))
                {
                    _trayIcon.ContextMenu.MenuItems.Remove(item);
                }
            }

            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffAC"].MenuItems.Clear();
            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnAC"].MenuItems.Clear();
            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffApp"].MenuItems.Clear();
            _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnApp"].MenuItems.Clear();
        }

        private WF.MenuItem getNewPowerSchemaItem(IPowerSchema powerSchema, EventHandler clickedHandler, bool isChecked)
        {
            var newItemMain = new WF.MenuItem(powerSchema.Name);
            newItemMain.Name = $"pwrScheme{powerSchema.Guid}";
            newItemMain.Checked = isChecked;
            newItemMain.Click += clickedHandler;

            return newItemMain;
        }

        #endregion

        #region OnSchemaClickMethods
        private void setPowerSchemaAsOffAC(IPowerSchema powerSchema)
        {
            configuration.Data.AutomaticPlanGuidOffAC = powerSchema.Guid;
            configuration.Save();
        }

        private void setPowerSchemaAsOnAC(IPowerSchema powerSchema)
        {
            configuration.Data.AutomaticPlanGuidOnAC = powerSchema.Guid;
            configuration.Save();
        }
        private void setPowerSchemaAsOffApp(IPowerSchema powerSchema)
        {
            configuration.Data.AutomaticPlanGuidOffApp = powerSchema.Guid;
            configuration.Save();
        }

        private void setPowerSchemaAsOnApp(IPowerSchema powerSchema)
        {
            configuration.Data.AutomaticPlanGuidOnApp = powerSchema.Guid;
            configuration.Save();
        }

        private void switchToPowerSchema(IPowerSchema powerSchema)
        {
            pwrManager.SetPowerSchema(powerSchema);
        }
        #endregion

        #region OtherItemsClicked

        void About_Click(object sender, EventArgs e)
        {
            Process.Start(AppStrings.AboutAppURL);
        }

        private void IconLicenceItem_Click(object sender, EventArgs e)
        {
            Process.Start(AppStrings.IconLicenceURL);
        }


        void Exit_Click(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            startWatcher.Stop();
            stopWatcher.Stop();
            startWatcher.Dispose();
            stopWatcher.Dispose();

            pwrManager.Dispose();

            Application.Current.Shutdown();
        }
        #endregion

    }
}
