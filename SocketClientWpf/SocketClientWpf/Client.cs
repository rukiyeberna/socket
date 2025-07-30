using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketClientWpf
{
    public class Client
    {
        public async Task<string> GonderAsync(string ip, int port, string mesaj)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(ip, port);

            NetworkStream stream = client.GetStream();
            byte[] mesajBytes = Encoding.UTF8.GetBytes(mesaj);
            await stream.WriteAsync(mesajBytes, 0, mesajBytes.Length);

            byte[] buffer = new byte[1024];
            int okunan = await stream.ReadAsync(buffer, 0, buffer.Length);
            string cevap = Encoding.UTF8.GetString(buffer, 0, okunan);

            return cevap;
        }
    }
}
