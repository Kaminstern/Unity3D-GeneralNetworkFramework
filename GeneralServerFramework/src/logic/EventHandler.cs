namespace GeneralServerFramework;
public partial class EventHandler
{
    public static void OnDisconnect(ClientState cs)
    {
        Console.WriteLine("Close");
    }

    public static void OnTimer()
    {
        CheckPing();
    }

    private static void CheckPing()
    {
        long now = Environment.TickCount64;
        foreach(ClientState cs in NetManager.clients.Values)
        {
            if(now - cs.lastPingTime > NetManager.pingInterval * 1000)
            {
                Console.WriteLine($"心跳超时：{cs.socket.RemoteEndPoint}");
                NetManager.Close(cs);
            }
        }
    }
}