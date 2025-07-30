using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServerHost
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Write("Sunucu IP adresini girin  ");
            string ipString = Console.ReadLine();

            Console.Write("Port numarasını girin : ");
            string portString = Console.ReadLine();

            if (!IPAddress.TryParse(ipString, out IPAddress ip))
            {
                Console.WriteLine("Geçersiz IP adresi.");
                return;
            }

            if (!int.TryParse(portString, out int port))
            {
                Console.WriteLine("Geçersiz port numarası.");
                return;
            }

            Server server = new Server();
            await server.StartAsync(ip, port);
        }
    }

    public class Server
    {
        private TcpListener _listener;
        private ConcurrentDictionary<string, TcpClient> _clients = new();
        
        public async Task StartAsync(IPAddress ip, int port)
        {
            _listener = new TcpListener(ip, port);
            _listener.Start();
            Console.WriteLine($"Sunucu başlatıldı. IP: {ip} Port: {port}");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

  
        private async Task HandleClientAsync(TcpClient client)
        {
            string userId = "";
            try
            {
                var stream = client.GetStream();
                byte[] buffer = new byte[1024];

                // İlk gelen mesaj = kullanıcı adı
                int idLen = await stream.ReadAsync(buffer, 0, buffer.Length);
                userId = Encoding.UTF8.GetString(buffer, 0, idLen).Trim();

                Console.WriteLine($"{userId} bağlandı.");
                _clients.TryAdd(userId, client);

                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    string mesaj = Encoding.UTF8.GetString(buffer, 0, read);
                    string fullMessage = $"{userId} : {mesaj}";

                    Console.WriteLine(fullMessage);
                    // await BroadcastMessageAsync(fullMessage, userId);
                    await SendToRecipientAsync(mesaj, userId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata ({userId}): {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    _clients.TryRemove(userId, out _);
                    Console.WriteLine($"{userId} bağlantısı kapandı.");
                }
            }
    }
        private async Task SendToRecipientAsync(string rawMessage, string senderId)
        {
            // Beklenen format: senderId|recipientId|messageText
            string[] parts = rawMessage.Split('|');
            if (parts.Length == 3)
            {
                string toId = parts[1];

                if (_clients.TryGetValue(toId, out TcpClient recipientClient))
                {
                    try
                    {
                        byte[] data = Encoding.UTF8.GetBytes(rawMessage);
                        await recipientClient.GetStream().WriteAsync(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Mesaj gönderilemedi ({toId}): {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Alıcı ({toId}) bağlı değil.");
                }
            }
        }

        private async Task BroadcastMessageAsync(string message, string senderId)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            foreach (var (id, client) in _clients)
            {
                if (id != senderId)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                    }
                    catch { /* hata olursa sessiz geç */ }
                }
            }
        }
    }
}
