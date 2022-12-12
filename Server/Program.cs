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
                new Server().Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        private Dictionary<string, Socket> connectedClients = new();

        public Dictionary<string, Socket> ConnectedClients
        {
            get => connectedClients;
            set => connectedClients = value;
        }

        private Socket ServerSocket;

        private readonly IPEndPoint EndPoint = new(IPAddress.Parse("127.0.0.1"), 5001);

        int clientNum;
        
        Server()
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
            List<string> uList = new List<string>(connectedClients.Keys);
            string m = Encoding.Unicode.GetString(bytes);
            string[] tokens = m.Split(':');
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = tokens[i].Replace("\0", "").Trim();
            }
            string message;

            string fromID;
            string toID;

            string code = tokens[0];

            if (code.Equals("ID"))
            {
                clientNum++;
                fromID = tokens[1];
                Console.WriteLine("[{0}번 유저] ID: {1} - {2}", clientNum, fromID, s.RemoteEndPoint);

                connectedClients.Add(fromID, s);

                message = $"ID: {fromID} 유저가 채팅방에 참가하였습니다.";
                s.Send(Encoding.Unicode.GetBytes("채팅방 입장 성공"));
                Broadcast(s, message);
            }

            else if (code.Equals("HELP"))
            {
                fromID = tokens[1];
                Console.WriteLine("\n{0} 유저가 도움말을 요청하였습니다.", fromID);
                s.Send(Encoding.Unicode.GetBytes("도움말 요청 성공"));
            }

            else if (code.Equals("LIST"))
            {
                fromID = tokens[1];
                message = $"[{fromID}] 유저가 회원 리스트 보기를 요청하였습니다.";
                Console.WriteLine("\n" + message);

                for (int i = 0; i < uList.Count; i++)
                {
                    if (uList[i] == fromID)
                    {
                        uList.RemoveAt(i);
                    }
                }
                string[] userList = uList.ToArray();
                Console.Write("회원 리스트 목록:");
                for (int i = 0; i < userList.Length; i++)
                {
                    Console.Write("{0} ", userList[i]);
                }
                s.Send(Encoding.Unicode.GetBytes("회원 리스트 요청 성공"));
            }

            else if (code.Equals("TO"))
            {
                try
                {
                    fromID = tokens[1];
                    toID = tokens[2];
                    string msg = tokens[3];
                    if (connectedClients.ContainsKey(toID))
                    {
                        string rMsg = "\n[" + fromID + "]님이 [" + toID + "]님에게 귓속말을 전송하였습니다. \n"
                                      + "귓속말 내용: " + msg;
                        Console.WriteLine(rMsg);

                        SendTo(toID, m);
                        s.Send(Encoding.Unicode.GetBytes("귓속말 전송 성공"));
                    }
                    else
                    {
                        s.Send(Encoding.Unicode.GetBytes("오류 메시지! - 유저가 존재하지 않습니다."));
                        Console.WriteLine("{0} 유저는 존재하지 않습니다!", toID);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            else if (code.Equals("BR"))
            {
                fromID = tokens[1];
                string msg = tokens[2];
                Console.WriteLine("{0} 유저의 전체 메시지: {1}", fromID, msg);
                m = "[" + fromID + "]님의 전체 메시지: " + msg;
                Broadcast(s, m);
                s.Send(Encoding.Unicode.GetBytes("전체 전송 성공"));
            }
            
            else if (code.Equals("EXIT"))
            {
                fromID = tokens[1];
                Console.WriteLine("{0} 유저가 채팅방을 나갔습니다.", fromID);
                m = "유저 [" + fromID + "] 님이 채팅방을 떠났습니다.";
                Broadcast(s, m);
                s.Send(Encoding.Unicode.GetBytes("채팅방 퇴장 성공"));
                connectedClients.Remove(fromID);
                clientNum--;
            }

            else if (code.Equals("KICK"))
            {
                string kickID = tokens[1];
                try
                {
                    string kickMsg = kickID + "님이 강퇴당했습니다.";
                    connectedClients[kickID].Send(Encoding.Unicode.GetBytes("강퇴당했습니다."));
                    connectedClients.Remove(kickID);
                    clientNum--;
                    Console.WriteLine(kickMsg);
                    Broadcast(s, kickMsg);
                    s.Send(Encoding.Unicode.GetBytes("강퇴 처리 완료"));
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            else
            {
                Broadcast(s, m);
            }
        }

        void SendTo(string id, string msg)
        {
            Socket socket;
            string[] tokens = msg.Split(":");
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = tokens[i].Replace("\0", "").Trim();
            }
            string message = tokens[1] + "님의 귓속말: " + tokens[3];
            byte[] data = Encoding.Unicode.GetBytes(message);

            if (connectedClients.ContainsKey(id))
            {
                connectedClients.TryGetValue(id, out socket!);
                try { socket.Send(data); } catch { }
            }
        }

        void Broadcast(Socket s, string msg)
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