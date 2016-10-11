using PowerSwitcher.TrayApp.Configuration;
using PowerSwitcher.TrayApp.Resources;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WF = System.Windows.Forms;


namespace PowerSwitcher.TrayApp
{

    public class TrayApp
    {

        #region PrivateObjects
        readonly WF.NotifyIcon _trayIcon;
        public event Action ShowFlyout;
        IPowerManager pwrManager;
        ConfigurationInstance<PowerSwitcherSettings> configuration;
        #endregion

        #region Contructor
        public TrayApp(IPowerManager powerManager, ConfigurationInstance<PowerSwitcherSettings> config)
        {
            this.pwrManager = powerManager;
            pwrManager.PowerSourceChanged += PowerManager_PowerSourceChanged;

            configuration = config;

            _trayIcon = new WF.NotifyIcon();
            _trayIcon.MouseClick += TrayIcon_MouseClick;

            var contextMenuRoot = new WF.ContextMenuStrip();
            contextMenuRoot.Opening += ContextMenu_Popup;

            _trayIcon.ContextMenuStrip = contextMenuRoot;

            var contextMenuRootItems = contextMenuRoot.Items;
            contextMenuRootItems.Add("-");

            var contextMenuSettings = new WF.ToolStripMenuItem(AppStrings.Settings);
            contextMenuRootItems.Add(contextMenuSettings);
            contextMenuSettings.Name = "settings";

            var settingsOnACItem = contextMenuSettings.DropDownItems.Add(AppStrings.SchemaToSwitchOnAc);
            settingsOnACItem.Name = "settingsOnAC";

            var settingsOffACItem = contextMenuSettings.DropDownItems.Add(AppStrings.SchemaToSwitchOffAc);
            settingsOffACItem.Name = "settingsOffAC";

            var automaticSwitchItem = (WF.ToolStripMenuItem)contextMenuSettings.DropDownItems.Add(AppStrings.AutomaticOnOffACSwitch);
            automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;
            automaticSwitchItem.Click += AutomaticSwitchItem_Click;

            var automaticHideItem = (WF.ToolStripMenuItem)contextMenuSettings.DropDownItems.Add(AppStrings.HideFlyoutAfterSchemaChangeSwitch);
            automaticHideItem.Checked = configuration.Data.AutomaticFlyoutHideAfterClick;
            automaticHideItem.Click += AutomaticHideItem_Click;

            var onlyDefaultSchemasItem = (WF.ToolStripMenuItem)contextMenuSettings.DropDownItems.Add(AppStrings.ShowOnlyDefaultSchemas);
            onlyDefaultSchemasItem.Checked = configuration.Data.ShowOnlyDefaultSchemas;
            onlyDefaultSchemasItem.Click += OnlyDefaultSchemas_Click;

            var aboutItem = contextMenuRootItems.Add(AppStrings.About);
            aboutItem.Click += About_Click;

            var exitItem = contextMenuRootItems.Add(AppStrings.Exit);
            exitItem.Click += Exit_Click;

            _trayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/PowerSwitcher.TrayApp;component/Tray.ico")).Stream);
            _trayIcon.Text = string.Concat(AppStrings.AppName);
            _trayIcon.Visible = true;

            //Run automatic on-off-AC change at boot
            fireManualOnOffACEvent();
        }

