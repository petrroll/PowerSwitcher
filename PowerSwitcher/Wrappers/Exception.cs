using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerSwitcher.Wrappers
{
    public class PowerSwitcherWrappersException : System.Exception
    {
        public PowerSwitcherWrappersException() { }
        public PowerSwitcherWrappersException(string message) : base(message) { }
        public PowerSwitcherWrappersException(string message, System.Exception inner) : base(message, inner) { }
    }
}
