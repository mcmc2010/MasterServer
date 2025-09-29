
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using Logger;



namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    public partial class GameEffectsManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="values"></param>
        /// <returns></returns>

        public async System.Threading.Tasks.Task<int> _CheckUserEffects(string user_uid, IEnumerable<string?>? values)
        {
            if (values == null || values.Count() == 0)
            {
                return 0;
            }

            var effects = AMToolkits.Game.ValuesUtils.ToGeneralValues(values);
            if (effects.IsNullOrEmpty())
            {
                return -1;
            }

            //
            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null)
            {
                //未验证
                return -2;
            }

            return await _CheckUserEffects(r_user, effects);
        }

        public async System.Threading.Tasks.Task<int> _CheckUserEffects(UserBase user, IEnumerable<string?>? values)
        {
            if (values == null || values.Count() == 0)
            {
                return 0;
            }

            var effects = AMToolkits.Game.ValuesUtils.ToGeneralValues(values);
            if (effects.IsNullOrEmpty())
            {
                return -1;
            }

            return await _CheckUserEffects(user, effects);
        }

        /// <summary>
        /// 检测需要全部特效都有效
        /// </summary>
        /// <param name="user"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _CheckUserEffects(UserBase user, AMToolkits.Game.GeneralValueData[]? values)
        {
            if (values == null || values.Count() == 0)
            {
                return 0;
            }


            // 必须
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEffects>();
            if (template_data == null)
            {
                return -1;
            }

            int result_code = 0;
            foreach (var effect in values)
            {
                var template_item = template_data.Get(effect.ID);
                if (template_item == null)
                {
                    _logger?.LogWarning($"{TAGName} (CheckUserEffect) (User:{user.ID}) {effect.ID} - Not Found");
                    result_code = -1;
                    break;
                }
                if ((result_code = await _CheckUserEffectData(user, template_item, effect)) <= 0)
                {
                    break;
                }
            }

            return result_code;
        }

        protected async System.Threading.Tasks.Task<int> _CheckUserEffectData(UserBase user, Game.TGameEffects template_data, AMToolkits.Game.GeneralValueData? value)
        {
            if (value == null)
            {
                return 0;
            }

            int result_code = 0;
            List<GameEffectItem> list = new List<GameEffectItem>();
            switch (template_data.EffectType)
            {
                case (int)GameEffectType.Experience:
                    {
                        result_code = await UserManager.Instance._GetUserGameEffects(user.ID, list, (int)GameEffectType.Experience, template_data.Group);
                        var effect = list.FirstOrDefault();
                        if (effect != null)
                        {
                            return -100;
                        }
                        break;
                    }
                case (int)GameEffectType.Pass:
                    {
                        result_code = await UserManager.Instance._GetUserGameEffects(user.ID, list, (int)GameEffectType.Pass, template_data.Group);
                        var effect = list.FirstOrDefault();
                        if (effect != null)
                        {
                            return -100;
                        }
                        break;
                    }

            }

            return result_code;
        }



        /// <summary>
        /// 内部调用
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="id"></param>
        /// <param name="list">关联物品</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _AddUserEffects(string user_uid, IEnumerable<string?>? values,
                                int remaining_time = -1,
                                List<UserInventoryItem>? list = null)
        {
            if (values == null || values.Count() == 0)
            {
                return 0;
            }

            var effects = AMToolkits.Game.ValuesUtils.ToGeneralValues(values);
            if (effects.IsNullOrEmpty())
            {
                return -1;
            }

            //
            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null)
            {
                //未验证
                return -2;
            }

            return await _AddUserEffects(r_user, effects, remaining_time, list);
        }

        public async System.Threading.Tasks.Task<int> _AddUserEffects(UserBase user, IEnumerable<string?>? values,
                                int remaining_time = -1,
                                List<UserInventoryItem>? list = null)
        {
            if (values == null || values.Count() == 0)
            {
                return 0;
            }

            var effects = AMToolkits.Game.ValuesUtils.ToGeneralValues(values);
            if (effects.IsNullOrEmpty())
            {
                return -1;
            }

            return await _AddUserEffects(user, effects, remaining_time, list);
        }

        public async System.Threading.Tasks.Task<int> _AddUserEffects(UserBase user, AMToolkits.Game.GeneralValueData[]? values,
                                int remaining_time = -1,
                                List<UserInventoryItem>? list = null)
        {
            if (values == null || values.Count() == 0)
            {
                return 0;
            }


            // 必须
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEffects>();
            if (template_data == null)
            {
                return -1;
            }

            int result_code = 0;
            foreach (var effect in values)
            {
                var template_item = template_data.Get(effect.ID);
                if (template_item == null)
                {
                    _logger?.LogWarning($"{TAGName} (AddUserEffect) (User:{user.ID}) {effect.ID} - Not Found");
                    continue;
                }
                List<GameEffectItem> items = new List<GameEffectItem>();
                if ((result_code = await _AddUserEffectData(user, template_item, effect, remaining_time, items)) < 0)
                {
                    break;
                }
                var effect_item = items.FirstOrDefault();
                if (effect_item != null)
                {
                    if (list != null && (result_code = await _UpdateUserEffectItemData(user, effect_item, list)) < 0)
                    {
                        break;
                    }
                }
            }

            return result_code;
        }

        protected async System.Threading.Tasks.Task<int> _AddUserEffectData(UserBase user, Game.TGameEffects template_data,
                                AMToolkits.Game.GeneralValueData? value,
                                int remaining_time = -1,
                                List<GameEffectItem>? items = null)
        {
            if (value == null)
            {
                return 0;
            }

            var effect_data = new NGameEffectData()
            {
                ID = value.ID,
                Name = template_data.Name,
                UserID = user.ID,
                EffectType = template_data.EffectType,
                EffectValue = template_data.EffectValue,
            };


            // 设置剩余时长
            if (remaining_time > 0)
            {
                effect_data.EndTime = DateTime.Now.AddSeconds(remaining_time);
            }

            int result_code = 0;
            List<GameEffectItem> list = new List<GameEffectItem>();
            switch (template_data.EffectType)
            {
                // 经验
                case (int)GameEffectType.Experience:
                    {
                        result_code = await UserManager.Instance._GetUserGameEffects(user.ID, list, (int)GameEffectType.Experience, template_data.Group);
                        break;
                    }
                // 经济
                case (int)GameEffectType.Economy:
                    {
                        result_code = await UserManager.Instance._GetUserGameEffects(user.ID, list, (int)GameEffectType.Economy, template_data.Group);
                        break;
                    }
                // Pass
                case (int)GameEffectType.Pass:
                    {
                        result_code = await UserManager.Instance._GetUserGameEffects(user.ID, list, (int)GameEffectType.Pass, template_data.Group);
                        break;
                    }

            }

            if (result_code < 0)
            {
                return -1;
            }

            // 已经有该特效，不能再添加其它同类的
            if (template_data.EffectType == (int)GameEffectType.Pass)
            {
                var effect = list.FirstOrDefault();
                if (effect != null)
                {
                    return -100;
                }
            }


            // 添加数据库记录
            if ((result_code = await UserManager.Instance._AddGameEffectData(user.ID, user.CustomID, effect_data, list)) < 0)
            {
                _logger?.LogWarning($"{TAGName} (AddUserEffectData) (User:{user.ID}) {value.ID} - {template_data.Name} Failed");
                return -1;
            }

            items?.AddRange(list);
            return 1;
        }

        protected async System.Threading.Tasks.Task<int> _UpdateUserEffectItemData(UserBase user,
                                GameEffectItem effect,
                                List<UserInventoryItem>? list = null)
        {
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            // 更新数据库记录
            int result_code = 0;
            if ((result_code = await UserManager.Instance._UpdateGameEffectData(user.ID, user.CustomID, effect, list)) < 0)
            {
                _logger?.LogWarning($"{TAGName} (UpdateUserEffectItemData) (User:{user.ID}) {effect.id} - {effect.name} Failed");
                return -1;
            }
            return 1;
        }
    }
}