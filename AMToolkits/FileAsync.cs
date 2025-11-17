

namespace AMToolkits
{
    /// <summary>
    /// 
    /// </summary>
    public class FileItem
    {
        public string filename = "";
        private readonly Queue<string?> _contents;

        private bool _is_busy = false;
        private object _locked = new object() { };

        public bool IsBusy => _is_busy;

        public FileItem()
        {
            _contents = new Queue<string?>();
        }

#pragma warning disable CS4014
        public void AppendText(string? content)
        {
            lock (_locked)
            {
                _contents.Enqueue(content);
            }
            
            this._AppendAsync(false);
        }
#pragma warning restore CS4014


        public async System.Threading.Tasks.Task Flush()
        {
            await this._AppendAsync(true);
        }


        private async System.Threading.Tasks.Task _AppendAsync(bool force = false)
        {
            if (!force && _is_busy)
            {
                return;
            }

            try
            {
                _is_busy = true;

                string content = "";
                lock (_locked)
                {
                    content = string.Join("", _contents);
                    _contents.Clear();
                }

                if (content.Length > 0)
                {
                    await File.AppendAllTextAsync(filename, content);
                }

                _is_busy = false;

            }
            catch (Exception ex)
            {
                _is_busy = false;

                System.Console.WriteLine($"(Exception) : {ex}");
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class FileAsync
    {
        private static object _locked = new object() { };
#if WINDOWS
        private readonly static Dictionary<string, FileItem> _list = new Dictionary<string, FileItem>(System.StringComparer.OrdinalIgnoreCase);
#else
        private readonly static Dictionary<string, FileItem> _list = new Dictionary<string, FileItem>();
#endif

        private static FileItem GetFileItem(string filename)
        {
            FileItem? item = null;
            lock (_locked)
            {
                _list.TryGetValue(filename, out item);
                if (item == null)
                {
                    item = new FileItem()
                    {
                        filename = filename
                    };
                    _list.Add(filename, item);
                }
            }
            return item;
        }

        public static void AppendAllText(string filename, string? content)
        {
            var item = GetFileItem(filename);

            item.AppendText(content);
        }
    }
}