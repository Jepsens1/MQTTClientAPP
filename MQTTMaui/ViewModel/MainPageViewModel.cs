using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTClient;
namespace MQTTMaui.ViewModel
{
    public partial class MainPageViewModel : ObservableObject
    {

        private IClient client;
        [ObservableProperty]
        string brokerAdress = "test.mosquitto.org";

        [ObservableProperty]
        int port = 1883;

        [ObservableProperty]
        string status;

        [RelayCommand]
        void Connect()
        {
            if (string.IsNullOrWhiteSpace(BrokerAdress) || Port <= 1)
                return;
            try
            {
                if(client == null)
                {
                    client = new Client(BrokerAdress, Port);
                    Status = client.Connect("TESTID", 60);
                    return;
                }
                if (Status.ToLower() == "connected to mqtt")
                    return;
                Status = client.Connect("TESTID", 60);

            }
            catch (Exception e)
            {
                Status = e.Message;
            }
        }
    }
}
