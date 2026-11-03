using System;
using MySqlConnector;

namespace GeneralServerFramework
{
    public class DbManager
    {
        public static MySqlConnection mysql;

        // 连接数据库
        public static bool Connect(string db, string ip, int port, string user, string pw)
        {
            // 创建MySqlConnection对象
            mysql = new MySqlConnection();
            var builder = new MySqlConnectionStringBuilder
            {
                Server = ip,
                Port = (uint)port,
                Database = db,
                UserID = user,        
                Password = pw
            };
            mysql.ConnectionString = builder.ConnectionString;
            // 连接
            try
            {
                mysql.Open();
                Console.WriteLine("[数据库]connect succ");
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine($"[数据库]connect fail, {e}");
                return false;
            }
        }
    }
}
