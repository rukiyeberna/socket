

# 🧠 C# .NET Remote Socket Messaging App

A real-time, cross-device remote messaging system developed using **C#**, **.NET Framework**, and **raw TCP sockets** — built entirely without `socket.io` or any third-party messaging protocols. The application is divided into two parts: a **WPF-based client** and a **console-based TCP server**, and demonstrates the implementation of a **custom communication protocol**, asynchronous networking via `NetworkStream`, and **stream-based image transfer**. Communication occurs through structured, delimited **message packets**, enabling both text and compressed screenshot delivery between remote devices over TCP.

---


## 🎯 Objective

This project was built to:

- Understand and implement **low-level socket communication** in .NET
- Gain hands-on experience with **WPF client development**
- Learn how to design and parse **custom message packet protocols**
- Build real-time client-server communication across remote machines

---

## 🔐 Notes & Considerations

- Pure socket implementation: no external libraries like `SignalR`, `socket.io`, etc.
- Image data is sent as Base64 string inside packet
- No message persistence — communication is live-only
- LAN or WAN compatible (requires port forwarding on host if over public internet)

---

## 💡 Key Highlights

| Area                     | Implementation                                                                 |
|--------------------------|----------------------------------------------------------------------------------|
| Protocol Design          | Custom delimited **message packets** over raw `NetworkStream`                  |
| Asynchronous Networking  | `TcpListener`, `TcpClient`, `Stream.ReadAsync/WriteAsync`                      |
| UI/UX                    | Built with **WPF**, dynamic textbox/image rendering, async-safe UI updates     |
| Remote Communication     | Clients connect from different machines using IP/Port over LAN/WAN             |
| Screenshot Transfer      | Compressed via `JPEG`, streamed as Base64                                      |
| Data Routing             | Server routes packets via `ConcurrentDictionary<string, TcpClient>`            |
| Error Handling           | Robust detection for malformed packets, invalid base64, disconnected clients   |

---

## 🧪 Technical Overview

### 🔌 Message Packet Structure

Each communication is encapsulated in a simple, custom **message packet**, manually constructed and transmitted via TCP:

```text
senderId:recipientId:payload
```

- `payload` can be a UTF-8 text message or a Base64-encoded image.

#### 📝 Examples

- `alice:bob:Hello from my laptop!`  
- `alice:bob:data:image/png;base64,iVBORw0K...`

---

### 🧠 Server Architecture

- Listens for TCP clients using `TcpListener`
- Accepts first incoming string as the `userId`
- Maps users in memory via thread-safe dictionary:
  ```csharp
  ConcurrentDictionary<string, TcpClient> _clients;
  ```
- Parses message packets and forwards to recipients
- Logs connection status and errors to console

---

### 🖥️ Client Features

- Built with **WPF (XAML + C#)**
- UI for entering:
  - User ID
  - Server IP and Port
  - Recipient ID and message
- Supports:
  - **Sending text messages**
  - **Capturing & sending full-screen images**
- Live preview of received image messages via `BitmapImage` stream binding

---

### 📷 Streamed Screenshot Flow

1. Capture screen using:
   ```csharp
   Graphics.CopyFromScreen(...)
   ```

2. Compress to JPEG in memory:
   ```csharp
   resizedBitmap.Save(MemoryStream, jpegCodec, encoderParams);
   ```

3. Convert to Base64 string and embed in message packet:
   ```csharp
   $"sender|recipient|data:image/png;base64,{Convert.ToBase64String(bytes)}"
   ```

4. Send over `NetworkStream.WriteAsync(...)`  
5. Receiver decodes stream and updates WPF UI image preview

✅ **All without saving temporary files — the entire process is stream-based.**

---

## 🖼️ Architecture

```
SocketMessagingApp/
├── SocketClientWpf/         # WPF-based UI client
│   ├── MainWindow.xaml(.cs) # UI, messaging, screenshot logic
│   ├── Client.cs            # Simple async TCP client
│   └── App.xaml / AssemblyInfo.cs
│
└── SocketServerHost/        # Console-based TCP server
    └── Program.cs           # Client manager, message router
```

---

## 🚀 How to Run

### 🖥️ 1. Start Server

```bash
cd SocketServerHost
dotnet run
```

Enter IP and port to listen on:

```text
Sunucu IP adresini girin: 0.0.0.0
Port numarasını girin: 5000
```

---

### 💻 2. Start WPF Client

- Open `SocketClientWpf` in Visual Studio
- Run the app
- Fill in:
  - Your User ID
  - Server IP and Port
  - Recipient ID
  - Optional message or screenshot trigger

---

## 📸 Demo gelcek



---



## 📄 License

MIT License

---

## 👩‍💻 Author

Developed by [rukiyeberna](https://github.com/rukiyeberna)  
A networking-focused project demonstrating end-to-end socket communication, protocol design, and real-time stream handling in C#.
