namespace MQTTClient
{
    public interface IClient
    {
        void Connect(string clientID, int keepAlive);
        void Subscribe(string topic, int qos, int packetIdentfier);
        void Publish(string topic, int packetIdentifier, int qos, string message);
        void PingRequest();
        void Disconnect();
    }
}
