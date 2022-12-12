using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
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

        public Dictionary<string, Socket> ConnectedClients
        {
            get => connectedClients;
            set => connectedClients = value;
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
                Console.WriteLine($"유저가 접속하였습니다! - {client.RemoteEndPoint}");

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                client.ReceiveAsync(args);

            } while (true);
        }


        void Disconnected(Socket client)
        {
            Console.WriteLine($"유저가 방을 떠났습니다 - {client.RemoteEndPoint}.");
            foreach (KeyValuePair<string, Socket> clients in connectedClients)
            {
                if (clients.Value == client)
                {
                    ConnectedClients.Remove(clients.Key);
                    clientNum--;
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
                Console.WriteLine("[{0}번 유저] ID: {1} - {2}", clientNum, fromID, s.RemoteEndPoint);

                connectedClients.Add(fromID, s);

                message = $"ID: {fromID} 유저가 채팅방에 참가하였습니다.";
                s.Send(Encoding.Unicode.GetBytes("채팅방에 입장하였습니다!"));
                Broadcast(s, message);
            }

            else if (code.Equals("SOS"))
            {
                fromID = tokens[1].Trim();
                message = $"[{fromID} 유저가 도움말을 요청하였습니다.";
                Console.WriteLine(message);
                s.Send(Encoding.Unicode.GetBytes("도움말 요청 성공"));
            }

            else if (code.Equals("LIST"))
            {
                List<string> uList = new List<string>(connectedClients.Keys);
                fromID = tokens[1].Trim();
                message = $"[{fromID}] 유저가 회원 리스트 보기를 요청하였습니다.";
                Console.WriteLine(message);

                for (int i = 0; i < uList.Count; i++)
                {
                    if (uList[i] == fromID)
                    {
                        uList.RemoveAt(i);
                    }
                }
                string[] userList = uList.ToArray();
                Console.Write("회원 리스트: {0}", userList);
                s.Send(Encoding.Unicode.GetBytes("회원 리스트 요청 성공"));
            }

            else if (code.Equals("TO"))
            {
                fromID = tokens[1].Trim();
                toID = tokens[2].Trim();
                string msg = tokens[3];
                string rMsg = "[" + fromID + "]님이 [" + toID + "]님에게 귓속말을 전송하였습니다. \n"
                    + "귓속말 내용: " + msg;
                Console.WriteLine(rMsg);

                SendTo(toID, m);
                s.Send(Encoding.Unicode.GetBytes("귓속말 전송 성공"));
            }

            else if (code.Equals("BR"))
            {
                fromID = tokens[1].Trim();
                string msg = tokens[2];
                Console.WriteLine("{0} 유저의 메시지: {1}", fromID, msg);
                m = "[" + fromID + "]님의 메시지: " + msg;
                Broadcast(s, m);
                s.Send(Encoding.Unicode.GetBytes("전체 전송 성공"));
            }

            else if (code.Equals("KICK"))
            {
                Socket socket;
                toID = tokens[1];
                try
                {
                    if (connectedClients.ContainsKey(toID))
                    {
                        connectedClients.TryGetValue(toID, out socket!);
                        foreach (KeyValuePair<string, Socket> clients in connectedClients)
                        {
                            if (clients.Value == socket)
                            {
                                ConnectedClients.Remove(clients.Key);
                                clientNum--;
                            }
                        }
                        socket.Disconnect(false);
                        socket.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                message = toID + "님이 강퇴 처리 되어 서버를 떠났습니다.";
                Console.WriteLine(message);
                Broadcast(s, message);
                s.Send(Encoding.Unicode.GetBytes("강퇴 요청 처리 완료"));
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
            string m = Encoding.Unicode.GetString(bytes);
            string[] tokens = m.Split(":");
            string message = tokens[1] + "님의 귓속말: " + tokens[3];
            byte[] data = Encoding.Unicode.GetBytes(message);

            if (connectedClients.ContainsKey(id))
            {
                connectedClients.TryGetValue(id, out socket!);
                try { socket.Send(data); } catch { }
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