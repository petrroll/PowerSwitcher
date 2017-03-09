using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace PowerSwitcher.TrayApp.Services
{
    ////
    //  Based on: http://stackoverflow.com/questions/48935/how-can-i-register-a-global-hot-key-to-say-ctrlshiftletter-using-wpf-and-ne
    ////
    public class HotKey
    {
        public Key Key { get; private set; }
        public KeyModifier KeyModifiers { get; private set; }
        public event Action HotKeyFired;

        public int VirtualKeyCode => KeyInterop.VirtualKeyFromKey(Key);
        public int Id =>  VirtualKeyCode + ((int)KeyModifiers* 0x10000); 

        public HotKey(Key k, KeyModifier keyModifiers)
        {
            Key = k;
            KeyModifiers = keyModifiers;
        }

        public void Fire()
        {
            HotKeyFired?.Invoke();
        }
    }


    public class HotKeyService : IDisposable
    {
        private readonly Dictionary<int, HotKey> _dictHotKeyToCalBackProc;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, UInt32 fsModifiers, UInt32 vlc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotKeyService()
        {
            _dictHotKeyToCalBackProc = new Dictionary<int, HotKey>();
            ComponentDispatcher.ThreadFilterMessage += ComponentDispatcherThreadFilterMessage;
        }


        public bool Register(HotKey hotkey)
        {
            if(_dictHotKeyToCalBackProc.ContainsKey(hotkey.Id)) { Unregister(_dictHotKeyToCalBackProc[hotkey.Id]); }

            var success = RegisterHotKey(IntPtr.Zero, hotkey.Id, (UInt32)hotkey.KeyModifiers, (UInt32)hotkey.VirtualKeyCode);
            if (!success)
            {
                //ERROR_HOTKEY_ALREADY_REGISTERED
                if (Marshal.GetLastWin32Error() == 1409) { return false; }
                else { throw new PowerSwitcherWrappersException($"RegisterHotKey() failed|{Marshal.GetLastWin32Error()}"); }
            }

            _dictHotKeyToCalBackProc.Add(hotkey.Id, hotkey);
            return true;
        }

        public void Unregister(HotKey hotkey)
        {
            if (!_dictHotKeyToCalBackProc.ContainsKey(hotkey.Id)) { throw new InvalidOperationException($"Trying to unregister not-registred Hotkey {hotkey.Id}"); }

            var success = UnregisterHotKey(IntPtr.Zero, hotkey.Id); 
            if (!success) { throw new PowerSwitcherWrappersException($"UnregisterHotKey() failed|{Marshal.GetLastWin32Error()}"); }

            _dictHotKeyToCalBackProc.Remove(hotkey.Id);          
        }

        public const int WmHotKey = 0x0312;
        private void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (handled) { return; }
            if (msg.message != WmHotKey) { return; }

            HotKey hotKey;
            if (_dictHotKeyToCalBackProc.TryGetValue((int)msg.wParam, out hotKey))
            {
                hotKey.Fire();
                handled = true;
            }
        }

        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    foreach (var item in _dictHotKeyToCalBackProc.Values.ToList())
                    {
                        Unregister(item);
                    }
                }

                _disposed = true;
            }
        }


    }

    [Flags]
    public enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        NoRepeat = 0x4000,
        Shift = 0x0004,
        Win = 0x0008
    }
}
