using System.Text.Json.Serialization;
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

        public string Items = "";

        public UserGender Gender = UserGender.Female;
        public string Region  = "";
    }

    [System.Serializable]
    public class AIPlayerData
    {
        /// <summary>
        /// 为实例ID
        /// </summary>
        public string ID = "";
        /// <summary>
        /// 为模版ID
        /// </summary>
        public int TID;
        public int Type;
        public int Level;
        public string Name = "";
        [JsonPropertyName("hol_value")]
        public int HOLValue = 100;
        public string Items = "";
        /// <summary>
        /// 
        /// </summary>
        public int status = 0;

        private AIPlayerTemplateData? _template_data = null;
        public AIPlayerTemplateData? TemplateData {
            get { return _template_data; }
        }

        public bool InitTemplateData(AIPlayerTemplateData template_data)
        {
            if(this.TID != template_data.ID)
            {
                return false;
            }

            this._template_data = template_data;

            this.Type = template_data.Type;

            return true;
        }
    }

    public class AIPlayerManager : SingletonT<AIPlayerManager>, ISingleton
    {
        [AutoInitInstance]
        protected static AIPlayerManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        private List<AIPlayerTemplateData> _template_data_list = new List<AIPlayerTemplateData>();
        // 将 Random 实例提升为类成员变量，避免重复创建
        private readonly System.Random _rand = new System.Random();

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
        /// ID8N
        /// </summary>
        /// <returns></returns>
        public string GeneratorID8N()
        {
            var now = DateTime.UtcNow;
            int NA = now.Year % 10;
            int NB = now.Month % 10;
            int NR = _rand.Next(1000, 9999);
            int NC = now.Second % 10;
            string iid = $"1{NA}{NB}{NR}{NC}";
            return iid;
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
                    Name = v.Name,
                    Items = string.Join(";", v.Items)
                };

                template.Gender = v.Gender.Trim().ToLower() == "female" ? UserGender.Female : UserGender.Male;
                template.Region = v.Region.Trim();

                _template_data_list.Add(template);
            }

            _logger?.Log($"{TAGName} Loaded TemplateData : {_template_data_list.Count}");
        }

        public AIPlayerTemplateData? GetTemplateData(int tid)
        {
            return _template_data_list.FirstOrDefault(v => v.ID == tid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void InitPlayerData(AIPlayerData data)
        {
            var template_data = this.GetTemplateData(data.TID);
            if(template_data == null)
            {
                _logger?.LogError($"{TAGName} (InitPlayerData) Error : Not Template (ID:{data.TID}) Data");
                return;
            }

            //
            data.InitTemplateData(template_data);
        }

        public AIPlayerData CreatePlayerData(AIPlayerTemplateData template_data)
        {
            var player_data = new AIPlayerData() {
                TID = template_data.ID,
                Name = template_data.Name,
                Level= template_data.Level
            };
            player_data.Items = template_data.Items;

            //
            player_data.ID = this.GeneratorID8N();
            //
            player_data.InitTemplateData(template_data);
            return player_data;
        }

        public AIPlayerTemplateData? Rand(List<AIPlayerData> without, int level = -1)
        {
            // 已经存在的
            var ids = without.Select(v => v.TID).ToHashSet();
            return this.Rand(ids, level);
        }

        public AIPlayerTemplateData? Rand(List<AIPlayerTemplateData> without, int level = -1)
        {
            // 已经存在的
            var ids = without.Select(v => v.ID).ToHashSet();
            return this.Rand(ids, level);
        }

        private AIPlayerTemplateData? Rand(HashSet<int> without, int level)
        {
            // 获取有效的模版
            var templates = _template_data_list.Where(v => {
                return !without.Contains(v.ID) 
                    && (level < 0 || (level >= 0 && level == v.Level));
                }).ToList();
            if(templates.Count == 0) {
                return null;
            }

            //
            int index = _rand.Next(0, templates.Count);
            return templates[index];
        }
    }
}