using Bersquash.Pages;
using Plugin.Firebase.Auth;

namespace Bersquash
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());

        }
    }
}
