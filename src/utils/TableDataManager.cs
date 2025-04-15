using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Logger;

namespace AMToolkits.Utility
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class TableDataNameAttribute : Attribute
    {
        public string TableName { get; private set; }
        public TableDataNameAttribute(string name)
        {
            TableName = name;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public interface ITableData
    {
        /// <summary>
        /// 
        /// </summary>
        int Id { get; }
        //string Value { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        bool ParseData(string data, string separators = ",");
    }

    [System.Serializable]
    public class TableDataSet
    {
        public string name = "";
        public int count;
        public virtual ITableData? Has(int id) { return null; }
    }

    public class TableDataSetT<T> : TableDataSet where T : ITableData
    {
        public List<T> data { get; private set; }

        public TableDataSetT()
        {
            data = new List<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override ITableData? Has(int id)
        {
            return this.Get(id);
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return data.GetEnumerator();
        }

        /// <summary>
        /// New list
        /// </summary>
        /// <returns></returns>
        public List<T> ToList()
        {
            return new List<T>(data);
        }

        internal void Add(T value)
        {
            data.Add(value);

            //
            this.count = data.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T? Get(int id)
        {
            if (data.Count == 0)
            {
                return default(T);
            }

            T? element = data.Find((v) => { return v.Id == id; });
            return element;
        }

        public T? Get(Predicate<T> match)
        {
            if (data.Count == 0)
            {
                return default(T);
            }

            T? element = data.Find(match);
            return element;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T? this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    return default(T);
                }
                return data[index];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="match"></param>
        /// <returns>Return New TableDataSet</returns>
        public TableDataSetT<T>? All(Predicate<T> match)
        {
            if (data.Count == 0)
            {
                return default(TableDataSetT<T>);
            }

            List<T> values = data.FindAll(match);
            return this.SubSet(values);
        }

        /// <summary>
        /// New sub set
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private TableDataSetT<T> SubSet(List<T> values)
        {
            TableDataSetT<T> tds = new TableDataSetT<T>();
            tds.name = this.name;
            tds.data = values;
            tds.count = values.Count;
            return tds;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TableDataManager : AMToolkits.Utility.SingletonT<TableDataManager>, AMToolkits.Utility.ISingleton
    {
        [AutoInitInstance]
        protected static TableDataManager? _instance;

        private string[]? _arguments = null;
        private Logger.LoggerEntry? _logger = null;


        public static string TABLEDATA_PATH = "DataTables";
        private List<TableDataSet> _data_sets = new List<TableDataSet>();
        private bool _is_print = true;

        /// <summary>
        /// Not call parent method
        /// </summary>
        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = CommandLineArgs.FirstParser(paramters);
            _logger = Logger.LoggerFactory.Instance;

            //
            _data_sets.Clear();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="force_load">如果表不存在是否强制加载</param>
        /// <returns></returns>
        public static TableDataSetT<T>? GetTableData<T>(bool force_load = true) where T : ITableData, new()
        {
            string name = typeof(T).Name;
            var data_set = _instance?._data_sets.FirstOrDefault(v => {
                return v.name == name || $"N{v.name}" == name || $"T{v.name}" == name;
            });
            if (data_set != null)
            {
                return data_set as TableDataSetT<T>;
            }

            if (force_load)
            {
                TableDataSetT<T>? data = LoadTableData<T>();
                return data;
            }
            return default(TableDataSetT<T>);
        }

        public static async Task<TableDataSetT<T>?> GetTableDataAsync<T>(bool force_load = true) where T : ITableData, new()
        {
            string name = typeof(T).Name;
            var data_set = _instance?._data_sets.FirstOrDefault(v => {
                return v.name == name || $"N{v.name}" == name || $"T{v.name}" == name;
            });
            if (data_set != null)
            {
                return data_set as TableDataSetT<T>;
            }
            if (force_load)
            {
                TableDataSetT<T>? data = await LoadTableDataAsync<T>();
                return data;
            }
            return default(TableDataSetT<T>);
        }

        private static async Task<TableDataSetT<T>?> LoadTableDataAsync<T>(string path = "") where T : ITableData, new()
        {
            var clazz_type = typeof(T);
            string name = clazz_type.Name;
            string custom_name = "";

            if (name.StartsWith("T") || name.StartsWith("N"))
            {
                name = name.Substring(1);
            }

            var attributes = clazz_type.GetCustomAttributes(typeof(TableDataNameAttribute), false);
            if (attributes.Length > 0)
            {
                TableDataNameAttribute attribute = (TableDataNameAttribute)attributes[0];
                custom_name = attribute.TableName.Trim();
            }

            TextAsset? asset = null;
            if (custom_name.Length > 0)
            {
                if (path.Length == 0)
                {
                    path = $"{TABLEDATA_PATH}/{custom_name}";
                }
                asset = await ResourcesManager.LoadAsync<TextAsset>(path, true);
            }

            if (asset == null)
            {
                if (path.Length == 0)
                {
                    path = $"{TABLEDATA_PATH}/{name}";
                }

                asset = await ResourcesManager.LoadAsync<TextAsset>(path);
            }

            if (asset == null)
            {
                _instance?._logger?.LogError($"Load table {name} is error.");
                return null;
            }

            TableDataSetT<T>? data_set = ParseTableData<T>(asset.text);
            if (data_set == null)
            {
                _instance?._logger?.LogError($"Load table {name} is error.");
                return null;
            }

            data_set.name = name;
            _instance?._data_sets.RemoveAll(v => v.name == data_set.name);
            _instance?._data_sets.Add(data_set);

            if (_instance?._is_print == true)
            {
                _instance._logger?.Log($"{_instance.TAGName} Load table {path} ok.");
            }
            return data_set;
        }

        private static TableDataSetT<T>? LoadTableData<T>(string path = "") where T : ITableData, new()
        {
            var clazz_type = typeof(T);
            string name = clazz_type.Name;
            string custom_name = "";

            if (name.StartsWith("T") || name.StartsWith("N"))
            {
                name = name.Substring(1);
            }

            var attributes = clazz_type.GetCustomAttributes(typeof(TableDataNameAttribute), false);
            if (attributes.Length > 0)
            {
                TableDataNameAttribute attribute = (TableDataNameAttribute)attributes[0];
                custom_name = attribute.TableName.Trim();
            }
            TextAsset? asset = null;
            if (custom_name.Length > 0)
            {
                if (path.Length == 0)
                {
                    path = $"{TABLEDATA_PATH}/{custom_name}";
                }
                asset = ResourcesManager.Load<TextAsset>(path, true);
            }

            if (asset == null)
            {
                if (path.Length == 0)
                {
                    path = $"{TABLEDATA_PATH}/{name}";
                }

                asset = ResourcesManager.Load<TextAsset>(path);
            }

            if (asset == null)
            {
                _instance?._logger?.LogError($"Load table {name} is error.");
                return null;
            }

            TableDataSetT<T>? data_set = ParseTableData<T>(asset.text);
            if (data_set == null)
            {
                _instance?._logger?.LogError($"Load table {name} is error.");
                return null;
            }

            data_set.name = name;
            _instance?._data_sets.RemoveAll(v => v.name == data_set.name);
            _instance?._data_sets.Add(data_set);

            if (_instance?._is_print == true)
            {
                _instance._logger?.Log($"{_instance.TAGName} Load table {path} ok.");
            }
            return data_set;
        }

        private static TableDataSetT<T>? ParseTableData<T>(string text) where T : ITableData, new()
        {
            TableDataSetT<T> data_set = new TableDataSetT<T>();
            string line = "";
            try
            {
                // 定义了一个字符串 text 包含了多行文本，其中包括不同操作系统所使用的换行符 (包括 \r\n 和 \n)
                string pattern = @".+?(?:\r\n?|\n|$)"; //@"\r\n|\n|\r";
                                                       // 使用正则表达式分割字符串
                int previous = 0;
                System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
                foreach (System.Text.RegularExpressions.Match v in matches)
                {
                    int current = v.Index;
                    int length = v.Length;//current - previous;
                    line = text.Substring(previous, length).Trim(' ');
                    previous = current + v.Length;

                    //Commit
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    var value = new T();
                    if (value.ParseData(line, "\t"))
                    {
                        data_set.Add(value);
                    }
                }
            }
            catch (Exception e)
            {
                _instance?._logger?.LogError($"[TableDataManager] Load table error: {e.Message}, At line: {line}");
                return null;
            }
            return data_set;
        }
    }

}
