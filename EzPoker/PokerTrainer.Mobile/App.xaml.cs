using Microsoft.Extensions.DependencyInjection;
using PokerTrainer.Mobile.Views;

namespace PokerTrainer.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //MainPage = new NavigationPage(new PokerTablePage());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}