using System;
using System.Collections.ObjectModel;

namespace PowerSwitcher.TrayApp.ViewModels
{
    public class MainWindowViewModelDesign
    {
        public ObservableCollection<IPowerSchema> Schemas { get; private set; } = new ObservableCollection<IPowerSchema>()
        {
            new PowerSchema("High performance", Guid.Empty, true),
            new PowerSchema("Balanced (recommended)", Guid.Empty),
            new PowerSchema("Power saver", Guid.Empty),
            new PowerSchema("Časovače vypnuty (prezentace)", Guid.Empty),
            new PowerSchema("Lorem Ipsum is simply dummy text of the printing and typesetting industry.", Guid.Empty),
        };
    }
}
