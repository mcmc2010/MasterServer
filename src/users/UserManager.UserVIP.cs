using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using Logger;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserVIPData
    {
        [JsonPropertyName("uid")]
        public string UID = "";

        [JsonPropertyName("name")]
        public string Name = "";


        /// <summary>
        /// 用户等级（不是账号等级）
        /// </summary>
        [JsonPropertyName("level")]
        public int Level = 0;
        /// <summary>
        /// 用户经验
        /// </summary>
        [JsonPropertyName("experience")]
        public int Experience = 0;

        /// <summary>
        /// 当前赛季 VIP
        /// </summary>
        [JsonPropertyName("vip_level")]
        public int VIPLevel = 0;

        /// <summary>
        /// 当前赛季 VIP值，该值是包括在VIPValueAdd内的，不需要再计算
        /// </summary>
        [JsonPropertyName("vip_value")]
        public int VIPValue = 0;

        /// <summary>
        /// 当前赛季 VIP值增加数量，默认为0
        /// 每次增加的有效值
        /// </summary>
        [JsonPropertyName("vip_value_add")]
        public int VIPValueAdd = 0;

        /// <summary>
        /// 玩家参与的赛季
        /// </summary>
        [JsonPropertyName("season")]
        public int Season = 1;

        /// <summary>
        /// 玩家参与的赛季时间
        /// </summary>
        [JsonPropertyName("season_time")]
        public DateTime? SeasonTime = null;


        public NUserVIPData ToNItem()
        {
            return new NUserVIPData()
            {
                UID = this.UID,
                Name = this.Name,
                Level = this.Level,
                Experience = this.Experience,
                VIPLevel = this.VIPLevel,
                VIPValue = this.VIPValue,
                VIPValueAdd = 0,
                Season = this.Season,
                SeasonTime = this.SeasonTime
            };
        }
    }


    [System.Serializable]
    public class NUserVIPData
    {
        [JsonPropertyName("uid")]
        public string UID = "";

        [JsonPropertyName("name")]
        public string Name = "";


        /// <summary>
        /// 用户等级（不是账号等级）
        /// </summary>
        [JsonPropertyName("level")]
        public int Level = 0;
        /// <summary>
        /// 用户经验
        /// </summary>
        [JsonPropertyName("experience")]
        public int Experience = 0;

        /// <summary>
        /// 当前赛季 VIP
        /// </summary>
        [JsonPropertyName("vip_level")]
        public int VIPLevel = 0;

        /// <summary>
        /// 当前赛季 VIP值，该值是包括在VIPValueAdd内的，不需要再计算
        /// </summary>
        [JsonPropertyName("vip_value")]
        public int VIPValue = 0;

        /// <summary>
        /// 当前赛季 VIP值增加数量，默认为0
        /// 每次增加的有效值
        /// </summary>
        [JsonPropertyName("vip_value_add")]
        public int VIPValueAdd = 0;

        /// <summary>
        /// 玩家参与的赛季
        /// </summary>
        [JsonPropertyName("season")]
        public int Season = 1;

        /// <summary>
        /// 玩家参与的赛季时间
        /// </summary>
        [JsonPropertyName("season_time")]
        public DateTime? SeasonTime = null;

    }

    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        #region Server Internal
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public async Task<int> _UpdateUserVIPData(string? user_uid, double amount, string currency,
                                List<UserVIPData>? vips = null)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            if(!GameSettingsInstance.Settings.VIP.Enabled)
            {
                _logger?.LogWarning($"{TAGName} (UserVIPData) : ({user_uid})  " +
                                $"Amount : {amount} {currency} " +
                                $"Update : Not Enabled");
                return 0;
            }

            int level = 0;
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TPlayerVip>();
            if (template_data == null)
            {
                return -1;
            }

            // 按等级排序
            var levels = template_data.ToList().OrderBy(v => v.Level).ToList();

            List<UserVIPData> list = new List<UserVIPData>();
            if (await _GetUserVIPData(user_uid, list) < 0)
            {
                return -1;
            }

            // 目前只处理VIP，不处理SVIP
            UserVIPData? data = list.FirstOrDefault();
            if (data == null)
            {
                return -1;
            }

            var experience_add = (float)amount * GameSettingsInstance.Settings.VIP.ExperienceRatio;
            var experience = data.VIPValue + experience_add;

            if (data.VIPLevel == 0)
            {
                level = 1;
            }
            
            int total = 0;
            foreach (var item in levels)
            {
                total += item.VipPoint;
                if (level == item.Level)
                {
                    if (experience >= item.VipPoint)
                    {
                        level = level + 1;
                        experience = experience - item.VipPoint;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            data.VIPLevel = level;
            data.VIPValue = (int)experience;
            data.VIPValueAdd = (int)experience_add;
            if(DBUpdateUserVIPData(user_uid, data) <= 0)
            {
                _logger?.LogError($"{TAGName} (UserVIPData) : ({user_uid})  " +
                                $"Amount : {amount} {currency} " +
                                $"Update : {level} - {(int)experience} Failed");
                return 0;
            }

            vips?.Add(data);

            _logger?.Log($"{TAGName} (UserVIPData) : ({user_uid})  " +
                                $"Amount : {amount} {currency} " +
                                $"Update : {level} - {(int)experience} Success");
            return 1;
        }

        public async Task<int> _GetUserVIPData(string user_uid,
                            List<UserVIPData> list)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();


            if (await DBGetUserVIPData(user_uid, list) < 0)
            {
                return -1;
            }

            return 1;
        }
        #endregion


        /// <summary>
        /// 获取用户VIP数据
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        protected async Task<int> GetUserVIPData(string? user_uid, List<UserVIPData> list)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            if(await DBGetUserVIPData(user_uid, list) < 0)
            {
                return -1;
            }
            return 1;
        }
    }
}