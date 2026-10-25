using General.Protos;
using Google.Protobuf;

namespace GeneralServerFramework;

public partial class MsgHandler
{
    public static void MsgPing(ClientState cs, IMessage msgBase)
    {
        Console.WriteLine("MsgPing");
        cs.lastPingTime = Environment.TickCount64;
        NetManager.Send(cs, new MsgPong());
    }
}