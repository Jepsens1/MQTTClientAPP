using System.Net.Sockets;
using System.Text;

namespace MQTTClient
{
    public class Client : IClient
    {
        private NetworkStream networkStream;
        private TcpClient tcpClient;
        private int _port = 0;
        private string _brokerAddr = string.Empty;
        private byte[] readBuffer = new byte[1024];
        private bool isThreadCreated;
        public Client(string brokerAddr, int port)
        {
            _brokerAddr = brokerAddr;
            _port = port;
        }
        public string Connect(string clientID, int keepAlive)
        {
            try
            {
                tcpClient = new TcpClient(_brokerAddr, _port);
                networkStream = tcpClient.GetStream();
                int payloadSize = 2 + Encoding.UTF8.GetByteCount(clientID); //LSB,MSB + clientid size
                byte[] fixedHeader = { 0x10, (byte)(10 + payloadSize) }; //10 = variable header

                byte[] variableHeader =
                {
                0x00,0x04, //Protocol name length
                0x4D, 0x51, 0x54, 0x54, //MQTT
                0x04, //Protocol level
                0x02, //CONNECT Flags
                0x00, (byte)keepAlive //keep alive
            };
                // Payload
                byte[] clientIDBytes = Encoding.UTF8.GetBytes(clientID);
                byte[] payload = new byte[2 + clientIDBytes.Length]; //LSB + MSB + ClientID

                payload[0] = (byte)(clientIDBytes.Length >> 8);  // Client Identifier Length MSB
                payload[1] = (byte)(clientIDBytes.Length & 0xFF);  // Client Identifier Length LSB
                Array.Copy(clientIDBytes, 0, payload, 2, clientIDBytes.Length); //Paste in the bytes from clientID into the payload array

                // Concatenate fixed header, variable header, and payload
                byte[] connectPacket = new byte[fixedHeader.Length + variableHeader.Length + payload.Length];
                Array.Copy(fixedHeader, 0, connectPacket, 0, fixedHeader.Length);
                Array.Copy(variableHeader, 0, connectPacket, fixedHeader.Length, variableHeader.Length);
                Array.Copy(payload, 0, connectPacket, fixedHeader.Length + variableHeader.Length, payload.Length);
                if (SendPacket(connectPacket))
                    return "Connected to MQTT";
                else return "Connection Failed";
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void Subscribe(string topic, int qos, int packetIdentfier)
        {
            if (tcpClient == null)
                return;

            if (tcpClient.Connected)
            {
                byte[] topicBytes = Encoding.UTF8.GetBytes(topic);
                byte[] payload = new byte[topicBytes.Length + 3]; //Topic bytes + 3, LSB,MSB and QOS

                byte[] fixedHeader = { 0x82, (byte)(2 + payload.Length) }; //2 bytes variable header plus payload size
                byte[] variableHeader = { 0x00, (byte)packetIdentfier };

                payload[0] = (byte)(topicBytes.Length >> 8); // Client Identifier Length MSB
                payload[1] = (byte)(topicBytes.Length & 0xFF); // Client Identifier Length LSB

                Array.Copy(topicBytes, 0, payload, 2, topicBytes.Length); //Paste in the bytes from topic into the payload array

                payload[payload.Length - 1] = (byte)qos; //paste in qos level as last element in payload array


                byte[] subscribePacket = new byte[fixedHeader.Length + variableHeader.Length + payload.Length];
                Array.Copy(fixedHeader, 0, subscribePacket, 0, fixedHeader.Length);
                Array.Copy(variableHeader, 0, subscribePacket, fixedHeader.Length, variableHeader.Length);
                Array.Copy(payload, 0, subscribePacket, fixedHeader.Length + variableHeader.Length, payload.Length);
                SendPacket(subscribePacket);
                if (!isThreadCreated)
                {
                    isThreadCreated = true;
                    Thread t = new Thread(BeginRead);
                    t.Start();
                }
            }
        }

        public void Publish(string topic, int packetIdentifier, int qos, string message)
        {
            if (tcpClient == null)
                return;
            if (tcpClient.Connected)
            {
                byte[] topicBytes = Encoding.UTF8.GetBytes(topic);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);


                byte[] variableHeader = new byte[4 + topicBytes.Length];
                variableHeader[0] = (byte)(topicBytes.Length >> 8);
                variableHeader[1] = (byte)(topicBytes.Length & 0xFF);
                Array.Copy(topicBytes, 0, variableHeader, 2, topicBytes.Length);
                variableHeader[variableHeader.Length - 2] = 0x00;
                variableHeader[variableHeader.Length - 1] = (byte)packetIdentifier;

                byte[] fixedHeader = { 0x32, (byte)(variableHeader.Length + messageBytes.Length) };

                byte[] publishPacket = new byte[fixedHeader.Length + variableHeader.Length + messageBytes.Length];
                Array.Copy(fixedHeader, 0, publishPacket, 0, fixedHeader.Length);
                Array.Copy(variableHeader, 0, publishPacket, fixedHeader.Length, variableHeader.Length);
                Array.Copy(messageBytes, 0, publishPacket, fixedHeader.Length + variableHeader.Length, messageBytes.Length);

                SendPacket(publishPacket);
            }

        }
        private bool SendPacket(byte[] packet)
        {
            try
            {
                networkStream.Write(packet, 0, packet.Length);
                networkStream.Flush();
                return true;

            }
            catch (Exception)
            {
                throw;
            }

        }
        public void BeginRead()
        {
            try
            {
                while (true)
                {
                    if (networkStream.DataAvailable)
                    {
                        int bytesRead = networkStream.Read(readBuffer, 0, readBuffer.Length);
                        if (bytesRead > 0)
                        {
                            string responseString = string.Empty;
                            for (int i = 0; i < bytesRead; i++)
                            {
                                responseString += $"0x{readBuffer[i]:X2} ";
                            }
                            Console.WriteLine(responseString);
                            Console.WriteLine(Encoding.UTF8.GetString(readBuffer));
                            Array.Clear(readBuffer, 0, readBuffer.Length);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    public void PingRequest()
    {
        byte[] fixedHeader = { 0xC0, 0x00 };
        SendPacket(fixedHeader);
    }

    //Disconnect
    public void Disconnect()
    {
        if (tcpClient.Connected)
        {
            byte[] disconnectPacket = { 0xE0 };
            networkStream.Write(disconnectPacket, 0, disconnectPacket.Length);
            networkStream.Flush();
            networkStream.Close();
            tcpClient.Close();
        }
    }
}
}
