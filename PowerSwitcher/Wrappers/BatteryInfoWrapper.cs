using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PowerSwitcher.Wrappers
{
    public class BatteryInfoWrapper : IDisposable
    {
        Microsoft.Win32.PowerModeChangedEventHandler powerChangedDelegate = null;
        public BatteryInfoWrapper(Action<PowerPlugStatus> powerStatusChangedFunc)
        {
            powerChangedDelegate = (sender, e) => { powerStatusChangedFunc(GetCurrentChargingStatus()); };
            Microsoft.Win32.SystemEvents.PowerModeChanged += powerChangedDelegate;
        }

        public PowerPlugStatus GetCurrentChargingStatus()
        {
            PowerStatus pwrStatus = SystemInformation.PowerStatus;
            return (pwrStatus.PowerLineStatus == PowerLineStatus.Online) ? PowerPlugStatus.Online : PowerPlugStatus.Offline;
        }

        public int GetChargeValue()
        {
            PowerStatus pwrStatus = SystemInformation.PowerStatus;
            return pwrStatus.BatteryLifeRemaining / 60;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) { return; }

            if (disposing)
            {
                var tmpDelegate = powerChangedDelegate;
                if (tmpDelegate == null) { return; }

                Microsoft.Win32.SystemEvents.PowerModeChanged -= tmpDelegate;
            }

            disposedValue = true;
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BatteryInfoWrapper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this); //No destructor so isn't required yet
        }
        #endregion


    }
}
