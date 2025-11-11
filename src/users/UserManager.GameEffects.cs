
using AMToolkits.Extensions;
using Logger;

namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class GameEffectItem
    {
        public int uid = 0;
        public int id = -1;
        public string server_uid = ""; //t_user表中的
        public string name = "";
        public string? effect_value = null;
        public int effect_type = 0;
        public int effect_sub_type = 0;
        public int group_index = 0;

        public DateTime? create_time = null;
        public DateTime? last_time = null;
        public DateTime? end_time = null;

        private AMToolkits.Utility.ITableData? _template_data = null;

        public int season = 0;

        public int status = 0;


        public bool IsEnded
        {
            get { return end_time != null; }
        }

        public void InitTemplateData<T>(T templete_data) where T : AMToolkits.Utility.ITableData
        {
            _template_data = templete_data;
        }

        public T? GetTemplateData<T>() where T : AMToolkits.Utility.ITableData
        {
            return (T?)this._template_data;
        }


        /// <summary>
        /// 需要转换为可通用的，与用户或角色关联的类
        /// </summary>
        /// <returns></returns>
        public NGameEffectData ToNItem()
        {
            return new NGameEffectData()
            {
                //UserID = this.server_uid,
                ID = this.id,
                Name = this.name,
                EffectType = this.effect_type,
                EffectSubType = this.effect_sub_type,
                GroupIndex = this.group_index,
                CreateTime = this.create_time,
                LastTime = this.last_time,
                EndTime = this.end_time,
                EffectValue = "",
                Season = this.season,
            };
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        #region Server Internal

        public async Task<int> _GetUserGameEffects(string user_uid,
                            List<GameEffectItem> items,
                            int type = -1,
                            int group_index = -1)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            if (await DBGetGameEffects(user_uid, items, type, group_index) < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserGameEffects) (User:{user_uid}) Failed");
                return -1;
            }

            return items.Count;
        }


        /// <summary>
        /// 添加特效
        ///   - 
        /// </summary>
        public async Task<int> _AddGameEffectData(string user_uid, string custom_uid,
                            NGameEffectData? data,
                            List<GameEffectItem> list)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (custom_uid == null || custom_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (data == null)
            {
                return -1;
            }

            // 保存记录
            int result_code = 0;
            if ((result_code = await _DBAddGameEffectData(user_uid, data, list)) < 0)
            {
                return -1;
            }

            // 已经完成，或不能完成，直接返回
            if (result_code == 0)
            {
                return 0;
            }
            return 1;
        }

        public async Task<int> _UpdateGameEffectData(string user_uid, string custom_uid,
                            GameEffectItem effect,
                            List<UserInventoryItem>? list = null)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            if (custom_uid == null || custom_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            if (list == null || list.Count == 0)
            {
                return 0;
            }

            // 保存记录
            int result_code = 0;
            if ((result_code = await _DBUpdateGameEffectData(user_uid, effect, list)) < 0)
            {
                return -1;
            }

            // 已经完成，或不能完成，直接返回
            if (result_code == 0)
            {
                return 0;
            }
            return 1;
        }

        #endregion

        public async Task<int> GetUserGameEffects(string user_uid,
                            List<NGameEffectData> items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            List<GameEffectItem> list = new List<GameEffectItem>();
            if (await DBGetGameEffects(user_uid, list) < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserGameEffects) (User:{user_uid}) Failed");
                return -1;
            }

            // 转换为可通用的类
            foreach (var v in list)
            {
                items.Add(v.ToNItem());
            }

            return items.Count;
        }
    }
}