using System.Text.RegularExpressions;
using AMToolkits.Utility;
using Logger;
using MySql.Data.MySqlClient; // 正确命名空间

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseResultItem
    {
        public string name = "";
        public object? value = null;

        public string String {
            get {
                return (string)(value ?? "");
            }
        }
        public long Number {
            get {
                if(value?.GetType() == typeof(System.UInt32))
                {
                    return (long)(UInt32)value;
                }
                else if(value?.GetType() == typeof(System.Int32))
                {
                    return (long)(Int32)value;
                }
                return (long)(value ?? 0);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DatabaseQuery
    {
        public string name = "";
        public string version = "";
        public MySqlConnection? db = null;
        internal Logger.LoggerEntry? logger = null;

        private int _affected = -1;
        private long _last_id = -1;

        private List<DatabaseResultItem> _result = new List<DatabaseResultItem>();

        public DatabaseResultItem? GetResultItem(string field)
        {
            if(String.IsNullOrEmpty(field.Trim()))
            {
                return null;
            }
            return _result.FirstOrDefault(v => v.name.Contains(field, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int Query(string sql, params object[] args)
        {
            if(db == null) {
                return -1;
            }

            _affected = -1;
            _result.Clear();
            PrintSQL(sql);

            MySqlCommand command = new MySqlCommand(sql, db);
            // 按顺序添加参数（与 SQL 中的 ? 顺序对应）
            for (int i = 0; i < args.Length; i++)
            {
                command.Parameters.Add(new MySqlParameter { Value = args[i] ?? DBNull.Value });
            }
            
            //_result = command.ExecuteScalar();
            if(!command.CommandText.Trim().StartsWith("SELECT", StringComparison.CurrentCultureIgnoreCase))
            {
                _affected = command.ExecuteNonQuery();
            }
            else
            {
                var reader = command.ExecuteReader();
                _affected = reader.RecordsAffected;
                
                //
                if (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string name = reader.GetName(i);
                        object value = reader.GetValue(i);
                        _result.Add( new DatabaseResultItem() {
                            name = name,
                            value = value is DBNull ? null : value
                        });
                    }
                }

                //
                reader.Close();
            }
            _last_id = command.LastInsertedId;
            command.Dispose();
            return _result.Count;
        }

        public void PrintSQL(string sql)
        {
            sql = Regex.Replace(sql, @"[\r\n]+", " ");
            logger?.Log($"[DatabaseManager] (Query) : " + sql);
        }
    }
}