using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTClient;
namespace MQTTMaui.ViewModel
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        string text;

        [ObservableProperty]
        string status;

        [RelayCommand]
        void Connect()
        {
            if (string.IsNullOrWhiteSpace(Text))
                return;

            IClient client = new Client(Text, 1883);
            client.Connect("TESTID", 60);
            Text = string.Empty;
        }
    }
}
