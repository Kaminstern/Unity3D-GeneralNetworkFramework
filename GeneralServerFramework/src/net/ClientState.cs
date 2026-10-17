using System.Net.Sockets;

namespace GeneralServerFramework
{
    class ClientState
    {
        // TCP连接所需Socket
        public Socket socket;
        // 填充BeginReceive参数的读缓冲区
        public byte[] readBuff = new byte[1024];
    }
}