using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SocketClientWpf
{
    public partial class MainWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private string _userId;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _userId = User_Id.Text.Trim();
                string ip = ServerIP.Text.Trim();
                int port = int.Parse(Port.Text.Trim());

                if (string.IsNullOrWhiteSpace(_userId))
                {
                    MessageBox.Show("Kullanıcı ID boş olamaz.");
                    return;
                }

                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Parse(ip), port);
                _stream = _client.GetStream();

                // İlk mesaj: sadece user ID gönderilir
                byte[] userBytes = Encoding.UTF8.GetBytes(_userId);
                await _stream.WriteAsync(userBytes, 0, userBytes.Length);

                Recieve.AppendText(" Sunucuya bağlanıldı.\n");

                // Gelen mesajları dinle
                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı hatası: " + ex.Message);
            }
        }

        private async void btnGonder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_stream == null || !_client.Connected)
                {
                    MessageBox.Show("Bağlantı yok.");
                    return;
                }

                string mesaj = Message.Text.Trim();
                string recipientId = Recipient_Id.Text.Trim();

                if (string.IsNullOrWhiteSpace(mesaj) || string.IsNullOrWhiteSpace(recipientId))
                {
                    MessageBox.Show("Mesaj ve Alıcı ID boş olamaz.");
                    return;
                }

                string formattedMessage = $"{_userId}:{recipientId}:{mesaj}";
                byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
                await _stream.WriteAsync(data, 0, data.Length);

                Message.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gönderim hatası: " + ex.Message);
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (_client.Connected)
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    string gelen = Encoding.UTF8.GetString(buffer, 0, read);

                    var parts = gelen.Split('|');
                    if (parts.Length == 3)
                    {
                        string senderId = parts[0];
                        string recipientId = parts[1];
                        string messageText = parts[2];

                        // Show only if this user is the recipient
                        if (recipientId == _userId)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Recieve.AppendText($"From {senderId}: {messageText}{Environment.NewLine}");
                            });
                        }
                    }
                
                    //    Dispatcher.Invoke(() =>
                    //{
                    //    Recieve.AppendText(gelen + Environment.NewLine);
                    //});
                }
                    }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    Recieve.AppendText(" Bağlantı kesildi: " + ex.Message + Environment.NewLine);
                });
            }
        }
        private void ClearPlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && (tb.Text == "Mesaj yaz..." || tb.Text == "Alıcı ID"))
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        private void RestorePlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb.Name == "Message")
                {
                    tb.Text = "Mesaj yaz...";
                }
                else if (tb.Name == "Recipient_Id")
                {
                    tb.Text = "Alıcı ID";
                }
                tb.Foreground = Brushes.Gray;
            }
        }


    }
}
