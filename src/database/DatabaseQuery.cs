using System.Text.RegularExpressions;
using AMToolkits.Utility;
using Logger;
using MySql.Data.MySqlClient; // 正确命名空间

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseQuery
    {
        public string name = "";
        public string version = "";
        public MySqlConnection? db = null;
        internal Logger.LoggerEntry? logger = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public object? Query(string sql)
        {
            if(db == null) {
                return null;
            }

            PrintSQL(sql);

            MySqlCommand command = new MySqlCommand(sql, db);
            var result = command.ExecuteScalar();
            command.Dispose();
            return result;
        }

        public void PrintSQL(string sql)
        {
            sql = Regex.Replace(sql, @"[\r\n]+", " ");
            logger?.Log($"[DatabaseManager] (Query) : " + sql);
        }
    }
}