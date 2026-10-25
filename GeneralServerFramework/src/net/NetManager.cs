using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using General.Protos;
using System.Collections.Concurrent;

namespace GeneralServerFramework;

public static class NetManager
{
    // 定义套接字
    static Socket socket;

    // 客户端Socket状态信息
    public static ConcurrentDictionary<Socket, ClientState> clients = new ConcurrentDictionary<Socket, ClientState>();

    private static readonly ConcurrentQueue<(ClientState state, IMessage msg)> msgQueue = new();

    // 每次Update时处理的消息量
    public static int MAX_MESSAGE_FIRE = 10;

    public static long lastTimerTime = 0;

    public const long TIMER_INTERVAL_MS = 1000;     // 1秒一次


    // 获取描述
    public static string GetDesc()
    {
        if (socket == null) { return ""; }
        if (!socket.Connected) { return ""; }
        return socket.LocalEndPoint.ToString();
    }

    // 连接
    public static void StartLoop(int port)
    {
        // Socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind
        IPAddress ipAdr = IPAddress.Parse("0.0.0.0");
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

        MsgDispatcher.Register(MsgMove.Descriptor.FullName, MsgHandler.MsgMove);

        MsgDispatcher.Register(MsgPing.Descriptor.FullName, MsgHandler.MsgPing);
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
            if (!clients.TryAdd(clientfd, state))
            {
                Console.WriteLine($"重复添加 socket: {clientfd.RemoteEndPoint}");
                clientfd.Close();
                return;
            }

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

        state.lastPingTime = Environment.TickCount64;

        // 更新当前连接的这个Client缓存写入指针
        // 消息处理
        state.readBuff.writeIdx += count;

        // 处理二进制消息
        OnReceiveData(state);

        // 移动缓冲区
        state.readBuff.CheckAndMoveBytes();

        // 广播
        // byte[] sendBytes = new byte[count];
        // Array.Copy(state.readBuff.bytes, state.readBuff.readIdx, sendBytes, 0, state.readBuff.length);
        // foreach (ClientState cs in clients.Values)
        // {
        //     cs.socket.Send(sendBytes);
        // }
        clientfd.BeginReceive(state.readBuff.bytes, state.readBuff.writeIdx, state.readBuff.remain, 0, ReceiveCallback, state);
    }

    // 处理数据
    private static void OnReceiveData(ClientState state)
    {
        ByteArray readBuff = state.readBuff;
        byte[] bytes = readBuff.bytes;
        // 消息长度
        if (readBuff.length < 2)
        {
            return;
        }
        int readIdx = readBuff.readIdx;
        Int16 bodyLength = (Int16)(bytes[readIdx] | bytes[readIdx + 1] << 8);

        // 消息体
        if (readBuff.length < bodyLength + 2)
        {
            return;
        }
        readBuff.readIdx += 2;

        // 解析协议名
        string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIdx, out int nameCount);
        if (String.IsNullOrWhiteSpace(protoName))
        {
            Console.WriteLine("OnReceiveData MsgBase.DecodeName fail");
            Close(state);
            return;
        }
        readBuff.readIdx += nameCount;

        // 解析协议体
        int bodyCount = bodyLength - nameCount;
        IMessage msgBase = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIdx, bodyCount);
        readBuff.readIdx += bodyCount;
        readBuff.CheckAndMoveBytes();

        // 添加到消息队列
        msgQueue.Enqueue((state, msgBase));
        Console.WriteLine($"Receive {protoName}");

        // 分发消息 → HandleMessage去做业务的处理和分发

        // 继续读取消息
        if (readBuff.length > 2)
        {
            OnReceiveData(state);
        }
    }

    // 发送
    public static void Send(ClientState cs, IMessage msg)
    {
        // 状态判断
        if (cs == null || !cs.socket.Connected)
        {
            return;
        }
        if (cs.isConnecting || cs.isClosing)
        {
            return;
        }


        // 数据编码（bytes前两位表示后面包含协议名与消息体的长度）
        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[2 + len];

        // 组装长度（和客户端一样的小端存储（先存数据的低位，再存数据的高位））
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);

        // 组装协议名
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        // 组装消息体
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

        // 写入队列
        ByteArray ba = new ByteArray(sendBytes);
        int count = 0;
        lock (cs.writeQueue)
        {
            cs.writeQueue.Enqueue(ba);
            count = cs.writeQueue.Count;
        }

        if (count == 1)
        {
            cs.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, cs);
        }
    }

    public static void SendCallback(IAsyncResult ar)
    {
        // 获取state、EndSend的处理
        ClientState state = (ClientState)ar.AsyncState;
        // 状态判断
        if (state == null || state.socket == null || !state.socket.Connected)
        {
            return;
        }
        int count = state.socket.EndSend(ar);
        // 获取写入队列第一条数据
        ByteArray ba;
        lock (state.writeQueue)
        {
            ba = state.writeQueue.First();
        }

        // 完整发送
        ba.readIdx += count;
        if (ba.length == 0)
        {
            lock (state.writeQueue)
            {
                state.writeQueue.Dequeue();     // 前面ba已经拿了队头数据，如果长度为0，表示为空，可以拿下一个数据，现在对头的空元素就可以出队了
                ba = state.writeQueue.Count > 0 ? state.writeQueue.First() : null;
            }
        }
        // 继续发送
        if (ba != null)
        {
            state.socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, state);
        }
        else if (state.isClosing)    // 正在关闭
        {
            state.socket.Close();
        }

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
            EventHandler.OnDisconnect(state);
            clients.TryRemove(state.socket, out _);
        }
    }

    public static void Update()
    {
        MsgUpdate();
        Timer();
    }

    // 和客户端一样，需要一个消息更新的操作
    public static void MsgUpdate()
    {
        for (int i = 0; i < MAX_MESSAGE_FIRE; i++)
        {
            if (!msgQueue.TryDequeue(out var item))
            {
                return;
            }

            HandleMessage(item.state, item.msg);
        }
    }

    // 业务处理入口
    private static void HandleMessage(ClientState state, IMessage msg)
    {
        MsgDispatcher.Dispatch(state, msg);
    }

    private static void Timer()
    {
        long now = Environment.TickCount64;

        // 主循环sleep(1)，这里控制1秒节拍
        if (now - lastTimerTime < TIMER_INTERVAL_MS)
        {
            return;
        }

        lastTimerTime = now;

        // 服务端定时任务
    }
}
