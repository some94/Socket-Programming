using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    public class Client
    {
        private const int BufferSize = 4096;

        public static void Main()
        {
            try
            {
                new Client().Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Press any key to exit the program.");
            Console.ReadKey();
        }


        private Socket clientSocket;
        public Socket ClientSocket
        {
            get => clientSocket;
            set => clientSocket = value;
        }
        private readonly IPEndPoint EndPoint = new(IPAddress.Parse("127.0.0.1"), 5001);

        Client()
        {
            ClientSocket = new(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
        }

        void Init()
        {
            ClientSocket.Connect(EndPoint);
            Console.WriteLine($"서버와 연결되었습니다..");

            // Received를 대기하고 있는 상태
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
            ClientSocket.ReceiveAsync(args);

            Send();
        }

        void Received(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                byte[] data = new byte[BufferSize];
                Socket server = (Socket)sender!;
                int n = server.Receive(data);

                string str = Encoding.Unicode.GetString(data);
                str = str.Replace("\0", "").Trim();
                Console.WriteLine("- " + str);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                ClientSocket.ReceiveAsync(args);
            }
            catch (Exception)
            {
                Console.WriteLine($"서버와 연결이 끊겼습니다..");
                ClientSocket.Close();
            }
        }

        void Send()
        {
            byte[] dataID;
            Console.WriteLine("사용하실 ID를 입력해주세요.");

            string nameID = Console.ReadLine()!;
            string message = "ID:" + nameID + ":";
            dataID = Encoding.Unicode.GetBytes(message);
            clientSocket.Send(dataID);

            Console.WriteLine("[{0}]님 환영합니다! 아래의 메시지 포맷을 참고하세요. \n\n"
                + "--------------------------------------------------------------------\n"
                + "1. 도움말 보기 --> HELP:본인ID \n"
                + "2. 채팅방 유저 리스트 보기 --> LIST:본인ID \n"
                + "3. 귓속말 보내기 --> TO:상대방ID:메시지 \n"
                + "4. 전체 전송 메시지 보내기 --> BR:메시지 내용 \n"
                + "5. 채팅방 나가기 --> EXIT:본인ID \n"
                + "6. 강퇴 하기 --> KICK:강퇴할 상대방ID\n"
                + "--------------------------------------------------------------------\n\n", nameID.Trim());
            do
            {
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split(':');
                string m;

                if (tokens[0].Equals("HELP"))
                {
                    m = "HELP:" + tokens[1];
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[{0}]님 환영합니다! 아래의 메시지 포맷을 참고하세요. \n\n"
                + "--------------------------------------------------------------------\n"
                + "1. 도움말 보기 --> HELP:본인ID \n"
                + "2. 채팅방 유저 리스트 보기 --> LIST:본인ID \n"
                + "3. 귓속말 보내기 --> TO:상대방ID:메시지 \n"
                + "4. 전체 전송 메시지 보내기 --> BR:메시지 내용 \n"
                + "5. 채팅방 나가기 --> EXIT:본인ID \n"
                + "6. 강퇴 하기 --> KICK:강퇴할 상대방ID\n"
                + "--------------------------------------------------------------------\n\n", tokens[1].Trim());
                    try { ClientSocket.Send(data); } catch { }
                }

                else if (tokens[0].Equals("LIST"))
                {
                    m = "LIST:" + tokens[1];
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("서버에서 회원 리스트 목록을 확인하세요.");
                    try { ClientSocket.Send(data); } catch { }
                }

                else if (tokens[0].Equals("TO"))
                {
                    m = "TO:" + nameID + ":" + tokens[1] + ":" + tokens[2];
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[{0}님에게 귓속말 전송 완료] - 보낸 내용: {1}", tokens[1].Trim(), tokens[2]);
                    try { ClientSocket.Send(data); } catch { }
                }

                else if (tokens[0].Equals("BR"))
                {
                    m = "BR:" + nameID + ": " + tokens[1];
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[전체 전송]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { }
                }

                else if (tokens[0].Equals("EXIT"))
                {
                    m = "EXIT:" + tokens[1];
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("=========채팅방에서 나왔습니다.=========");
                    try { ClientSocket.Send(data); } catch { }
                }

                else if (tokens[0].Equals("KICK"))
                {
                    m = "KICK:" + tokens[1];
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[강퇴 요청이 접수되었습니다] - 대상자: {0}", tokens[1].Trim());
                    try { ClientSocket.Send(data); } catch { }
                }
            } while (true);
        }
    }
}
