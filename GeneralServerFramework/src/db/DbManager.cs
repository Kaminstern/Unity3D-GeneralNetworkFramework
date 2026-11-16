using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace GeneralServerFramework
{
    public class DbManager
    {
        public static MySqlConnection mysql;

        // 直接用JsonSerializer，不用JavaScriptSerializer

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
            catch (Exception e)
            {
                Console.WriteLine($"[数据库]connect fail, {e}");
                return false;
            }
        }

        // 检查用户名密码
        public static bool CheckPassword(string id, string pw)
        {
            if (!IsSafeString(id))
            {
                Console.WriteLine("[数据库] CreatePlayer fail, id not safe");
                return false;
            }
            if (!IsSafeString(pw))
            {
                Console.WriteLine("[数据库] CreatePlayer fail, pw not safe");
                return false;
            }

            // 查询
            string sql = string.Format("select * from account where id = '{0}' and pw = '{1}';", id, pw);
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return hasRows;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[数据库] CheckPassword err, {ex.Message}");
                return false;
            }
        }

        // 注册
        public static bool Register(string id, string pw)
        {
            if (!IsSafeString(id))
            {
                Console.WriteLine($"[数据库] Register fail, id not safe");
                return false;
            }
            if (!IsSafeString(pw))
            {
                Console.WriteLine($"[数据库] Register fail, pw not safe");
                return false;
            }

            if (IsAccountExist(id))
            {
                Console.WriteLine($"[数据库] Register fail, id exist");
                return false;
            }

            // 写入数据库User表
            string sql = string.Format("insert into account set id = '{0}', pw = '{1}';", id, Md5WithSalt(pw, id));
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[数据库] Register fail {ex.Message}");
                return false;
            }
        }

        // 创建角色
        public static bool CreatePlayer(string id)
        {
            if (!IsSafeString(id))
            {
                Console.WriteLine("[数据库] CreatePlayer fail, id not safe");
                return false;
            }
            PlayerData playerData = new PlayerData();
            string data = JsonSerializer.Serialize(playerData);
            string sql = string.Format("insert into player set id = '{0}', data = '{1}';", id, data);
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[数据库] CreatePlayer err, {ex.Message}");
                return false;
            }
        }

        // 获取玩家数据
        public static PlayerData? GetPlayerData(string id)
        {
            if (!IsSafeString(id))
            {
                Console.WriteLine("[数据库] CreatePlayer fail, id not safe");
                return null;
            }
            string sql = string.Format("select * from player where id = '{0}';", id);
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                if (!dataReader.HasRows)
                {
                    dataReader.Close();
                    return null;
                }
                dataReader.Read();
                string data = dataReader.GetString("data");
                // 反序列化
                PlayerData playerData = JsonSerializer.Deserialize<PlayerData>(data)!;
                dataReader.Close();
                return playerData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[数据库] CreatePlayer err, {ex.Message}");
                return null;
            }
        }

        // 保存角色
        public static bool UpdatePlayerData(string id, PlayerData playerData)
        {
            // 序列化
            string data = JsonSerializer.Serialize(playerData);
            string sql = string.Format("update player set data = '{0}' where id = '{1}';", data, id);
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[数据库] CreatePlayer err, {ex.Message}");
                return false;
            }
        }

        // 是否存在该用户
        public static bool IsAccountExist(string id)
        {
            if (!IsSafeString(id))
            {
                return false;
            }
            // SQL语句
            string s = string.Format("select * from acoutn where id='{0}';", id);
            // 查询
            try
            {
                MySqlCommand cmd = new MySqlCommand(s, mysql);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return hasRows;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[数据库] IsSafeString err, {ex.Message}");
                return false;
            }
        }

        // 判定安全字符串
        private static bool IsSafeString(string str)
        {
            return !Regex.IsMatch(str, @"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']");
        }

        // md5
        private static string Md5WithSalt(string input, string salt)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(salt + input);
            byte[] hash = MD5.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
