﻿using Petrroll.Helpers;
using PowerSwitcher.TrayApp.Configuration;
using PowerSwitcher.TrayApp.Services;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace PowerSwitcher.TrayApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public HotKeyService HotKeyManager { get; private set; }
        public bool HotKeyFailed { get; private set; }

        public IPowerManager PowerManager { get; private set; }
        public TrayApp TrayApp { get; private set; }
        public ConfigurationInstance<PowerSwitcherSettings> Configuration { get; private set; }

        private Mutex _mMutex;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!tryToCreateMutex()) return;

            var configurationManager = new ConfigurationManagerXML<PowerSwitcherSettings>(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Petrroll", "PowerSwitcher", "PowerSwitcherSettings.xml"
                ));

            Configuration = new ConfigurationInstance<PowerSwitcherSettings>(configurationManager);
            migrateSettings();

            HotKeyManager = new HotKeyService();
            HotKeyFailed = false;

            PowerManager = new PowerManager();
            MainWindow = new MainWindow();
            TrayApp = new TrayApp(PowerManager, Configuration); //Has to be last because it hooks to MainWindow

            Configuration.Data.PropertyChanged += Configuration_PropertyChanged;
            if (Configuration.Data.ShowOnShortcutSwitch) { registerHotkeyFromConfiguration(); }
            if (Configuration.Data.CycleNextSchemaSwitch) { registerHotkeyFromConfiguration(true); }

            TrayApp.CreateAltMenu();
        }

        private void migrateSettings()
        {
            //Migration of shortcut because Creators update uses WinShift + S for screenshots
            if(Configuration.Data.ShowOnShortcutKey == System.Windows.Input.Key.S &&
                Configuration.Data.ShowOnShortcutKeyModifier == (KeyModifier.Shift | KeyModifier.Win))
            {
                Configuration.Data.ShowOnShortcutKey = System.Windows.Input.Key.L;
                Configuration.Save();
            }
        }

        private void Configuration_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(PowerSwitcherSettings.ShowOnShortcutSwitch))
            {
                if (Configuration.Data.ShowOnShortcutSwitch) { registerHotkeyFromConfiguration(); }
                else { unregisterHotkeyFromConfiguration(); }
            }

            if(e.PropertyName == nameof(PowerSwitcherSettings.CycleNextSchemaSwitch))
            {
                if (Configuration.Data.CycleNextSchemaSwitch) { registerHotkeyFromConfiguration(true); }
                else { unregisterHotkeyFromConfiguration(true); }
            }

        }

        private void unregisterHotkeyFromConfiguration(bool isCycleNextSchemaSwitch = false)
        {
            if(isCycleNextSchemaSwitch)
                HotKeyManager.Unregister(new HotKey(Configuration.Data.CycleNextSchemaKey, Configuration.Data.CycleNextSchemaKeyModifier));
            else
                HotKeyManager.Unregister(new HotKey(Configuration.Data.ShowOnShortcutKey, Configuration.Data.ShowOnShortcutKeyModifier));
        }

        private bool registerHotkeyFromConfiguration(bool isCycleNextSchemaSwitch = false)
        {
            HotKey newHotKey = null;
            if(isCycleNextSchemaSwitch)
                newHotKey = new HotKey(Configuration.Data.CycleNextSchemaKey, Configuration.Data.CycleNextSchemaKeyModifier);
            else
                newHotKey = new HotKey(Configuration.Data.ShowOnShortcutKey, Configuration.Data.ShowOnShortcutKeyModifier);

            bool success = HotKeyManager.Register(newHotKey);
            if(!success) { HotKeyFailed = true; return false; }

            if (isCycleNextSchemaSwitch)
                newHotKey.HotKeyFired += CycleNextPowerSchema;
            else
                newHotKey.HotKeyFired += (this.MainWindow as MainWindow).ToggleWindowVisibility;

            return true;
        }

        private void CycleNextPowerSchema()
        {
            PowerManager.CycleNextPowerSchema();
        }

        private bool tryToCreateMutex()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var mutexName = string.Format(CultureInfo.InvariantCulture, "Local\\{{{0}}}{{{1}}}", assembly.GetType().GUID, assembly.GetName().Name);

            bool mutexCreated;

            _mMutex = new Mutex(true, mutexName, out mutexCreated);
            if (mutexCreated) { return true; }

            _mMutex = null;
            Current.Shutdown();
            return false;
        }

        private void DisposeMutex()
        {
            if (_mMutex == null) return;
            _mMutex.ReleaseMutex();
            _mMutex.Close();
            _mMutex = null;
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            DisposeMutex();
            PowerManager?.Dispose();
            HotKeyManager?.Dispose();
        }

        ~App()
        {
            DisposeMutex();
        }

    }
}
