using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PowerSwitcher.Wrappers
{

    public class BatteryInfoWrapper : IDisposable
    {


        Microsoft.Win32.PowerModeChangedEventHandler powerChangedDelegate = null;
        public BatteryInfoWrapper(Action<PowerPlugStatus> powerStatusChangedFunc)
        {
            powerChangedDelegate = (sender, e) => { powerStatusChangedFunc(ChargingStatus()); };
            Microsoft.Win32.SystemEvents.PowerModeChanged += powerChangedDelegate;
        }

        public PowerPlugStatus ChargingStatus()
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


    public class PowProfWrapper
    {

        public Guid GetActiveGuid()
        {
            Guid activeSchema = Guid.Empty;
            IntPtr guidPtr = IntPtr.Zero;

            try
            {
                var errCode = PowerGetActiveScheme(IntPtr.Zero, out guidPtr);

                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetActiveGuid() failed with code {errCode}"); }
                if (guidPtr == IntPtr.Zero) { throw new PowerSwitcherWrappersException("GetActiveGuid() returned null pointer for GUID"); }

                activeSchema = (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));
            }
            finally
            {
                if (guidPtr != IntPtr.Zero) { LocalFree(guidPtr); }
            }

            return activeSchema;
        }

        public void SetActiveGuid(Guid guid)
        {
            var errCode = PowerSetActiveScheme(IntPtr.Zero, ref guid);
            if (errCode != 0) { throw new PowerSwitcherWrappersException($"SetActiveGuid() failed with code {errCode}"); }
        }

        public string GetPowerPlanName(Guid guid)
        {
            string name = string.Empty;

            IntPtr bufferPointer = IntPtr.Zero;
            uint bufferSize = 0;

            try
            {
                var errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPointer, ref bufferSize);
                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetPowerPlanName() failed when getting buffer size with code {errCode}"); }

                if (bufferSize <= 0) { return String.Empty; }
                bufferPointer = Marshal.AllocHGlobal((int)bufferSize);

                errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPointer, ref bufferSize);
                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetPowerPlanName() failed when getting buffer pointer with code {errCode}"); }

                name = Marshal.PtrToStringUni(bufferPointer);
            }
            finally
            {
                if (bufferPointer != IntPtr.Zero) { Marshal.FreeHGlobal(bufferPointer); }
            }

            return name;
        }

        private const int ERROR_NO_MORE_ITEMS = 259;
        public List<PowerSchema> GetCurrentSchemas()
        {
            var powerSchemas = getAllPowerSchemaGuids().Select(guid => new PowerSchema(GetPowerPlanName(guid), guid)).ToList();
            return powerSchemas;
        }

        private IEnumerable<Guid> getAllPowerSchemaGuids()
        {
            var schemeGuid = Guid.Empty;

            uint sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
            uint schemeIndex = 0;

            while (true)
            {
                uint errCode = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)AccessFlags.ACCESS_SCHEME, schemeIndex, ref schemeGuid, ref sizeSchemeGuid);
                if (errCode == ERROR_NO_MORE_ITEMS) { yield break; }
                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetPowerSchemeGUIDs() failed when getting buffer pointer with code {errCode}"); }

                yield return schemeGuid;
                schemeIndex++;
            }
        }

        #region EnumerationEnums
        public enum AccessFlags : uint
        {
            ACCESS_SCHEME = 16,
            ACCESS_SUBGROUP = 17,
            ACCESS_INDIVIDUAL_SETTING = 18
        }
        #endregion


        #region DLL imports

        [DllImport("kernel32.dll")]
        private static extern int GetSystemDefaultLCID();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerSetActiveScheme")]
        private static extern uint PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid ActivePolicyGuid);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerGetActiveScheme")]
        private static extern uint PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerReadFriendlyName")]
        private static extern uint PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr BufferPtr, ref uint BufferSize);

        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, UInt32 AcessFlags, UInt32 Index, ref Guid Buffer, ref UInt32 BufferSize);

        #endregion
    }

    public class DefaultPowerSchemes
    {
        public List<PowerSchema> GetCurrentSchemas()
        {
            var schemas = new List<PowerSchema>();

            schemas.Add(new PowerSchema("Maximum performance", new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")));
            schemas.Add(new PowerSchema("Balanced", new Guid("381b4222-f694-41f0-9685-ff5bb260df2e")));
            schemas.Add(new PowerSchema("Power saver", new Guid("a1841308-3541-4fab-bc81-f71556f20b4a")));

            return schemas;
        }
    }

    //Deprecated wrapper, not used anymore (replaced by PowProWrapper)
    public class WmiPowerSchemesWrapper
    {
        public List<PowerSchema> GetCurrentSchemas()
        {
            var schemas = new List<PowerSchema>();

            using (var searcher = new ManagementObjectSearcher(@"root\CIMV2\power", @"Select * FROM Win32_PowerPlan"))
            using (var collection = searcher.Get())
            {              
                foreach (ManagementObject mo in collection)
                {                   
                    var name = (string)mo.GetPropertyValue("ElementName");
                    var instanceId = (string)mo.GetPropertyValue("InstanceID");

                    var match = Regex.Match(instanceId, @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}");
                    if (!match.Success) { throw new PowerSwitcherWrappersException("Invalid GUID format in Win32_PowerPlan.InstanceID"); }

                    string guid = match.Value;
                    schemas.Add(new PowerSchema(name, new Guid(guid)));
                }
            }


            return schemas;
        }

    }

    public class PowerSwitcherWrappersException : System.Exception
    {
        public PowerSwitcherWrappersException() { }
        public PowerSwitcherWrappersException(string message) : base(message) { }
        public PowerSwitcherWrappersException(string message, System.Exception inner) : base(message, inner) { }
    }
}
