using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;

namespace Client
{
    internal class ViewModel
    {
        private UdpClient client = new UdpClient();

        private bool isListening = false;

        public string Ip { get; set; } = "127.0.0.1";

        public string Port { get; set; } = "3737";

        public string Message { get; set; } = "Lorem Ipsum!";

        private ObservableCollection<string> messages { get; set; } =
            new ObservableCollection<string>();

        public IEnumerable<string> Messages => messages;

        private RelayCommand? joinCommand;

        public ICommand JoinCommand => joinCommand ??=
            new RelayCommand(o => Join());

        private RelayCommand? sendMessageCommand;

        public ICommand SendMessageCommand => sendMessageCommand ??=
            new RelayCommand(o => SendMessage(Message));

        private RelayCommand? leaveCommand;

        public ICommand LeaveCommand => leaveCommand ??=
            new RelayCommand(o => Leave());

        private async void SendMessage(string text)
        {
            IPEndPoint serverIp = new IPEndPoint(IPAddress.Parse(Ip), int.Parse(Port));
            byte[] data = Encoding.UTF8.GetBytes(text);

            await client.SendAsync(data, serverIp);
        }

        private async void Listen()
        {
            while (isListening)
            {
                UdpReceiveResult res = await client.ReceiveAsync();
                string message = Encoding.UTF8.GetString(res.Buffer);
                messages.Add(message);
            }
        }

        private void Join()
        {
            SendMessage("<join>");
            isListening = true;
            Listen();
        }

        private void Leave()
        {
            SendMessage("<leave>");
            isListening = false;
        }
    }
}