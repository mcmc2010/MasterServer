
using AMToolkits.Extensions;
using Logger;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class GameEventsManager
    {

        /// <summary>
        /// 结算
        /// </summary>
        /// <param name="user"></param>
        /// <param name="id"></param>
        /// <param name="template_item"></param>
        /// <param name="result_events"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> GameEventFinal_Result(UserBase user,
                                int id,
                                Game.TGameEvents template_item,
                                List<GameEventItem> result_events,
                                GameEventDataResult result,
                                List<AMToolkits.Game.GeneralItemData>? output_item_list = null)
        {
            var event_item = result_events.FirstOrDefault();
            // 获取道具 (是否有物品发放)
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(template_item.Items);
            if (items != null && items.Length > 0)
            {
            }
            // 其次：事件物品中包含
            else if (output_item_list != null && (items = output_item_list.ToArray()) != null && items.Length > 0)
            {

            }
            else
            {
                return 0;
            }


            //
            var item_list = UserManager.Instance.InitGeneralItemData(items);
            if (item_list == null)
            {
                _logger?.LogError($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} Add Items Failed");
                return -1;
            }

            //
            var vc_list = item_list.Where(v => AMToolkits.Game.ItemUtils.HasVirtualCurrency(v.ID)).ToList();
            if (vc_list.Count > 0)
            {
                item_list.RemoveAll(v => vc_list.Contains(v));

                List<string> vcs = new List<string>();

                foreach (var v in vc_list)
                {
                    Dictionary<string, object?>? result_vc = null;
                    string currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;
                    if (v.ID == AMToolkits.Game.ItemConstants.ID_GD && v.Count > 0)
                    {
                        result_vc = await UserManager.Instance._UpdateVirtualCurrency(user.ID, v.Count, AMToolkits.Game.VirtualCurrency.GD);
                    }
                    else if (v.ID == AMToolkits.Game.ItemConstants.ID_GM)
                    {
                        currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT;
                        result_vc = await UserManager.Instance._UpdateVirtualCurrency(user.ID, v.Count, AMToolkits.Game.VirtualCurrency.GM);
                    }

                    // 默认为int32，此处用浮点表示
                    float balance = 0.0f;
                    if (result_vc == null)
                    {
                        _logger?.LogError($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} " +
                                          $"Amount: {v.Count} {currency} Failed");
                    }
                    else
                    {
                        // 默认为int32，此处用浮点表示
                        balance = System.Convert.ToSingle(result_vc.Get("balance") ?? 0.0f);
                    }

                    vcs.Add($"{currency}:{v.Count}|{balance}");

                }
                
                // 添加数据库记录
                if (vcs.Count > 0 && await UserManager.Instance._UpdateGameEventVirtualCurrency(user.ID, user.CustomID, result_events[0], vcs) <= 0)
                {
                    _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} VirtualCurrency Failed");
                    return -1;
                }
            }

            if (item_list.Count > 0)
            {
                // 需要对齐
                int index = 1000;
                foreach (var v in item_list)
                {
                    v.NID = ++index;
                }

                string print = "";
                print = string.Join(";", items.Select(v => $"[{v.NID}] {v.ID} - {v.GetTemplateData<Game.TItems>()?.Name} ({v.Count})"));

                // 发放物品 :
                var result_code = await UserManager.Instance._AddUserInventoryItems(user.ID, item_list);
                if (result_code < 0)
                {
                    _logger?.LogError($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} Add Items: {print} Failed");
                    return -1;
                }

                result.Items = new List<AMToolkits.Game.GeneralItemData>(item_list);

                // 添加数据库记录
                if (await UserManager.Instance._UpdateGameEventItemData(user.ID, user.CustomID, result_events[0], result.Items) <= 0)
                {
                    _logger?.LogWarning($"{TAGName} (GameEventFinal) (User:{user.ID}) {id} - {template_item.Name} Add Items: {print} Failed");
                    return -1;
                }
            }
            return 1;

        }

        /// <summary>
        /// 普通
        /// </summary>
        /// <param name="template_item"></param>
        /// <param name="result">上一个调用结果</param>
        protected async System.Threading.Tasks.Task<int> GameEventFinal_Normal(UserBase user,
                                int id,
                                Game.TGameEvents template_item,
                                List<GameEventItem> result_events)
        {
            result_events.Clear();

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();
            if (template_data == null)
            {
                return -1;
            }

            List<GameEventItem> list = new List<GameEventItem>();
            int result_code = await UserManager.Instance._GetUserGameEvents(user.ID, list, (int)GameEventType.Normal, -1);
            if (result_code <= 0)
            {
                return -1;
            }

            var event_item = list.FirstOrDefault(v => v.id == template_item.Id);
            if (event_item == null)
            {
                return 0;
            }
            event_item.InitTemplateData<Game.TGameEvents>(template_item);

            bool is_completed = false;
            if (template_item.GroupType == (int)AMToolkits.Game.GameGroupType.None)
            {
                is_completed = true;
            }

            if (is_completed && event_item.completed_time == null)
            {
                event_item.completed_time = DateTime.Now;
            }

            if ((result_code = await UserManager.Instance._UpdateGameEventItem(user.ID, event_item)) < 0)
            {
                return -1;
            }

            // 返回事件和关联事件
            result_events.Add(event_item);

            return 1;

        }
        
        /// <summary>
        /// 段位
        /// </summary>
        /// <param name="template_item"></param>
        /// <param name="result">上一个调用结果</param>
        protected async System.Threading.Tasks.Task<int> GameEventFinal_GameRank(UserBase user,
                                int id,
                                Game.TGameEvents template_item,
                                List<GameEventItem> result_events)
        {
            result_events.Clear();

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();
            if (template_data == null)
            {
                return -1;
            }

            List<GameEventItem> list = new List<GameEventItem>();
            int result_code = await UserManager.Instance._GetUserGameEvents(user.ID, list, (int)GameEventType.Rank, -1);
            if (result_code <= 0)
            {
                return -1;
            }

            var event_item = list.FirstOrDefault(v => v.id == template_item.Id);
            if (event_item == null)
            {
                return 0;
            }
            event_item.InitTemplateData<Game.TGameEvents>(template_item);

            bool is_completed = false;

            UserProfileExtend profile = new UserProfileExtend();
            if (await UserManager.Instance._GetUserProfile(user.ID, profile) < 0)
            {
                return -1;
            }

            int rank_level_limit = 0;
            if(!int.TryParse(event_item.GetTemplateData<Game.TGameEvents>()?.Value ?? "0", out rank_level_limit))
            {
                rank_level_limit = 0;
            }

            //
            if (rank_level_limit > 0 &&
                (rank_level_limit <= profile.RankLevel || rank_level_limit <= profile.RankLevelBest))
            {
                is_completed = true;
            }
            
            if (is_completed && event_item.completed_time == null)
            {
                event_item.completed_time = DateTime.Now;
            }

            if ((result_code = await UserManager.Instance._UpdateGameEventItem(user.ID, event_item)) < 0)
            {
                return -1;
            }

            // 返回事件和关联事件
            result_events.Add(event_item);

            return 1;

        }


        /// <summary>
        ///  通行证
        /// </summary>
        /// <param name="template_item"></param>
        /// <param name="result">上一个调用结果</param>
        protected async System.Threading.Tasks.Task<int> GameEventFinal_GamePass(UserBase user,
                                int id,
                                Game.TGameEvents template_item,
                                List<GameEventItem> result_events,
                                List<AMToolkits.Game.GeneralItemData> output_item_list)
        {
            result_events.Clear();
            output_item_list.Clear();

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();
            if (template_data == null)
            {
                return -1;
            }

            var pass_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TBattlePass>();
            if (pass_template_data == null)
            {
                return -1;
            }

            // 0: 获取特效 是否包含通行证
            List<GameEffectItem> effect_list = new List<GameEffectItem>();
            if(await UserManager.Instance._GetUserGameEffects(user.ID, effect_list, (int)GameEffectType.Pass, -1) < 0)
            {
                return -1;
            }

            int pass_level = UserManager.Instance._GetPassLevel(effect_list.Select(v => v.id));

            List<UserGamePassData> pass_data_list = new List<UserGamePassData>();
            UserGamePassData? pass_data = null;
            if (await UserManager.Instance._GetUserGamePass(user.ID, pass_data_list) < 0 ||
                (pass_data = pass_data_list.FirstOrDefault()) == null)
            {
                return -1;
            }

            int user_pass_level = UserManager.Instance._GetUserGamePassLevel(pass_data.PassValue);
            

            // 1: 获取当前事件是否存在
            List<GameEventItem> list = new List<GameEventItem>();
            int result_code = await UserManager.Instance._GetUserGameEvents(user.ID, list, (int)GameEventType.Pass, -1);
            if (result_code <= 0)
            {
                return -1;
            }

            var event_item = list.FirstOrDefault(v => v.id == template_item.Id);
            if (event_item == null)
            {
                return 0;
            }
            event_item.InitTemplateData<Game.TGameEvents>(template_item);

            List<string> values = (event_item.value ?? "")
                            .Split(";")
                            .Concat(new string[] { "", "", "" })
                            .Take(3)
                            .ToList();

            //
            int pass_id = 0;
            int.TryParse(event_item.GetTemplateData<Game.TGameEvents>()?.Value ?? "0", out pass_id);
            Game.TBattlePass? pass_template_item = null;
            if (pass_id > 0)
            {
                pass_template_item = pass_template_data.Get(pass_id);
            }
            // 当前用户pass level等级不够
            if(pass_template_item == null || user_pass_level < pass_template_item.Level)
            {
                return -101;
            }

            bool is_completed = false;
            // 添加普通物品
            List<AMToolkits.Game.GeneralItemData> item_list = new List<AMToolkits.Game.GeneralItemData>();
            
            if (values[0].IsNullOrWhiteSpace()) {
                var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(pass_template_item?.Reward ?? "");
                if (items != null && !items.IsNullOrEmpty())
                {
                    item_list.AddRange(items);
                }
                values[0] = AMToolkits.Utils.DateTimeToString();
            }

            // 获取通行证物品
            if(pass_level >= 1 && values[1].IsNullOrWhiteSpace())
            {
                var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(pass_template_item?.RewardLv1 ?? "");
                if (items != null && !items.IsNullOrEmpty())
                {
                    item_list.AddRange(items);
                }
                values[1] = AMToolkits.Utils.DateTimeToString();
            }
            if (pass_level >= 2 && values[2].IsNullOrWhiteSpace())
            {
                var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(pass_template_item?.RewardLv2 ?? "");
                if (items != null && !items.IsNullOrEmpty())
                {
                    item_list.AddRange(items);
                }
                values[2] = AMToolkits.Utils.DateTimeToString();
            }

            // 没有物品可领取
            if (item_list.Count == 0)
            {
                return -100;
            }

            output_item_list.AddRange(item_list);

            event_item.value = string.Join(";", values);
            if (is_completed && event_item.completed_time == null)
            {
                event_item.completed_time = DateTime.Now;
            }

            if ((result_code = await UserManager.Instance._UpdateGameEventItem(user.ID, event_item)) < 0)
            {
                return -1;
            }

            // 返回事件和关联事件
            result_events.Add(event_item);

            return 1;

        }

        /// <summary>
        /// 付费
        /// </summary>
        /// <param name="user"></param>
        /// <param name="id"></param>
        /// <param name="template_item"></param>
        /// <param name="event_data"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> GameEventFinal_Payment(UserBase user,
                                int id,
                                Game.TGameEvents template_item,
                                List<GameEventItem> result_events)
        {
            result_events.Clear();

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TGameEvents>();
            if (template_data == null)
            {
                return -1;
            }

            //
            List<TransactionItem> transactions = new List<TransactionItem>();
            int result_code = await PaymentManager.Instance._GetTransactions(user.ID, transactions, "review");
            if (result_code <= 0)
            {
                return -1;
            }

            // 没有充值
            if (transactions.Count == 0)
            {
                return -100;
            }

            // 暂时不考虑货币种类
            // 充值金额不够
            double total = transactions.Sum(v => v.price);
            if (template_item.Pay > total)
            {
                return -101;
            }

            Game.TGameEvents? last_template_item = null;
            List<GameEventItem> list = new List<GameEventItem>();
            if (template_item.GroupType == (int)AMToolkits.Game.GameGroupType.Daily)
            {
                last_template_item = template_data.Get(template_item.RequireLastId);
                result_code = await UserManager.Instance._GetUserGameEvents(user.ID, list, (int)GameEventType.Payment, template_item.Group);
            }
            else
            {
                result_code = await UserManager.Instance._GetUserGameEvents(user.ID, list, (int)GameEventType.Payment, -1);
            }
            if (result_code <= 0)
            {
                return -1;
            }

            var event_item = list.FirstOrDefault(v => v.id == template_item.Id);
            if (event_item == null)
            {
                return 0;
            }
            event_item.InitTemplateData<Game.TGameEvents>(template_item);

            var last_event_item = list.FirstOrDefault(v => template_item.RequireLastId > 0 && v.id == template_item.RequireLastId);
            if (last_event_item != null && last_template_item != null)
            {
                last_event_item.InitTemplateData<Game.TGameEvents>(last_template_item);
            }

            // 是否可以完成
            bool is_completed = false;
            if (template_item.GroupType == (int)AMToolkits.Game.GameGroupType.Daily)
            {
                // 
                if (template_item.RequireLastId == 0)
                {
                    is_completed = true;
                }
                else if (last_event_item != null)
                {
                    var timespan = (event_item.create_time - last_event_item.create_time);
                    if (timespan?.TotalDays < 0 || !last_event_item.IsCompleted)
                    {
                        return -103;
                    }

                    is_completed = true;
                }
                else
                {
                    return -102;
                }
            }

            if (is_completed && event_item.completed_time == null)
            {
                event_item.completed_time = DateTime.Now;
            }

            if ((result_code = await UserManager.Instance._UpdateGameEventItem(user.ID, event_item)) < 0)
            {
                return -1;
            }

            // 返回事件和关联事件
            result_events.Add(event_item);
            if (last_event_item != null)
            {
                result_events.Add(last_event_item);
            }
            return 1;
        }
    }
}