using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        const int port = 3737;
        const string JOIN_CMD = "<join>";
        const string LEAVE_CMD = "<leave>";

        static UdpClient server = new UdpClient(port);
        static Dictionary<IPEndPoint, string> members = new Dictionary<IPEndPoint, string>();

        private static async void SendFromUser(string text, IPEndPoint from, string messageAuthor)
        {
            string message = $"{messageAuthor} - {text} : {DateTime.Now.ToShortTimeString()}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            foreach (IPEndPoint ip in members.Keys)
                if (ip.Port != from.Port && ip.Address != from.Address)
                    await server.SendAsync(data, ip);
        }

        private static async void SendToUser(string text, IPEndPoint to, string messageAuthor)
        {
            string message = $"{messageAuthor} - {text} : {DateTime.Now.ToShortTimeString()}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            await server.SendAsync(data, to);
        }

        static void Main(string[] args)
        {

            while (true)
            {
                IPEndPoint? clientIp = null;

                byte[] data = server.Receive(ref clientIp);
                string message = Encoding.UTF8.GetString(data);

                Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {message} | from {clientIp}");

                if (message.StartsWith(JOIN_CMD) && !members.ContainsKey(clientIp))
                {
                    string userName = message[JOIN_CMD.Length..];
                    if (members.Count < 10)
                    {
                        SendFromUser($"{userName} joined", clientIp, "Server");
                        members.Add(clientIp, userName);
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {userName} joined");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {userName} cannot join, server is full");
                        SendToUser("Server is full, you cannot join", clientIp, "Server");
                    }
                }
                else if (message == LEAVE_CMD && members.ContainsKey(clientIp))
                {
                    string userName = members[clientIp];
                    SendFromUser($"{userName} left", clientIp, "Server");
                    members.Remove(clientIp);
                    Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] - {userName} left");
                }
                else if (!members.ContainsKey(clientIp))
                    SendToUser($"You cannot send messages unless you are registered", clientIp, "Server");
                else
                    SendFromUser(message, clientIp, members[clientIp]);
            }
        }
    }
}