using General.Protos;
using Google.Protobuf;

namespace GeneralServerFramework;

public partial class MsgHandler
{
    public static void MsgMove(ClientState c, IMessage msgBase)
    {
        MsgMove msgMove = (MsgMove)msgBase;
        Console.WriteLine(msgMove.X);
        msgMove.X++;
        NetManager.Send(c, msgMove);
    }
}