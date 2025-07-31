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

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();


        public MainWindow()
        {
            InitializeComponent();

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);

        }

        private byte[] CompressImage(System.Drawing.Bitmap originalBitmap, long quality = 50L)
        {
            try
            {
                // Görüntüyü yeniden boyutlandır (opsiyonel - performans için)
                int newWidth = Math.Min(originalBitmap.Width, 1024);
                int newHeight = (int)(originalBitmap.Height * ((double)newWidth / originalBitmap.Width));

                using (var resizedBitmap = new System.Drawing.Bitmap(originalBitmap, newWidth, newHeight))
                {
                    // JPEG encoder ve kalite parametresi
                    ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                    EncoderParameters encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                    using (var ms = new MemoryStream())
                    {
                        resizedBitmap.Save(ms, jpegCodec, encoderParams);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Görüntü sıkıştırma hatası: " + ex.Message);
                // Hata durumunda orijinal PNG formatında döndür
                using (var ms = new MemoryStream())
                {
                    originalBitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        // JPEG encoder'ı bul
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {

                int width = (int)SystemParameters.VirtualScreenWidth;
                int height = (int)SystemParameters.VirtualScreenHeight;

                using (var bmp = new System.Drawing.Bitmap(width, height))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen((int)SystemParameters.VirtualScreenLeft,
                                         (int)SystemParameters.VirtualScreenTop,
                                         0, 0,
                                         bmp.Size);
                    }

                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);

                        // _lastScreenshotBytes = ms.ToArray();
                        // Görüntüyü sıkıştır
                        //anlık oluyo bu bu değil
                        //double originalSizeKB = ms.Length / 1024.0;
                        //Recieve.AppendText($"Orijinal görüntü boyutu: {originalSizeKB:F1} KB\n");

                        _lastScreenshotBytes = CompressImage(bmp, 60L); // %60 kalite

                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        // image.StreamSource = ms;
                        image.StreamSource = new MemoryStream(_lastScreenshotBytes);
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();

                        PreviewImage.Source = image;
                        PreviewImage.Visibility = Visibility.Visible;
                    }


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ekran görüntüsü alınırken hata: " + ex.Message);
            }
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

                if (string.IsNullOrWhiteSpace(recipientId))
                {
                    MessageBox.Show("Alıcı ID boş olamaz.");
                    return;
                }

                string formattedMessage;
                if (!string.IsNullOrWhiteSpace(mesaj))
                {
                  
                    formattedMessage = $"{_userId}|{recipientId}|{mesaj}";

                }
                else if (_lastScreenshotBytes != null)
                {
                    // Görsel gönder
                    string base64String = Convert.ToBase64String(_lastScreenshotBytes);
                   
                    double originalSizeKB = _lastScreenshotBytes.Length / 1024.0;
                    Recieve.AppendText($"Orijinal görüntü boyutu: {originalSizeKB:F1} KB\n");

                    //formattedMessage = $"{_userId}|{recipientId}|[IMAGE]{base64String}";
                    formattedMessage = $"{_userId}|{recipientId}|data:image/png;base64,{base64String}";


                }
                else
                {
                    MessageBox.Show("Gönderilecek mesaj veya ekran görüntüsü yok.");
                    return;
                }

                //if (string.IsNullOrWhiteSpace(mesaj) || string.IsNullOrWhiteSpace(recipientId))
                //{
                //    MessageBox.Show("Mesaj ve Alıcı ID boş olamaz.");
                //    return;
                //}
                //bozma

                // string formattedMessage = $"{_userId}|{recipientId}|{mesaj}";
                byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
                await _stream.WriteAsync(data, 0, data.Length);

                Message.Clear();

                if (_lastScreenshotBytes != null && string.IsNullOrWhiteSpace(mesaj))
                {
                    double sizeKB = _lastScreenshotBytes.Length / 1024.0;
                    Recieve.AppendText($"Görüntü gönderildi (Boyut: {sizeKB:F1} KB)\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gönderim hatası: " + ex.Message);
            }
        }

        private async Task ReceiveMessages()
        {
            try
            {
                byte[] buffer = new byte[1048576];
                MemoryStream memoryStream = new MemoryStream();

                while (_client.Connected)
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    // Gelen veriyi MemoryStream'e ekle
                    memoryStream.Write(buffer, 0, read);

                    // MemoryStream'deki veriyi string'e çevir
                    string currentData = Encoding.UTF8.GetString(memoryStream.ToArray());

                    var parts = currentData.Split('|');
                    if (parts.Length >= 3)
                    {
                        // Eğer resim mesajıysa, base64 stringin tam gelip gelmediğini kontrol et
                        string messageText = parts[2];
                        bool isCompleteMessage = true;

                        if (messageText.StartsWith("data:image/png;base64,") || messageText.StartsWith("data:image/jpeg;base64,"))
                        {
                            string base64Part = messageText.StartsWith("data:image/png;base64,")
                                ? messageText.Substring("data:image/png;base64,".Length)
                                : messageText.Substring("data:image/jpeg;base64,".Length);

                            // Base64 stringin tam olup olmadığını kontrol et
                            try
                            {
                                // Base64 decode işlemi başarılı olursa mesaj tamdır
                                Convert.FromBase64String(base64Part);
                            }
                            catch
                            {
                                // Decode başarısız ise mesaj henüz tam gelmemiş
                                isCompleteMessage = false;
                            }
                        }

                        if (isCompleteMessage)
                        {
                            string senderId = parts[0];
                            string recipientId = parts[1];

                            if (recipientId == _userId)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        if (messageText.StartsWith("data:image/png;base64,") || messageText.StartsWith("data:image/jpeg;base64,"))
                                        {
                                            // Base64 stringinden resim oluştur
                                            string base64String = messageText.StartsWith("data:image/png;base64,")
                                                ? messageText.Substring("data:image/png;base64,".Length)
                                                : messageText.Substring("data:image/jpeg;base64,".Length);

                                            byte[] imageBytes = Convert.FromBase64String(base64String);

                                            BitmapImage image = new BitmapImage();
                                            using (MemoryStream ms = new MemoryStream(imageBytes))
                                            {
                                                image.BeginInit();
                                                image.StreamSource = ms;
                                                image.CacheOption = BitmapCacheOption.OnLoad;
                                                image.EndInit();
                                            }

                                            PreviewImage.Source = image;
                                            PreviewImage.Visibility = Visibility.Visible;

                                            Recieve.AppendText($"From {senderId}: [Resim alındı]{Environment.NewLine}");
                                        }
                                        else
                                        {
                                            PreviewImage.Visibility = Visibility.Collapsed;
                                            Recieve.AppendText($"From {senderId}: {messageText}{Environment.NewLine}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Recieve.AppendText($"Mesaj işlenirken hata: {ex.Message}{Environment.NewLine}");
                                    }
                                });
                            }

                            
                            memoryStream.SetLength(0);
                            memoryStream.Position = 0;
                        }
                    }
                }

                memoryStream.Dispose();
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

        int i = 0;
        private byte[] _lastScreenshotBytes;

        private void ScreenCaptureImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            if (i == 0)
            {

                dispatcherTimer.Start();
                i = 1;
            }
            else
            {

                dispatcherTimer.Stop();
                i = 0;
            }


        }






    }
}
