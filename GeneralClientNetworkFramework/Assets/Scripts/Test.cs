using Google.Protobuf;
using General.Protos;
using UnityEngine;

namespace GeneralClientFramework
{
    public class Test : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            NetManager.AddEventListener(NetEvent.ConnectSucc, OnConnectSucc);
            NetManager.AddEventListener(NetEvent.ConnectFail, OnConnectFail);
            NetManager.AddEventListener(NetEvent.Cloas, OnConnectClose);
            NetManager.AddMsgListener(typeof(MsgMove).FullName, OnMsgMove);

        }

        // Update is called once per frame
        void Update()
        {
            NetManager.Update();
        }

        // 玩家点击连接按钮
        public void OnConnectClick()
        {
            NetManager.Connect("127.0.0.1", 8888);
            // TODO:开始转圈，提示“连接中”
        }

        // 主动关闭
        public void OnCloseClick()
        {
            NetManager.Close();
        }

        // 测试msgmove
        public void OnMoveClick()
        {
            MsgMove msgMove = new MsgMove();
            msgMove.X = 120;
            msgMove.Y = 10;
            msgMove.Z = -5;
            NetManager.Send(msgMove);
        }

        // 连接成功回调
        void OnConnectSucc(string err)
        {
            Debug.Log("OnConnectSucc");
        }

        // 连接失败回调
        void OnConnectFail(string err)
        {
            Debug.Log("OnConnectFail");
        }

        // 关闭连接
        void OnConnectClose(string err)
        {
            Debug.Log("OnConnectClose");
        }

        // 接收OnMsgMove协议
        public void OnMsgMove(IMessage msgBase)
        {
            MsgMove msg = msgBase as MsgMove;
            // 消息处理
            Debug.Log($"OnMsgMove msg.x = {msg.X}");
            Debug.Log($"OnMsgMove msg.y = {msg.Y}");
            Debug.Log($"OnMsgMove msg.z = {msg.Z}");
        }
    }
}