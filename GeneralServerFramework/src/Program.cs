using General.Protos;
using Google.Protobuf;
using System.Net;
using System.Net.Sockets;

namespace GeneralServerFramework
{
    /// <summary>
    /// 服务器经历Socket、Bind、Listen三个步骤初始化监听Socket，然后调用BeginAccept开始异步处理客户端连接
    /// 商业上为了做到性能极致，大多使用异步，或使用多线程模拟异步程序。后续书中提到的服务端使用select，尝试改为异步实现
    /// </summary>
    class MainClass
    {
        // 监听Socket
        static Socket listenfd;
        // 获取所有连接的客户端Socket和状态信息
        static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

        public static void Main(string[] args)
        {
            MsgBase.Register(MsgMove.Parser);
            MsgBase.Register(MsgAttack.Parser);

            // Socket
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8888);
            listenfd.Bind(ipEp);

            // Listen
            listenfd.Listen(0);     // 参数backlog表示队列中最多可容纳等待接受的连接数，0表示不限制
            Console.WriteLine("[服务器] 启动成功");

            // Accept
            listenfd.BeginAccept(AcceptCallback, listenfd);

            // 等待
            Console.ReadLine();
        }

        // Accept回调
        public static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("[服务器] Accept");
                Socket listenfd = (Socket)ar.AsyncState!;
                Socket clientfd = listenfd.EndAccept(ar);

                ClientState state = new ClientState();
                state.socket = clientfd;
                clients.Add(clientfd, state);
                // 接收数据
                clientfd.BeginReceive(state.readBuff, 0, 1024, 0, ReceiveCallback, state);

                // 继续Accept
                listenfd.BeginAccept(AcceptCallback, listenfd);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket Accept fail {ex.Message}");
            }
        }

        // Receive回调
        public static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("回调");
                ClientState state = (ClientState)ar.AsyncState!;
                Socket clientfd = state.socket;
                int count = clientfd.EndReceive(ar);

                // 客户端关闭
                if (count == 0)
                {
                    clientfd.Close();
                    clients.Remove(clientfd);
                    Console.WriteLine("Socket close");
                    return;
                }

                //Int16 bodyLength = (Int16)(state.readBuff[0] | state.readBuff[1] << 8);
                //int nameCount = 0;
                //string protoName = MsgBase.DecodeName(state.readBuff, 2, out nameCount);
                //IMessage msg = MsgBase.Decode(protoName, state.readBuff, nameCount + 2, bodyLength - nameCount);
                //MsgMove msgMove = msg as MsgMove;
                //Console.WriteLine($"msgMove.x = {msgMove.X}");

                Console.WriteLine($"Received {BitConverter.ToString(state.readBuff, 0, count)}");
                // 广播
                byte[] sendBytes = new byte[count];
                Array.Copy(state.readBuff, 0, sendBytes, 0, count);
                foreach (ClientState cs in clients.Values)
                {
                    cs.socket.Send(sendBytes);
                }
                clientfd.BeginReceive(state.readBuff, 0, 1024, 0, ReceiveCallback, state);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket Receive fail {ex.Message}");
            }
        }
    }
}