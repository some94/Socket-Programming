using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;


namespace AClient
{
    public class Client
    {
        private readonly static int BufferSize = 4096;

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

        public Client()
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
            Console.WriteLine($"Server connected.");

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
                str = str.Replace("\0", "");
                Console.WriteLine("수신:" + str); // 여기서 Server 처럼 Split 해서 포멧 바꾸면 됨

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                ClientSocket.ReceiveAsync(args);
            }
            catch (Exception)
            {
                Console.WriteLine($"Server disconnected.");
                ClientSocket.Close();
            }
        }

        void Send()
        {
            byte[] dataID;
            Console.WriteLine("ID를 입력하세요");
            string nameID = Console.ReadLine()!;
            string message = "ID:" + nameID + ":";
            dataID = Encoding.Unicode.GetBytes(message);
            clientSocket.Send(dataID);

            Console.WriteLine("특정 사용자에게 보낼 때는 사용자ID:메시지 로 입력하시고\n" +
                "브로드캐스트하려면 BR:메시지 를 입력하세요");
            do
            {
                byte[] data;
                string msg = Console.ReadLine()!;
                string[] tokens = msg.Split(':');
                string m;
                if (tokens[0].Equals("BR"))
                {
                    m = "BR:" + nameID + ":" + tokens[1] + ":";

                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[전체전송]{0}", tokens[1]);
                    try { ClientSocket.Send(data); } catch { }
                }

                else //  (tokens[0].Equals("TO"))
                {
                    m = "TO:" + nameID + ":" + tokens[0] + ":" + tokens[1] + ":";
                    data = Encoding.Unicode.GetBytes(m);
                    Console.WriteLine("[{0}에게 전송]:{1}", tokens[0], tokens[1]);
                    try { ClientSocket.Send(data); } catch { }
                }
            } while (true);
        }
    }
}
