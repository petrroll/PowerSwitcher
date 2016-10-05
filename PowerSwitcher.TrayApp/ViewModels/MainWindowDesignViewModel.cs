using PowerSwitcher.TrayApp.Extensions;
using System;
using System.Collections.ObjectModel;

namespace PowerSwitcher.TrayApp.ViewModels
{
    public class MainWindowViewModelDesign : ObservableObject
    {
        public MainWindowViewModelDesign()
        {
            Schemas = new ObservableCollection<IPowerSchema>()
            {
                new PowerSchema("Balanced (recommended)", Guid.Empty),
                new PowerSchema("Power saver", Guid.Empty),
                new PowerSchema("Časovače vypnuty (prezentace)", Guid.Empty),
                new PowerSchema("Lorem Ipsum is simply dummy text of the printing and typesetting industry.", Guid.Empty)
            };
            Schemas.Add(ActiveSchema);
        }

        public IPowerSchema ActiveSchema { get; set; } = new PowerSchema("High performance", Guid.Empty, true);
        public ObservableCollection<IPowerSchema> Schemas { get; set; }
    }
}
