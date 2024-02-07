namespace MQTTClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IClient client = new Client("test.mosquitto.org", 1883);
            client.Connect("CLIENTAWFAFAWDWDADWDW", 60);
            //client.PingRequest();
            client.Subscribe("example/topic", 0, 10);
            client.Publish("example/topic", 10, 0, "Hello");
            //client.Disconnect();
        }
    }
}