using System;
using System.Text.RegularExpressions;
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
            catch (Exception e)
            {
                Console.WriteLine($"[数据库]connect fail, {e}");
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
            string sql = string.Format("insert into account set id = '{0}', pw = '{1}';", id, pw);
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[数据库] Register fail {ex.Message}");
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
    }
}
