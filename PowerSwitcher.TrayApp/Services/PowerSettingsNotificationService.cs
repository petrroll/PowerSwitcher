using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PowerSwitcher.TrayApp.Services
{
    public class PowerSettingsNotificationService : IDisposable
    {
        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, Int32 Flags);

        [DllImport(@"User32", EntryPoint = "UnregisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnregisterPowerSettingNotification(IntPtr handle);


        private const int WM_POWERBROADCAST = 0x0218;

        static Guid GUID_BATTERY_PERCENTAGE_REMAINING = new Guid("A7AD8041-B45A-4CAE-87A3-EECBB468A9E1");
        static Guid GUID_ACDC_POWER_SOURCE = new Guid(0x5D3E9A59, 0xE9D5, 0x4B00, 0xA6, 0xBD, 0xFF, 0x34, 0xFF, 0x51, 0x65, 0x48);
        static Guid GUID_POWERSCHEME_PERSONALITY = new Guid(0x245D8541, 0x3943, 0x4422, 0xB0, 0x25, 0x13, 0xA7, 0x84, 0xF6, 0x79, 0xB7);


        // Win32 decls and defs
        const int PBT_APMQUERYSUSPEND = 0x0000;
        const int PBT_APMQUERYSTANDBY = 0x0001;
        const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
        const int PBT_APMQUERYSTANDBYFAILED = 0x0003;
        const int PBT_APMSUSPEND = 0x0004;
        const int PBT_APMSTANDBY = 0x0005;
        const int PBT_APMRESUMECRITICAL = 0x0006;
        const int PBT_APMRESUMESUSPEND = 0x0007;
        const int PBT_APMRESUMESTANDBY = 0x0008;
        const int PBT_APMBATTERYLOW = 0x0009;
        const int PBT_APMPOWERSTATUSCHANGE = 0x000A; // power status
        const int PBT_APMOEMEVENT = 0x000B;
        const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        const int PBT_POWERSETTINGCHANGE = 0x8013; // DPPE



        const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        // This structure is sent when the PBT_POWERSETTINGSCHANGE message is sent.
        // It describes the power setting that has changed and contains data about the change
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public int DataLength;
        }

        IntPtr hPowerSrc, hBattCapacity, hPowerScheme;
        internal void RegisterForPowerNotifications(IntPtr hwnd)
        {
            hPowerSrc = RegisterPowerSettingNotification(hwnd, ref GUID_ACDC_POWER_SOURCE, DEVICE_NOTIFY_WINDOW_HANDLE);
            testResultOrThrow(hPowerSrc);

            hBattCapacity = RegisterPowerSettingNotification(hwnd, ref GUID_BATTERY_PERCENTAGE_REMAINING, DEVICE_NOTIFY_WINDOW_HANDLE);
            testResultOrThrow(hBattCapacity);

            hPowerScheme = RegisterPowerSettingNotification(hwnd, ref GUID_POWERSCHEME_PERSONALITY, DEVICE_NOTIFY_WINDOW_HANDLE);
            testResultOrThrow(hPowerScheme);
        }

        private void testResultOrThrow(IntPtr result)
        {
            if (result == IntPtr.Zero) { throw new PowerSwitcherWrappersException($"RegisterForPowerNotifications() failed|{Marshal.GetLastWin32Error()}"); }
        }

        private void testResultOrThrow(int result)
        {
            if (result == 0) { throw new PowerSwitcherWrappersException($"RegisterForPowerNotifications() failed|{Marshal.GetLastWin32Error()}"); }
        }

        internal void UnregisterForPowerNotifications()
        {
            testResultOrThrow(UnregisterPowerSettingNotification(hBattCapacity));
            testResultOrThrow(UnregisterPowerSettingNotification(hPowerScheme));
            testResultOrThrow(UnregisterPowerSettingNotification(hPowerSrc));
        }

        internal IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WM_POWERBROADCAST || wParam.ToInt32() != PBT_POWERSETTINGCHANGE) { return IntPtr.Zero; }

            try
            {
                POWERBROADCAST_SETTING notification = (POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING));
                IntPtr pData = (IntPtr)((int)lParam + Marshal.SizeOf(notification));

                if (notification.PowerSetting == GUID_POWERSCHEME_PERSONALITY)
                {
                    if (notification.DataLength != Marshal.SizeOf(typeof(Guid)))
                    { throw new PowerSwitcherWrappersException($"WndProc - powerscheme change - message isn't guid sized."); }

                    Guid newPersonality = (Guid)Marshal.PtrToStructure(pData, typeof(Guid));
                    SetPowerPlan(newPersonality);
                }
                else if (notification.PowerSetting == GUID_BATTERY_PERCENTAGE_REMAINING || notification.PowerSetting == GUID_ACDC_POWER_SOURCE)
                {
                    if (notification.DataLength != Marshal.SizeOf(typeof(Int32)))
                    { throw new PowerSwitcherWrappersException($"WndProc - powerscheme change - message isn't int32 sized."); }

                    Int32 iData = (Int32)Marshal.PtrToStructure(pData, typeof(Int32));
                    if (notification.PowerSetting == GUID_BATTERY_PERCENTAGE_REMAINING) { SetBatteryLevel(iData); }
                    else if (notification.PowerSetting == GUID_ACDC_POWER_SOURCE) { SetPowerSource(iData); }
                }
            }
            catch
            {
                throw new PowerSwitcherWrappersException($"WndProc - unknown issue.");
            }

            handled = true;
            return IntPtr.Zero;

        }

        private void SetPowerSource(int iData)
        {
            //iData
            // battery: 1
            // AC     : 0
        }

    
        private void SetBatteryLevel(int iData)
        {
            //iData: battery level
        }

        private void SetPowerPlan(Guid guid)
        {


        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                UnregisterForPowerNotifications();
                disposedValue = true;
            }
        }

         ~PowerSettingsNotificationService() {
           Dispose(false);
         }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
