using General.Protos;
using Google.Protobuf;

namespace GeneralServerFramework;

public partial class MsgHandler
{
    // 处理注册协议
    public static void MsgRegister(ClientState cs, IMessage msgBase)
    {
        Console.WriteLine("MsgRegister");
        MsgRegister msg = (MsgRegister)msgBase;
        // 注册
        if (DbManager.Register(msg.Id, msg.Pw))
        {
            DbManager.CreatePlayer(msg.Id);
            msg.Result = 0;
        }
        else
        {
            msg.Result = 1;
        }
        NetManager.Send(cs, msg);
    }

    // 登录协议
    public static void MsgLogin(ClientState cs, IMessage msgBase)
    {
        MsgLogin msg = (MsgLogin)msgBase;
        if (!DbManager.CheckPassword(msg.Id, msg.Pw))
        {
            msg.Result = 1;
            NetManager.Send(cs, msg);
            return;
        }
        // 不允许再次登录
        if (cs.player != null)
        {
            msg.Result = 1;
            NetManager.Send(cs, msg);
            return;
        }
        // 如果已经登录，踢下线
        if (PlayerManager.IsOnline(msg.Id))
        {
            // 发送踢下线协议
            Player other = PlayerManager.GetPlayer(msg.Id);
            MsgKick msgKick = new MsgKick();
            msgKick.Reason = 0;
            other.Send(msgKick);
            // 断开连接
            NetManager.Close(other.state);
        }
        // 获取玩家数据
        PlayerData? playerData = DbManager.GetPlayerData(msg.Id);
        if (playerData == null)
        {
            msg.Result = 1;
            NetManager.Send(cs, msg);
            return;
        }
        // 构建Player
        Player player = new Player(cs);
        player.id = msg.Id;
        player.data = playerData;
        PlayerManager.AddPlayer(msg.Id, player);
        cs.player = player;
        // 返回协议
        msg.Result = 0;
        player.Send(msg);
    }

    // 玩家退出游戏下线
    public static void OnDisconnect(ClientState cs)
    {
        Console.WriteLine("Close");
        // player下线
        if (cs.player != null)
        {
            // 保存
            DbManager.UpdatePlayerData(cs.player.id, cs.player.data);
            // 移除
            PlayerManager.RemovePlayer(cs.player.id);
        }
    }
}