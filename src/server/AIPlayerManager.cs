using AMToolkits.Utility;
using Logger;


namespace Server
{
    [System.Serializable]
    public class AIPlayerTemplateData
    {
        public int ID;
        public int Type;
        public int Level;
        public string Name = "";
    }



    public class AIPlayerManager : SingletonT<AIPlayerManager>, ISingleton
    {
        [AutoInitInstance]
        protected static AIPlayerManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        private List<AIPlayerTemplateData> _template_data_list = new List<AIPlayerTemplateData>();

        protected override void OnInitialize(object[] paramters) 
        { 
            _arguments = CommandLineArgs.FirstParser(paramters[0]);

            var config = paramters[1] as ServerConfig;
            if(config == null)
            {
                System.Console.WriteLine("[AIPlayerManager] Config is NULL.");
                return ;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;

            //
            this.InitTemplateData();
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitTemplateData()
        {
            //
            _template_data_list.Clear();

            var table_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TAIPlayers>();
            if(table_data == null) {
                _logger?.LogWarning($"{TAGName} Not Items");
                return;
            }

            foreach(var v in table_data)
            {
                if(v.Status < 0) { continue; }

                var template = new AIPlayerTemplateData()
                {
                    ID = v.Id,
                    Level = v.Level,
                    Type = v.Type,
                    Name = v.Name
                };

                _template_data_list.Add(template);
            }

            _logger?.LogWarning($"{TAGName} Loaded TemplateData : {_template_data_list.Count}");
        }
    }
}