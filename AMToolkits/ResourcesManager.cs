


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logger;


namespace AMToolkits.Utility
{ 
    public enum AssetType
    {
        None,
        Text,
    }
    public interface IAsset
    {
        public AssetType type { get; }
    }

    [System.Serializable]
    public class TextAsset : IAsset
    {
        public AssetType type { get { return AssetType.Text; } }
        public System.Text.Encoding encoding = System.Text.Encoding.UTF8;
        public string text = "";
    }

    [System.Serializable]
    public class ResourcesItem
    {
        public string key = "";
        public IAsset? value;
    }

    /// <summary>
    /// 
    /// </summary>
    public class ResourcesManager : AMToolkits.SingletonT<ResourcesManager>, AMToolkits.ISingleton
    {
        public static string ROOT_PATH = "./data";
#pragma warning disable CS0649
        [AutoInitInstance]
        private static ResourcesManager? _instance;
#pragma warning restore CS0649

        private string[]? _arguments = null;
        private Logger.LoggerEntry? _logger = null;

        private List<ResourcesItem> _res_list = new List<ResourcesItem>();

        /// <summary>
        /// Initialize 
        /// </summary>
        /// <summary>
        /// Not call parent method
        /// </summary>
        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = CommandLineArgs.FirstParser(paramters);
            _logger = Logger.LoggerFactory.Instance;

            //
            _res_list.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T? Get<T>(string key) where T : IAsset
        {
            var item = this._res_list.FirstOrDefault(v => v.key == key.Trim());
            if (item == null)
            {
                return default(T);
            }
            return (T?)item.value;
        }

        private bool Add<T>(string key, T o) where T : IAsset
        {
            var item = this._res_list.FirstOrDefault(v => v.key == key.Trim());
            if (item != null)
            {
                return false;
            }

            this._res_list.Add(new ResourcesItem() { key = key.Trim(), value = o });
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T? Load<T>(string path, bool ignore_error = false) where T : IAsset
        {
            if (_instance == null)
            {
                System.Console.WriteLine($"(Resources Loader) Load '{path}' failed, not initialize.");
                return default(T);
            }


            T? o = _instance.Get<T>(path);
            if (o != null)
            {
                return o;
            }

            o = ResourcesLoad<T>(path);
            if(o == null)
            {
                if (!ignore_error)
                {
                    _instance._logger?.LogError($"(Resources Loader) Load '{path}' failed.");
                }
                return default(T);
            }

            _instance.Add<T>(path, o);
            return o;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="ignore_error"></param>
        /// <returns></returns>
        public static async System.Threading.Tasks.Task<T?> LoadAsync<T>(string path, bool ignore_error = false) where T : IAsset
        {
            if (_instance == null)
            {
                System.Console.WriteLine($"(Resources Loader) Load '{path}' failed, not initialize.");
                return default(T);
            }

            T? o = _instance.Get<T>(path);
            if (o != null)
            {
                return o;
            }

            o = (T?)await ResourcesLoadAsync<T>(path);
            if (o == null)
            {
                if (!ignore_error)
                {
                    _instance?._logger?.LogError($"(Resources Loader) Load Async '{path}' failed.");
                }
                return default(T);
            }

            _instance?.Add(path, o);
            return o;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static T? ResourcesLoad<T>(string filename) where T : IAsset
        {
            string ext = "";
            AssetType type = AssetType.None;
            if (typeof(T) == typeof(TextAsset))
            {
                type = AssetType.Text;
                ext = System.IO.Path.GetExtension(filename);
                if (ext.Length == 0)
                {
                    ext = ".txt";
                }
                else
                {
                    ext = "";
                }
            }

            if(type == AssetType.None)
            {
                return default(T);
            }

            string pathname = System.IO.Path.GetFullPath(ROOT_PATH);
            string fullname = System.IO.Path.GetFullPath(System.IO.Path.Join(pathname, filename + ext));

            try
            {
                if(!System.IO.File.Exists(fullname))
                {
                    return default(T);
                }

                if(type == AssetType.Text) {
                    return (T)(IAsset)ResourcesLoadText(fullname, System.Text.Encoding.UTF8);
                }
                return default(T);
            } catch (Exception e) {
                _instance?._logger?.LogException("Error", e);
                return default(T);
            }
        }

        private static async Task<T?> ResourcesLoadAsync<T>(string filename) where T : IAsset
        {
            string ext = "";
            AssetType type = AssetType.None;
            if(typeof(T) == typeof(TextAsset))
            {
                type = AssetType.Text;
                ext = System.IO.Path.GetExtension(filename);
                if(ext.Length == 0) {
                    ext = ".txt";
                }
            }

            if(type == AssetType.None)
            {
                return default(T);
            }

            string pathname = System.IO.Path.GetFullPath(ROOT_PATH);
            string fullname = System.IO.Path.GetFullPath(System.IO.Path.Join(pathname, filename + ext));

            try
            {
                if(!System.IO.File.Exists(fullname))
                {
                    return default(T);
                }

                if(type == AssetType.Text) {
                    return (T)(IAsset)await ResourcesLoadTextAsync(fullname, System.Text.Encoding.UTF8);
                }
                return default(T);
            } catch (Exception e) {
                _instance?._logger?.LogException("Error", e);
                return default(T);
            }
        }

        private static TextAsset ResourcesLoadText(string filename, System.Text.Encoding encoding)
        {
            string text = System.IO.File.ReadAllText(filename, encoding);
            TextAsset asset = new TextAsset() {
                encoding = encoding,
                text = text
            };
            return asset;
        }

        private static async Task<TextAsset> ResourcesLoadTextAsync(string filename, System.Text.Encoding encoding)
        {
            string text = await System.IO.File.ReadAllTextAsync(filename, encoding);
            TextAsset asset = new TextAsset() {
                encoding = encoding,
                text = text
            };
            return asset;
        }
    }
}
