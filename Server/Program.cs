using MessageLibrary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server
{
    internal class Program
    {
        private const int maximumClients = 10;

        private const int port = 3737;

        private static UdpClient server = new UdpClient(port);

        private static Dictionary<IPEndPoint, string> members = new Dictionary<IPEndPoint, string>();

        private static async void SendFromUser(Message message)
        {
            string messageJson = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(messageJson);

            foreach (IPEndPoint ip in members.Keys)
                if (ip.Port != message.FromPort && ip.Address != IPAddress.Parse(message.FromAddress))
                    await server.SendAsync(data, ip);
        }

        private static async void SendToUser(Message message)
        {
            string messageJson = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(messageJson);

            IPEndPoint to = new IPEndPoint(IPAddress.Parse(message.ToAddress), message.ToPort
                ?? throw new ArgumentNullException());
            await server.SendAsync(data, to);
        }

        static void Main(string[] args)
        {
            while (true)
            {
                IPEndPoint? clientIp = null;

                byte[] data = server.Receive(ref clientIp);
                string messageJson = Encoding.UTF8.GetString(data);
                Message message = JsonSerializer.Deserialize<Message>(messageJson);

                if (!string.IsNullOrWhiteSpace(message.Text))
                    Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {message.Text} | from {clientIp}");
                else
                    Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {message.Command} | from {clientIp}");

                if (message.Command == Message.JOIN_CMD && !members.ContainsKey(clientIp))
                {
                    if (members.Count < maximumClients)
                    {
                        Message response = new Message
                        {
                            SenderName = "<server>",
                            Text = $"{message.SenderName} joined",
                            FromAddress = clientIp.Address.ToString(),
                            FromPort = message.FromPort
                        };
                        SendFromUser(response);

                        members.Add(clientIp, message.SenderName);
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {message.SenderName} joined");
                    }
                    else
                    {
                        Message response = new Message
                        {
                            SenderName = "<server>",
                            Text = "Server is full, you cannot join",
                            ToAddress = clientIp.Address.ToString(),
                            ToPort = message.ToPort
                        };
                        SendToUser(response);

                        Console.WriteLine(
                            $"[{DateTime.Now.ToShortTimeString()}] - {message.SenderName} cannot join, server is full");
                    }
                }
                else if (message.Command == Message.LEAVE_CMD && members.ContainsKey(clientIp))
                {
                    Message response = new Message
                    {
                        SenderName = "<server>",
                        Text = $"{message.SenderName} left",
                        FromAddress = clientIp.Address.ToString(),
                        FromPort = clientIp.Port
                    };
                    SendFromUser(response);

                    members.Remove(clientIp);
                    Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {message.SenderName} left");
                }
                else if (!members.ContainsKey(clientIp))
                {
                    Message response = new Message
                    {
                        SenderName = "<server>",
                        Text = "You cannot send messages unless you are registered",
                        ToAddress = clientIp.Address.ToString(),
                        ToPort= clientIp.Port
                    };
                    SendToUser(response);
                }
                else if (message.ReplyToMessage == null)
                {
                    Message response = new Message
                    {
                        SenderName = message.SenderName,
                        Text = message.Text,
                        FromAddress = clientIp.Address.ToString(),
                        FromPort = clientIp.Port
                    };
                    SendFromUser(response);
                }
                else
                {
                    Message response = new Message
                    {
                        SenderName = message.SenderName,
                        Text = message.Text,
                        FromAddress = clientIp.Address.ToString(),
                        FromPort = clientIp.Port,
                        ToAddress = message.ToAddress.ToString(),
                        ToPort = message.ToPort,
                        ReplyToMessage = message.ReplyToMessage
                    };
                    SendToUser(response);
                }
            }
        }
    }
}