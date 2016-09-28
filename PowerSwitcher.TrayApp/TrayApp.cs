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

        public TrayApp()
        {
            powerManager = new PowerManager();

            _trayIcon = new WF.NotifyIcon();
            _trayIcon.ContextMenu = new WF.ContextMenu();

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

        private void ContextMenu_Popup(object sender, EventArgs e)
        {
            for (int i = _trayIcon.ContextMenu.MenuItems.Count - 1; i >= 0; i--)
            {
                var item = _trayIcon.ContextMenu.MenuItems[i];
                if (item.Name.StartsWith("pwrScheme", StringComparison.Ordinal))
                {
                    _trayIcon.ContextMenu.MenuItems.Remove(item);
                }
            }

            foreach (var powerSchema in powerManager.PowerSchemas)
            {

                var newItem = new WF.MenuItem(powerSchema.Name, (s, ea) => { powerManager.SetPowerSchema(powerSchema); });
                newItem.Name = $"pwrScheme{powerSchema.Guid}";
                _trayIcon.ContextMenu.MenuItems.Add(newItem);
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

            System.Windows.Application.Current.Shutdown();
        }

    }
}
