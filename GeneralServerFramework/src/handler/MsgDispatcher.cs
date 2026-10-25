using Google.Protobuf;

namespace GeneralServerFramework;

public static class MsgDispatcher
{
    // 协议名 → 处理函数
    private static readonly Dictionary<string, Action<ClientState, IMessage>> handlers = new();

    public static void Register(string protoName, Action<ClientState, IMessage> handler)
    {
        handlers[protoName] = handler;
    }

    public static void Dispatch(ClientState state, IMessage msg)
    {
        string name = msg.Descriptor.FullName;
        if(handlers.TryGetValue(name, out var handler))
        {
            handler(state, msg);
        }
        else
        {
            Console.WriteLine($"未注册的消息处理：{name}");
        }
    }
}