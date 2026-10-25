using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
using Google.Protobuf;
using General.Protos;

namespace GeneralClientFramework
{
    // 事件
    public enum NetEvent
    {
        ConnectSucc = 1,
        ConnectFail = 2,
        Cloas = 3
    }


    // 网络模块
    public static class NetManager
    {
        // 定义套接字
        static Socket socket;
        // 定义缓冲区
        static ByteArray readBuff;
        // 写入队列
        static Queue<ByteArray> writeQueue;


        // 是否正在连接
        static bool isConnecting = false;
        // 是否正在关闭
        static bool isClosing = false;

        // 消息列表
        static List<IMessage> msgList = new List<IMessage>();
        // 消息列表长度
        static int msgCount = 0;
        // 每次Update时处理的消息量
        readonly static int MAX_MESSAGE_FIRE = 10;

        // 是否启用心跳
        public static bool isUsePing = true;
        // 心跳间隔
        public static int pingInterval = 10;
        // 上一次发送PING的时间
        static float lastPingTime = 0;
        // 上一次收到PONG的时间
        static float lastPongTime = 0;


        // 事件委托类型
        public delegate void EventListener(string err);

        // 事件监听列表
        public static Dictionary<NetEvent, EventListener> eventListeners = new Dictionary<NetEvent, EventListener>();

        // 添加事件监听
        public static void AddEventListener(NetEvent netEvent, EventListener listener)
        {
            // 添加事件
            if (eventListeners.ContainsKey(netEvent))
            {
                eventListeners[netEvent] += listener;
            }
            else // 新增事件
            {
                eventListeners[netEvent] = listener;
            }
        }

        // 删除监听事件
        public static void RemoveEventListener(NetEvent netEvent, EventListener listener)
        {
            if (eventListeners.ContainsKey(netEvent))
            {
                eventListeners[netEvent] -= listener;
                // 删除，否则eventListeners.ContainsKey(netEvent)还是返回true
                if (eventListeners[netEvent] == null)
                {
                    eventListeners.Remove(netEvent);
                }
            }
        }

        // 分发事件
        public static void FireEvent(NetEvent netEvent, string err)
        {
            if (eventListeners.ContainsKey(netEvent))
            {
                eventListeners[netEvent](err);
            }
        }



        // 消息委托类型
        public delegate void MsgListener(IMessage msgBase);

        // 消息监听列表
        public static Dictionary<string, MsgListener> msgListeners = new Dictionary<string, MsgListener>();

        // 添加消息监听
        public static void AddMsgListener(string name, MsgListener listener)
        {
            // 添加
            if (msgListeners.ContainsKey(name))
            {
                msgListeners[name] += listener;
            }
            // 新增
            else
            {
                msgListeners[name] = listener;
            }
        }

        // 删除消息监听
        public static void RemoveMsgListener(string name, MsgListener listener)
        {
            if (msgListeners.ContainsKey(name))
            {
                msgListeners[name] -= listener;
                if (msgListeners[name] == null)
                {
                    msgListeners.Remove(name);
                }
            }
        }

        // 分发消息
        public static void FireMsg(string name, IMessage msg)
        {
            if (msgListeners.ContainsKey(name))
            {
                msgListeners[name](msg);
            }
        }

        // 连接
        public static void Connect(string ip, int port)
        {
            InitProtocols();
            // 判断状态
            if (socket != null && socket.Connected)
            {
                Debug.Log("Connect fail, already connected");
                return;
            }
            if (isConnecting)
            {
                Debug.Log("Connect fail, isConnecting");
                return;
            }
            // 初始化成员
            InitState();
            // 参数设置
            socket.NoDelay = true;     // 不使用Nagle算法，不将小数据量的tcp整合为一个大的再发送
            // connect
            isConnecting = true;
            socket.BeginConnect(ip, port, ConnectCallback, socket);
        }

        // 重置缓冲区，防止客户端再次重连时读取到上一次还未处理的readbuff数据
        private static void InitState()
        {
            // Socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 接收缓冲区
            readBuff = new ByteArray();
            // 写入队列
            writeQueue = new Queue<ByteArray>();
            // 重置连接状态
            isConnecting = false;
            // 是否正在关闭
            isClosing = false;
            msgList = new List<IMessage>();
            msgCount = 0;

            // 上一次发送Ping的时间
            lastPingTime = Time.time;
            // 上一次收到Pong的时间
            lastPongTime = Time.time;

            // 客户端可能与服务端断线重连，InitState可能被多次调用，但MsgPong协议无须多次监听，代码判断监听列表中是否以及存在MsgPong协议监听
            // 监听PONG协议
            if (!msgListeners.ContainsKey(typeof(MsgPong).FullName))
            {
                AddMsgListener(typeof(MsgPong).FullName, OnMsgPong);
            }
        }

