using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WF = System.Windows.Forms;


namespace PowerSwitcher.TrayApp
{

    class TrayApp
    {

        #region PrivateObjects
        readonly WF.NotifyIcon _trayIcon;
        public event Action ShowFlyout;
        IPowerManager powerManager;
        ConfigurationInstance<PowerSwitcherSettings> configuration;
        #endregion

        #region Contructor
        public TrayApp()
        {
            powerManager = new PowerManager();
            powerManager.PowerSourceChanged += PowerManager_PowerSourceChanged;

            var configurationManager = new ConfigurationManagerXML<PowerSwitcherSettings>("PowerSwitcherSettings.xml");
            configuration = new ConfigurationInstance<PowerSwitcherSettings>(configurationManager);

            _trayIcon = new WF.NotifyIcon();
            _trayIcon.MouseClick += TrayIcon_MouseClick;

            var contextMenuRoot = new WF.ContextMenu();
            contextMenuRoot.Popup += ContextMenu_Popup;

            _trayIcon.ContextMenu = contextMenuRoot;

            var contextMenuRootItems = contextMenuRoot.MenuItems;
            contextMenuRootItems.Add("-");

            var contextMenuSettings = contextMenuRootItems.Add("Settings");
            contextMenuSettings.Name = "settings";

            var settingsOnACItem = contextMenuSettings.MenuItems.Add("Schema to switch to on AC");
            settingsOnACItem.Name = "settingsOnAC";

            var settingsOffACItem = contextMenuSettings.MenuItems.Add("Schema to switch to off AC");
            settingsOffACItem.Name = "settingsOffAC";

            var automaticSwitchItem = contextMenuSettings.MenuItems.Add("Automatic on/of AC switch");
            automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;
            automaticSwitchItem.Click += AutomaticSwitchItem_Click;

            var aboutItem = contextMenuRootItems.Add("About");
            aboutItem.Click += About_Click;

            var exitItem = contextMenuRootItems.Add("Exit");
            exitItem.Click += Exit_Click;

            _trayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/PowerSwitcher.TrayApp;component/Tray.ico")).Stream);
            _trayIcon.Text = string.Concat("Power switcher");
            _trayIcon.Visible = true;
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

        #region AutomaticOnACSwitchRelated

        private void AutomaticSwitchItem_Click(object sender, EventArgs e)
        {
            WF.MenuItem automaticSwitchItem = (WF.MenuItem)sender;

            configuration.Data.AutomaticOnACSwitch = !configuration.Data.AutomaticOnACSwitch;
            automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;
            configuration.Save();
        }

        private void PowerManager_PowerSourceChanged(PowerPlugStatus currentPowerPlugStatus)
        {
            if(!configuration.Data.AutomaticOnACSwitch) { return; }

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

            IPowerSchema schemaToSwitchTo = powerManager.GetSchemaToGuid(schemaGuidToSwitch);
            if(schemaToSwitchTo == null) { return; }

            powerManager.SetPowerSchema(schemaToSwitchTo);
        }

        #endregion

        #region ContextMenuItemRelatedStuff

        private void ContextMenu_Popup(object sender, EventArgs e)
        {
            clearPowerSchemasInTray();

            powerManager.UpdateSchemas();
            foreach (var powerSchema in powerManager.PowerSchemas)
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

        private void switchToPowerSchema(IPowerSchema powerSchema)
        {
            powerManager.SetPowerSchema(powerSchema);
        }
        #endregion

        #region OtherItemsClicked

        void About_Click(object sender, EventArgs e)
        {
            Process.Start("http://github.com/File-New-Project/EarTrumpet");
        }

        void Exit_Click(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            powerManager.Dispose();

            Application.Current.Shutdown();
        }
        #endregion

    }
}
