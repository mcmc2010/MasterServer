
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
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _AddUserEffects(UserBase user, IEnumerable<string?>? values)
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

            return await _AddUserEffects(user, effects);
        }

        public async System.Threading.Tasks.Task<int> _AddUserEffects(UserBase user, AMToolkits.Game.GeneralValueData[]? values)
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
                if ((result_code = await _AddUserEffectData(user, template_item, effect)) < 0)
                {
                    break;
                }
            }

            return result_code;
        }

        protected async System.Threading.Tasks.Task<int> _AddUserEffectData(UserBase user, Game.TGameEffects template_data, AMToolkits.Game.GeneralValueData? value)
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

            int result_code = 0;
            List<GameEffectItem> list = new List<GameEffectItem>();
            switch (template_data.EffectType)
            {
                case (int)GameEffectType.Pass:
                    {
                        result_code = await UserManager.Instance._GetUserGameEffects(user.ID, list, (int)GameEffectType.Pass, template_data.Group);
                        break;
                    }

            }

            if (result_code <= 0)
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
            if ((result_code = await UserManager.Instance._AddGameEffectData(user.ID, user.CustomID, effect_data)) < 0)
            {
                _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user.ID}) {value.ID} - {template_data.Name} Failed");
                return -1;
            }

            return 1;
        }
    }
}