        private void fireManualOnOffACEvent()
        {
            var currPowerPlugState = pwrManager.GetCurrentPowerPlugStatus();
            PowerManager_PowerSourceChanged(currPowerPlugState);
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

        private void AutomaticHideItem_Click(object sender, EventArgs e)
        {
            var automaticHideItem = (WF.ToolStripMenuItem)sender;

            configuration.Data.AutomaticFlyoutHideAfterClick = !configuration.Data.AutomaticFlyoutHideAfterClick;
            automaticHideItem.Checked = configuration.Data.AutomaticFlyoutHideAfterClick;

            configuration.Save();
        }

        private void OnlyDefaultSchemas_Click(object sender, EventArgs e)
        {
            var onlyDefaultSchemasItem = (WF.ToolStripMenuItem)sender;

            configuration.Data.ShowOnlyDefaultSchemas = !configuration.Data.ShowOnlyDefaultSchemas;
            onlyDefaultSchemasItem.Checked = configuration.Data.AutomaticFlyoutHideAfterClick;

            configuration.Save();
        }

        private void AutomaticSwitchItem_Click(object sender, EventArgs e)
        {
            var automaticSwitchItem = (WF.ToolStripMenuItem)sender;

            configuration.Data.AutomaticOnACSwitch = !configuration.Data.AutomaticOnACSwitch;
            automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;

            if (configuration.Data.AutomaticOnACSwitch) { fireManualOnOffACEvent(); }

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

            IPowerSchema schemaToSwitchTo = pwrManager.PowerSchemas.FirstOrDefault(sch => sch.Guid == schemaGuidToSwitch);
            if(schemaToSwitchTo == null) { return; }

            pwrManager.SetPowerSchema(schemaToSwitchTo);
        }

        #endregion

        #region ContextMenuItemRelatedStuff

        private void ContextMenu_Popup(object sender, EventArgs e)
        {
            clearPowerSchemasInTray();

            pwrManager.UpdateSchemas();
            foreach (var powerSchema in pwrManager.PowerSchemas)
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

            _trayIcon.ContextMenuStrip.Items.Insert(0, newItemMain);

            var newItemSettingsOffAC = getNewPowerSchemaItem(
                powerSchema,
                (s, ea) => setPowerSchemaAsOffAC(powerSchema),
                (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOffAC)
                );

            ((WF.ToolStripMenuItem)((WF.ToolStripMenuItem)_trayIcon.ContextMenuStrip.Items["settings"]).DropDownItems["settingsOffAC"]).DropDownItems.Insert(0, newItemSettingsOffAC);

            var newItemSettingsOnAC = getNewPowerSchemaItem(
                powerSchema,
                (s, ea) => setPowerSchemaAsOnAC(powerSchema),
                (powerSchema.Guid == configuration.Data.AutomaticPlanGuidOnAC)
                );

            ((WF.ToolStripMenuItem)((WF.ToolStripMenuItem)_trayIcon.ContextMenuStrip.Items["settings"]).DropDownItems["settingsOnAC"]).DropDownItems.Insert(0, newItemSettingsOnAC);
        }

        private void clearPowerSchemasInTray()
        {
            for (int i = _trayIcon.ContextMenuStrip.Items.Count - 1; i >= 0; i--)
            {
                var item = _trayIcon.ContextMenuStrip.Items[i];
                if (item.Name.StartsWith("pwrScheme", StringComparison.Ordinal))
                {
                    _trayIcon.ContextMenuStrip.Items.Remove(item);
                }
            }

            ((WF.ToolStripMenuItem)((WF.ToolStripMenuItem)_trayIcon.ContextMenuStrip.Items["settings"]).DropDownItems["settingsOnAC"]).DropDownItems.Clear();
            ((WF.ToolStripMenuItem)((WF.ToolStripMenuItem)_trayIcon.ContextMenuStrip.Items["settings"]).DropDownItems["settingsOffAC"]).DropDownItems.Clear();
        }

        private WF.ToolStripMenuItem getNewPowerSchemaItem(IPowerSchema powerSchema, EventHandler clickedHandler, bool isChecked)
        {
            var newItemMain = new WF.ToolStripMenuItem(powerSchema.Name);
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
            pwrManager.SetPowerSchema(powerSchema);
        }
        #endregion

        #region OtherItemsClicked

        void About_Click(object sender, EventArgs e)
        {
            Process.Start(AppStrings.AboutAppURL);
        }

        void Exit_Click(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            pwrManager.Dispose();

            Application.Current.Shutdown();
        }
        #endregion

    }
}
