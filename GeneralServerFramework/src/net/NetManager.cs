using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using General.Protos;
using System.Runtime.CompilerServices;


namespace GeneralServerFramework;

public static class NetManager
{
    // 定义套接字
    static Socket socket;

    // 客户端Socket状态信息
    public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

    // 获取描述
    public static string GetDesc()
    {
        if (socket == null) { return ""; }
        if (!socket.Connected) { return ""; }
        return socket.LocalEndPoint.ToString();
    }

    // 连接
    public static void Listen(string ip, int port)
    {
        // Socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind
        IPAddress ipAdr = IPAddress.Parse(ip);
        IPEndPoint ipEp = new IPEndPoint(ipAdr, port);
        socket.Bind(ipEp);

        // Listen
        socket.Listen(0);     // 参数backlog表示队列中最多可容纳等待接受的连接数，0表示不限制

        Console.WriteLine("[服务器] 启动成功");
        InitProtocols();

        // Accept
        socket.BeginAccept(AcceptCallback, socket);
    }

    // 统一注册所有协议
    private static void InitProtocols()
    {
        MsgBase.Register(MsgMove.Parser);
        MsgBase.Register(MsgAttack.Parser);
        MsgBase.Register(MsgPing.Parser);
        MsgBase.Register(MsgPong.Parser);
    }

    // Accept回调
    private static void AcceptCallback(IAsyncResult ar)
    {
        try
        {
            Socket listenfd = (Socket)ar.AsyncState!;
            Socket clientfd = listenfd.EndAccept(ar);
            Console.WriteLine("[服务器] Accept");
            ClientState state = new ClientState();
            state.socket = clientfd;
            clients.Add(clientfd, state);

            // 接收数据
            clientfd.BeginReceive(state.readBuff.bytes, state.readBuff.writeIdx, state.readBuff.remain, 0, ReceiveCallback, state);

            // 继续Accept
            listenfd.BeginAccept(AcceptCallback, listenfd);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket Accept fail {ex.Message}");
        }
    }

    // Receive回调
    private static void ReceiveCallback(IAsyncResult ar)
    {

        ClientState state = (ClientState)ar.AsyncState!;
        Socket clientfd = state.socket;
        int count = 0;
        Console.WriteLine("回调");

        // 缓冲区不够，清除，若依旧不够，则返回
        if (state.readBuff.remain <= 0)
        {
            OnReceiveData(state);
            state.readBuff.MoveBytes();
        }
        if (state.readBuff.remain <= 0)
        {
            Console.WriteLine("Receive fail, maybe msg length > buff capacity");
            Close(state);
            return;
        }

        try
        {
            count = clientfd.EndReceive(ar);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Receive fail {ex.Message}");
            Close(state);
            return;
        }

        // 客户端关闭
        if (count <= 0)
        {
            Console.WriteLine($"Socket client {clientfd.RemoteEndPoint!.ToString()}");
            Close(state);
            return;
        }

        // Int16 bodyLength = (Int16)(state.readBuff.bytes[state.readBuff.readIdx] | state.readBuff.bytes[state.readBuff.readIdx+1] << 8);
        // int nameCount = 0;
        // string protoName = MsgBase.DecodeName(state.readBuff.bytes, 2, out nameCount);
        // IMessage msg = MsgBase.Decode(protoName, state.readBuff.bytes, nameCount + 2, bodyLength - nameCount);
        // MsgMove msgMove = msg as MsgMove;
        // Console.WriteLine($"msgMove.x = {msgMove.X}");

        // Console.WriteLine($"Received {BitConverter.ToString(state.readBuff.bytes, state.readBuff.readIdx, count)}");

        // 更新当前连接的这个Client缓存写入指针
        // 消息处理
        state.readBuff.writeIdx += count;

        // 处理二进制消息
        OnReceiveData(state);

        // 移动缓冲区
        state.readBuff.CheckAndMoveBytes();

        // 广播
        byte[] sendBytes = new byte[count];
        Array.Copy(state.readBuff.bytes, state.readBuff.readIdx, sendBytes, 0, state.readBuff.length);
        foreach (ClientState cs in clients.Values)
        {
            cs.socket.Send(sendBytes);
        }
        clientfd.BeginReceive(state.readBuff.bytes, state.readBuff.writeIdx, state.readBuff.remain, 0, ReceiveCallback, state);
    }



    // 发送
    public static void Send(string sendStr)
    {
        if (socket == null || !socket.Connected) { return; }
        byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
        socket.Send(sendBytes);
    }

    // 处理数据
    private static void OnReceiveData(ClientState socket)
    {

    }

    // 关闭连接
    public static void Close(ClientState state)
    {
        // 判断状态
        if (state.socket == null || !state.socket.Connected)
        {
            return;
        }
        if (state.isConnecting)
        {
            return;
        }
        if (state.writeQueue.Count > 0)
        {
            state.isClosing = true;
        }
        else
        {
            state.socket.Close();
            // 分发连接关闭的事件
            state.FireEvent(NetEvent.Cloas, "");
            clients.Remove(state.socket);
        }
    }
}
