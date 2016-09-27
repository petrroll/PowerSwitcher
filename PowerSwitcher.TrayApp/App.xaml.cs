using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PowerSwitcher.TrayApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {


        private Mutex _mMutex;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var mutexName = string.Format(CultureInfo.InvariantCulture, "Local\\{{{0}}}{{{1}}}", assembly.GetType().GUID, assembly.GetName().Name);

            bool mutexCreated;

            _mMutex = new Mutex(true, mutexName, out mutexCreated);

            if (!mutexCreated)
            {
                _mMutex = null;
                Current.Shutdown();
                return;
            }

            new MainWindow();
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
        }

        ~App()
        {
            DisposeMutex();
        }

    }
}
