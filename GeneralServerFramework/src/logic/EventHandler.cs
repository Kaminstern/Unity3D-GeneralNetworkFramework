namespace GeneralServerFramework;
public partial class EventHandler
{
    public static void OnDisconnect(ClientState cs)
    {
        Console.WriteLine("Close");
    }

    public static void OnTimer()
    {
        
    }
}