using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.VisualBasic;

namespace AServer
{
    public class Server
    {
        private readonly static int BufferSize = 4096;

        public static void Main()
        {
            try
            {
                new Server().Init();        // 서버 시작
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        private Dictionary<string, Socket> connectedClients = new();        // 소켓 값이 들어간다
        private Dictionary<string, Socket> connectedManagers = new();

        public Dictionary<string, Socket> ConnectedClients      
        {
            get => connectedClients;
            set => connectedClients = value;
        }

        public Dictionary<string, Socket> connectedManagers
        {
            get => connectedManagers;
            set => connectedManagers = value;
        }


        private Socket ServerSocket;

        private readonly IPEndPoint EndPoint = new(IPAddress.Parse("127.0.0.1"), 5001);

        int clientNum;
        Server()        // new Server 하는 순간 Server 생성자가 호출
        {
            ServerSocket = new(
                AddressFamily.InterNetwork,     
                SocketType.Stream,
                ProtocolType.Tcp
            );
            clientNum = 0;
        }


        void Init()
        {
            ServerSocket.Bind(EndPoint);
            ServerSocket.Listen(100);
            Console.WriteLine("유저를 기다리는 중입니다...");

            Accept();
        }


        void Accept()
        {
            do
            {
                Socket client = ServerSocket.Accept();
                Console.WriteLine($"유저가 접속하였습니다!: {client.RemoteEndPoint}");

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                client.ReceiveAsync(args);

            } while (true);
        }


        void Disconnected(Socket client)
        {
            Console.WriteLine($"유저가 방을 떠났습니다.: {client.RemoteEndPoint}");
            foreach (KeyValuePair<string, Socket> clients in connectedClients)
            {
                if (clients.Value == client)
                {
                    ConnectedClients.Remove(clients.Key);
                    clientNum--;
                }
            }
            foreach (KeyValuePair<string, Socket> clients in connectedManagers)
            {
                if (clients.Value == client)
                {
                    ConnectedManagers.Remove(clients.Key);
                }
            }
            client.Disconnect(false);
            client.Close();
        }


        void Received(object? sender, SocketAsyncEventArgs e)
        {
            Socket client = (Socket)sender!;
            byte[] data = new byte[BufferSize];
            try
            {
                int n = client.Receive(data);
                if (n > 0)
                {
                    MessageProc(client, data);

                    SocketAsyncEventArgs argsR = new SocketAsyncEventArgs();
                    argsR.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                    client.ReceiveAsync(argsR);
                }
                else { throw new Exception(); }
            }
            catch (Exception)
            {
                Disconnected(client);
            }
        }

        void MessageProc(Socket s, byte[] bytes)
        {
            string m = Encoding.Unicode.GetString(bytes);
            string[] tokens = m.Split(':');
            string message;

            string fromID;
            string toID;

            string code = tokens[0];

            if (code.Equals("ID"))
            {
                clientNum++;
                fromID = tokens[1].Trim();
                Console.WriteLine("[{0}번 유저] ID: {1}, {2}", clientNum, fromID, s.RemoteEndPoint);

                connectedClients.Add(fromID, s);
                connectedManagers.Add(fromID, s);

                message = $"ID: {fromID}:유저가 끝말잇기에 참가하였습니다.";
                s.Send(Encoding.Unicode.GetBytes("끝말잇기 채팅방에 입장하였습니다!"));
                Broadcast(s, message);
            }

            else if (code.Equals("BR"))
            {
                fromID = tokens[1].Trim();
                string msg = tokens[2];
                Console.WriteLine("{0} 유저의 답: {1}", fromID, msg)
                Broadcast(s, msg);
                s.Send(Encoding.Unicode.GetBytes("BR_Success!"));
            }

            else if (code.Equals("KICK"))
            {
                fromID= tokens[1].Trim();
                string msg = tokens[2];
                message = msg + "님이 강퇴당했습니다.";

                connectedClients[msg].Send(Encoding.Unicode.GetBytes("관리자에 의해 강퇴당했습니다."));
                Console.WriteLine($"유저가 방을 떠났습니다.: {connectedClients[msg].RemoteEndPoint}");

                connectedClients.Remove(msg);
                clientNum--;
                Broadcast(s, message);
                s.Send(Encoding.Unicode.GetBytes("강퇴 당했습니다."));
            }

            else
            {
                Broadcast(s, m);
            }
        }


        void SendTo(string id, string msg)
        {
            Socket socket;
            byte[] bytes = Encoding.Unicode.GetBytes(msg);

            if (connectedManagers.ContainsKey(id))
            {
                connectedManagers.TryGetValue(id, out socket!);
                try
                {
                    socket.Send(bytes);
                    socket.Send(Encoding.Unicode.GetBytes("[" + id + "] 유저에게 메시지를 전달하였습니다."));
                }
                catch { }
            }
        }


        void Broadcast(Socket s, string msg) // 모든 클라이언트에게 Send
        {
            byte[] bytes = Encoding.Unicode.GetBytes(msg);

            foreach (KeyValuePair<string, Socket> client in connectedClients.ToArray())
            {
                try
                {
                    // send
                    if (s != client.Value)
                        client.Value.Send(bytes);
                }
                catch (Exception)
                {
                    Disconnected(client.Value);
                }
            }
        }
    }
}