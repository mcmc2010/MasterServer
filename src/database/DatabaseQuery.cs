using System.Data.SqlTypes;
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
        private MySqlTransaction? _transaction = null;
        internal Logger.LoggerEntry? logger = null;

        private int _affected = -1;
        private long _last_id = -1;

        private DatabaseResultItemSet _result = new DatabaseResultItemSet();
        public DatabaseResultItemSet ResultItems {
            get { return _result; }
        }

        public void Release()
        {
            _result.Clear();
            
            if(_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }

            if(db != null)
            {
                db.Close();
                db.Dispose();
                db = null;
            }

        }


        public int Transaction()
        {
            if(db == null) {
                return -1;
            }

            if(_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            _transaction = db.BeginTransaction();
            return 1;
        }

        public void Commit()
        {
            if(_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            if(_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// 执行一个单一返回值
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int Query(string sql, params object?[] args)
        {
            if(db == null) {
                return -1;
            }

            _affected = -1;
            _result.Clear();

            // 移除SQL语句中的注释
            var regex = new System.Text.RegularExpressions.Regex(@"\s*(--|#).*?$", RegexOptions.Compiled);
            sql = regex.Replace(sql, "");
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
                this.ParseResultData(reader, _result);

                //
                reader.Close();
            }
            _last_id = command.LastInsertedId;
            command.Dispose();
            return _result.Count;
        }

        /// <summary>
        /// 返回一个列表,该列表不会缓存，执行完成后结果将丢弃
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public int QueryWithList(string sql, out List<DatabaseResultItemSet>? list, params object[] args)
        {
            list = null;

            if(db == null) {
                return -1;
            }

            list = new List<DatabaseResultItemSet>();

            _affected = -1;
            _result.Clear();

            // 移除SQL语句中的注释
            var regex = new System.Text.RegularExpressions.Regex(@"\s*(--|#).*?$", RegexOptions.Compiled);
            sql = regex.Replace(sql, "");
            PrintSQL(sql);

            MySqlCommand command = new MySqlCommand(sql, db);
            // 按顺序添加参数（与 SQL 中的 ? 顺序对应）
            for (int i = 0; i < args.Length; i++)
            {
                command.Parameters.Add(new MySqlParameter { Value = args[i] ?? DBNull.Value });
            }
            
            var reader = command.ExecuteReader();
            _affected = reader.RecordsAffected;

            // 读取列表
            DatabaseResultItemSet result;
            while(this.ParseResultData(reader, result = new DatabaseResultItemSet()) > 0)
            {
                list.Add(result);
            }

            //
            reader.Close();

            //
            _last_id = command.LastInsertedId;
            command.Dispose();
            return list.Count;
        }

        private int ParseResultData(MySqlDataReader reader, DatabaseResultItemSet result)
        {
            result.Clear();
            if(!reader.Read()) {
                return 0;
            }
            
            //
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);
                object value = reader.GetValue(i);
                result.Add(name, new DatabaseResultItem() {
                    name = name,
                    value = value is DBNull ? null : value
                });
            }

            //
            return result.Count;
        }

        public void PrintSQL(string sql)
        {
            sql = Regex.Replace(sql, @"[\r\n]+", " ");
            logger?.Log($"[DatabaseManager] (Query) : " + sql);
        }
    }
}