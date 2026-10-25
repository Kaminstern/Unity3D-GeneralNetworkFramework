using System.Net.Sockets;

namespace GeneralServerFramework
{

    // 事件
    public enum NetEvent
    {
        ConnectSucc = 1,
        ConnectFail = 2,
        Cloas = 3
    }

    // 客户端消息，每一个客户端连接会对应一个ClientState
    public class ClientState
    {
        // TCP连接所需Socket
        public Socket socket;
        // 定义缓冲区
        public ByteArray readBuff = new ByteArray();
        // 写入队列
        public Queue<ByteArray> writeQueue = new Queue<ByteArray>();

        // 是否正在连接
        public bool isConnecting = false;
        // 是否正在关闭
        public bool isClosing = false;

        // 是否启用心跳
        public bool isUsePing = true;
        // 心跳间隔
        public int pingInterval = 5;
        // 上一次发送PING的时间
        public float lastPingTime = Environment.TickCount64;
        // 上一次收到PONG的时间
        public float lastPongTime = 0;

        // 玩家
        public Player player;


        // 事件委托类型
        public delegate void EventListener(string err);

        // 事件监听列表
        public Dictionary<NetEvent, EventListener> eventListeners = new Dictionary<NetEvent, EventListener>();

        // 添加事件监听
        public void AddEventListener(NetEvent netEvent, EventListener listener)
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
        public void RemoveEventListener(NetEvent netEvent, EventListener listener)
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
        public void FireEvent(NetEvent netEvent, string err)
        {
            if (eventListeners.ContainsKey(netEvent))
            {
                eventListeners[netEvent](err);
            }
        }
    }
}