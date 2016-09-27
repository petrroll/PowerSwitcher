using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace PowerSwitcher
{
    public class PowerSchema
    {
        public string Name { get; }
        public Guid Guid { get; }

        public PowerSchema(string name, Guid guid)
        {
            this.Name = name;
            this.Guid = guid;
        }
    }

    public class PowerManager
    {
        List<PowerSchema> powerSchemas { get; set; }

        public void prepopulateDefaultSchemas()
        {
            powerSchemas.Add(new PowerSchema("Maximum performance", new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")));
            powerSchemas.Add(new PowerSchema("Balanced", new Guid("381b4222-f694-41f0-9685-ff5bb260df2e")));
            powerSchemas.Add(new PowerSchema("Power saver", new Guid("a1841308-3541-4fab-bc81-f71556f20b4a")));
        }

    }

    public static class BatteryInfoWrapper
    {
        public static bool IsCharging()
        {
            PowerStatus pwrStatus = SystemInformation.PowerStatus;
            return pwrStatus.PowerLineStatus == PowerLineStatus.Online;
        }

        public static int GetChargeValue()
        {
            PowerStatus pwrStatus = SystemInformation.PowerStatus;
            return pwrStatus.BatteryLifeRemaining / 60;
        }
    }


    public static class PowProfWrapper
    {

        private static Guid GetActiveGuid()
        {
            Guid activeSchema = Guid.Empty;
            IntPtr guidPtr = IntPtr.Zero;
            if (PowerGetActiveScheme(IntPtr.Zero, out guidPtr) != 0) { return Guid.Empty; }

            activeSchema = (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));
            if (guidPtr != IntPtr.Zero) { LocalFree(guidPtr); }

            return activeSchema;
        }

        private static void SetActiveGuid(Guid guid)
        {
            PowerSetActiveScheme(IntPtr.Zero, ref guid);
        }


        #region DLL imports

        [DllImport("kernel32.dll")]
        private static extern int GetSystemDefaultLCID();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LocalFree(IntPtr hMem);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerSetActiveScheme")]
        public static extern uint PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid ActivePolicyGuid);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerGetActiveScheme")]
        public static extern uint PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerReadFriendlyName")]
        public static extern uint PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr Buffer, ref uint BufferSize);

        #endregion
    }

    public static class WmiPowerSchemesWrapper
    {
        public static List<PowerSchema> GetCurrentSchemas()
        {
            var schemas = new List<PowerSchema>();

            var searcher = new ManagementObjectSearcher(@"root\CIMV2\power", @"Select * FROM Win32_PowerPlan");
            var collection = searcher.Get();

            foreach (ManagementObject mo in collection)
            {
                var name = (string)mo.GetPropertyValue("ElementName");
                var instanceId = (string)mo.GetPropertyValue("InstanceID");

                var match = Regex.Match(instanceId, @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}");
                if(!match.Success) { return null; }

                string guid = match.Value;
                schemas.Add(new PowerSchema(name, new Guid(guid)));
            }

            return schemas;
        }
    }

}



