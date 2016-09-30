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
        readonly WF.NotifyIcon _trayIcon;
        public event Action ShowFlyout;
        IPowerManager powerManager;
        ConfigurationInstance<PowerSwitcherSettings> configuration;

        public TrayApp()
        {
            powerManager = new PowerManager();
            powerManager.PowerSourceChanged += PowerManager_PowerSourceChanged;

            var configurationManager = new ConfigurationManagerXML<PowerSwitcherSettings>("PowerSwitcherSettings.xml");
            configuration = new ConfigurationInstance<PowerSwitcherSettings>(configurationManager);

            _trayIcon = new WF.NotifyIcon();
            _trayIcon.ContextMenu = new WF.ContextMenu();

            _trayIcon.ContextMenu.MenuItems.Add("-");

            var settingsMenuHive = _trayIcon.ContextMenu.MenuItems.Add("Settings");
            settingsMenuHive.Name = "settings";

            var settingsOnACItem = settingsMenuHive.MenuItems.Add("Schema to switch to on AC");
            settingsOnACItem.Name = "settingsOnAC";

            var settingsOffACItem = settingsMenuHive.MenuItems.Add("Schema to switch to off AC");
            settingsOffACItem.Name = "settingsOffAC";

            var automaticSwitchItem = settingsMenuHive.MenuItems.Add("Automatic on/of AC switch");
            automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;
            automaticSwitchItem.Click += (sender, e) =>
            {
                configuration.Data.AutomaticOnACSwitch = !configuration.Data.AutomaticOnACSwitch;
                automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;
                configuration.Save();
            };

            var aboutItem = _trayIcon.ContextMenu.MenuItems.Add("About");
            aboutItem.Click += About_Click;

            var exitItem = _trayIcon.ContextMenu.MenuItems.Add("Exit");
            exitItem.Click += Exit_Click;

            _trayIcon.MouseClick += TrayIcon_MouseClick;
            _trayIcon.ContextMenu.Popup += ContextMenu_Popup;
            _trayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/PowerSwitcher.TrayApp;component/Tray.ico")).Stream);
            _trayIcon.Text = string.Concat("Power switcher");
            _trayIcon.Visible = true;

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

        private void ContextMenu_Popup(object sender, EventArgs e)
        {
            powerManager.UpdateSchemas();

            for (int i = _trayIcon.ContextMenu.MenuItems.Count - 1; i >= 0; i--)
            {
                var item = _trayIcon.ContextMenu.MenuItems[i];
                if (item.Name.StartsWith("pwrScheme", StringComparison.Ordinal))
                {
                    _trayIcon.ContextMenu.MenuItems.Remove(item);
                }
            }

            for(int i = _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffAC"].MenuItems.Count - 1; i >= 0; i--)
            {
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffAC"].MenuItems.Remove(_trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffAC"].MenuItems[i]);
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnAC"].MenuItems.Remove(_trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnAC"].MenuItems[i]);
            }

            foreach (var powerSchema in powerManager.PowerSchemas)
            {
                var newItemMain = new WF.MenuItem(powerSchema.Name);

                newItemMain.Click += (s, ea) => { powerManager.SetPowerSchema(powerSchema); };
                newItemMain.Name = $"pwrScheme{powerSchema.Guid}";
                newItemMain.Checked = powerSchema.IsActive;
                _trayIcon.ContextMenu.MenuItems.Add(0, newItemMain);

                var newItemSettingsOffAC = new WF.MenuItem(powerSchema.Name);

                newItemSettingsOffAC.Click += (s, ea) => { configuration.Data.AutomaticPlanGuidOffAC = powerSchema.Guid; configuration.Save(); };
                newItemSettingsOffAC.Name = $"pwrScheme{powerSchema.Guid}";
                newItemSettingsOffAC.Checked = (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOffAC);
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOffAC"].MenuItems.Add(0, newItemSettingsOffAC);

                var newItemSettingsOnAC = new WF.MenuItem(powerSchema.Name);

                newItemSettingsOnAC.Click += (s, ea) => { configuration.Data.AutomaticPlanGuidOnAC = powerSchema.Guid; configuration.Save(); };
                newItemSettingsOnAC.Name = $"pwrScheme{powerSchema.Guid}";
                newItemSettingsOnAC.Checked = (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOnAC);
                _trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnAC"].MenuItems.Add(0, newItemSettingsOnAC);
            }
        }

        void TrayIcon_MouseClick(object sender, WF.MouseEventArgs e)
        {
            if (e.Button == WF.MouseButtons.Left)
            {
                ShowFlyout?.Invoke();
            }
        }

        void About_Click(object sender, EventArgs e)
        {
            Process.Start("http://github.com/File-New-Project/EarTrumpet");
        }

        void Exit_Click(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            powerManager.Dispose();

            System.Windows.Application.Current.Shutdown();
        }

    }
}
