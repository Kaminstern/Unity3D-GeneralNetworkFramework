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
        static Socket socket;


        public static void Main(string[] args)
        {
            MsgBase.Register(MsgMove.Parser);
            MsgBase.Register(MsgAttack.Parser);

            NetManager.Listen("127.0.0.1", 8888);

            // 等待
            Console.ReadLine();
        }

        
    }
}