        // 统一注册所有协议
        private static void InitProtocols()
        {
            MsgBase.Register(MsgMove.Parser);
            MsgBase.Register(MsgAttack.Parser);
            MsgBase.Register(MsgPing.Parser);
            MsgBase.Register(MsgPong.Parser);
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);
                Debug.Log("Socket Connect success");
                FireEvent(NetEvent.ConnectSucc, "");
                isConnecting = false;
                // 开始接收数据
                socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
            }
            catch (SocketException ex)
            {
                Debug.Log($"Socket connect fail, {ex.Message}");
                FireEvent(NetEvent.ConnectFail, ex.Message);
                isConnecting = false;
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = ar.AsyncState as Socket;
                int count = socket.EndReceive(ar);
                if (count == 0)
                {
                    Close();
                    return;
                }
                readBuff.writeIdx += count;
                // 处理二进制
                OnReceiveData();
                // 继续接收数据
                if (readBuff.remain < 8)
                {
                    readBuff.MoveBytes();
                    readBuff.ReSize(readBuff.length * 2);
                }
                socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
            }
            catch (SocketException ex)
            {
                Debug.Log($"Socket Receive fail: {ex.Message}");
            }
        }

        // 处理数据
        private static void OnReceiveData()
        {
            // Debug.Log($"Received {BitConverter.ToString(readBuff.bytes, readBuff.readIdx, readBuff.length)}");
            // 消息长度
            if (readBuff.length <= 2)
            {
                return;
            }
            // 获取消息体长度
            int readIdx = readBuff.readIdx;
            byte[] bytes = readBuff.bytes;
            Int16 bodyLength = (Int16)(bytes[readIdx] | bytes[readIdx + 1] << 8);
            if (bodyLength + 2 > readBuff.length)
            {
                return;
            }
            readBuff.readIdx += 2;
            // 解析协议名
            int nameCount = 0;
            string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIdx, out nameCount);
            // Debug.Log($"protoName: {protoName}");
            if (protoName == null)
            {
                Debug.Log("OnReceiveData MsgBase.DecodeName fail");
                return;
            }
            readBuff.readIdx += nameCount;
            // 解析协议体
            int bodyCount = bodyLength - nameCount;
            IMessage msg = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIdx, bodyCount);
            readBuff.readIdx += bodyCount;
            readBuff.CheckAndMoveBytes();
            // 将接收到的内容放到消息队列中
            lock (msgList)
            {
                msgList.Add(msg);
            }
            msgCount++;
            // 继续读消息
            if (readBuff.length > 2)
            {
                OnReceiveData();
            }
        }


        // 关闭连接
        public static void Close()
        {
            // 判断状态
            if (socket == null || !socket.Connected)
            {
                return;
            }
            if (isConnecting)
            {
                return;
            }
            if (writeQueue.Count > 0)
            {
                isClosing = true;
            }
            else
            {
                socket.Close();
                // 分发连接关闭的事件
                FireEvent(NetEvent.Cloas, "");
            }
        }

        // 发送数据
        public static void Send(IMessage msg)
        {
            // 判断状态
            if (socket == null || !socket.Connected)
            {
                return;
            }
            if (isClosing || isConnecting)
            {
                return;
            }
            // 数据编码
            byte[] nameBytes = MsgBase.EncodeName(msg);
            byte[] bodyBytes = MsgBase.Encode(msg);

            int len = nameBytes.Length + bodyBytes.Length;
            byte[] sendBytes = new byte[2 + len];
            // 组装数据
            sendBytes[0] = (byte)(len % 256);
            sendBytes[1] = (byte)(len / 256);
            Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
            Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
            Debug.Log(System.BitConverter.ToString(sendBytes));

            // 写入队列
            ByteArray ba = new ByteArray(sendBytes);
            int count = 0;      // writeQueue的长度
            lock (writeQueue)
            {
                writeQueue.Enqueue(ba);
                count = writeQueue.Count;
            }
            // send
            if (count == 1)
            {
                socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, socket);
            }
        }

        public static void SendCallback(IAsyncResult ar)
        {
            // 获取state、EndSend的处理
            Socket socket = ar.AsyncState as Socket;
            // 判断状态
            if (socket == null || !socket.Connected)
            {
                return;
            }
            int count = socket.EndSend(ar);
            // 获取写入队列第一条数据
            ByteArray ba;
            lock (writeQueue)
            {
                ba = writeQueue.First();
            }

            // 完整发送
            ba.readIdx += count;
            if (ba.length == 0)
            {
                lock (writeQueue)
                {
                    writeQueue.Dequeue();
                    ba = writeQueue.Count > 0 ? writeQueue.First() : null;
                }
            }
            // 继续发送
            if (ba != null)
            {
                socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, socket);
            }
            // 正在关闭
            else if (isClosing)
            {
                socket.Close();
            }
        }


        // 发送PING协议
        private static void PingUpdate()
        {
            // 是否启用
            if (!isUsePing)
            {
                return;
            }
            // 发送PING
            if (Time.time - lastPingTime > pingInterval)
            {
                MsgPing msgPing = new MsgPing();
                Send(msgPing);
                lastPingTime = Time.time;
            }
            // 检查PONG时间
            if (Time.time - lastPongTime > pingInterval * 4)
            {
                Close();
            }
        }



        // 更新消息
        public static void MsgUpdate()
        {
            // 做初步判断
            if (msgCount == 0)
            {
                return;
            }
            // 重复处理消息
            for (int i = 0; i < MAX_MESSAGE_FIRE; i++)
            {
                // 获取第一条消息
                IMessage msgBase = null;
                lock (msgList)
                {
                    if (msgList.Count > 0)
                    {
                        msgBase = msgList[0];
                        Debug.Log($"msgBase {msgBase.ToString()}");
                        msgList.RemoveAt(0);
                        msgCount--;
                    }
                }
                // 分发消息
                if (msgBase != null)
                {
                    string name = msgBase.Descriptor.FullName;
                    FireMsg(name, msgBase);
                }
                // 没有消息了
                else
                {
                    break;
                }
            }
        }

        // 监听PONG协议，负责更新lastPongTime
        private static void OnMsgPong(IMessage msgBase)
        {
            lastPongTime = Time.time; ;
        }

        // Update
        public static void Update()
        {
            MsgUpdate();
            PingUpdate();
        }
    }
}