using Google.Protobuf;

namespace GeneralServerFramework;

public class Player
{
    // id
    public string id ="";
    // 指向ClientState
    public ClientState state;
    // 临时数据，如坐标
    public int x;
    public int y;
    public int z;

    // 数据库数据
    public PlayerData data;

    public Player(ClientState state)
    {
        this.state = state;
    }

    // 发送信息（找到player关联的客户端，然后给这个关联的客户端发送协议）
    public void Send(IMessage msgBase)
    {
        NetManager.Send(state, msgBase);
    }
}