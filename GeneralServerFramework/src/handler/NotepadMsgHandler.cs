using System;
using General.Protos;
using Google.Protobuf;

namespace GeneralServerFramework;

public partial class MsgHandler
{
    // 获取记事本内容
    public static void MsgGetText(ClientState cs, IMessage msgBase)
    {
        MsgGetText msg = (MsgGetText)msgBase;
        Player player = cs.player;
        if (player == null)
        {
            return;
        }
        // 获取text
        msg.Text = player.data.text;
        player.Send(msg);
    }

    // 保存记事本内容
    public static void MsgSaveText(ClientState cs, IMessage msgBase)
    {
        MsgSaveText msg = (MsgSaveText)msgBase;
        Player player = cs.player;
        if (player == null)
        {
            return;
        }
        player.data.text = msg.Text;
        // 下线的时候统一保存
        player.Send(msg);
    }
}