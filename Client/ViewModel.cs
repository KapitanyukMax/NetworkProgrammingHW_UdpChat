using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Client
{
    internal class ViewModel
    {
        private const string JOIN_CMD = "<join>";

        private const string LEAVE_CMD = "<leave>";

        private UdpClient client = new UdpClient();

        private bool isListening = false;

        public string Name { get; set; } = string.Empty;

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
            new RelayCommand(o => SendMessage());

        private RelayCommand? leaveCommand;

        public ICommand LeaveCommand => leaveCommand ??=
            new RelayCommand(o => Leave());

        private async void GetResponse()
        {
            UdpReceiveResult res =  await client.ReceiveAsync();
            string message = Encoding.UTF8.GetString(res.Buffer);
            messages.Add(message);
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

        private async void Send(string text)
        {
            IPEndPoint serverIp = new IPEndPoint(IPAddress.Parse(Ip), int.Parse(Port));
            byte[] data = Encoding.UTF8.GetBytes(text);

            await client.SendAsync(data, serverIp);

            if (!isListening)
                GetResponse();
        }

        private void SendMessage()
        {
            if (Message.StartsWith(JOIN_CMD) || Message == LEAVE_CMD)
                MessageBox.Show("You cannot send 'join' or 'leave' command messages");
            else if (string.IsNullOrWhiteSpace(Message))
                MessageBox.Show("You cannot send an empty message");
            else
                Send(Message);
        }

        private async void Join()
        {
            if (isListening)
            {
                MessageBox.Show("You have already joined");
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Enter a valid name to join");
                return;
            }

            isListening = true;
            Send(JOIN_CMD + Name);
            Listen();
        }

        private void Leave()
        {
            if (!isListening)
            {
                MessageBox.Show("You cannot leave unless you have joined");
                return;
            }

            isListening = false;
            Send(LEAVE_CMD);
        }
    }
}