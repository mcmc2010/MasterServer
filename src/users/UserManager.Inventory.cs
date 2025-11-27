

using AMToolkits.Extensions;
using AMToolkits.Game;
using Logger;


namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NUserInventoryItem
    {
        public string iid = "";
        public int index = 0;
        public string name = "";
        public int count = 0;
        public DateTime? create_time = null;
        public DateTime? expired_time = null;
        public DateTime? remaining_time = null;
        public DateTime? using_time = null;
        public string custom_data = "";
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class UserInventoryItem
    {
        public int uid = 0;
        public string iid = "";
        public string server_uid = ""; //t_user表中的
        public int index = 0;
        public int group = 0;
        public string name = "";
        public int count = 0;
        public DateTime? create_time = null;
        public DateTime? expired_time = null;
        public DateTime? remaining_time = null;
        public DateTime? using_time = null;

        private Dictionary<string, string>? _custom_data = null;
        public int status = 0;

        /// <summary>
        /// 需要转换为可通用的，与用户或角色关联的类
        /// </summary>
        /// <returns></returns>
        public NUserInventoryItem ToNItem()
        {
            return new NUserInventoryItem()
            {
                iid = this.iid,
                index = this.index,
                name = this.name,
                count = this.count,
                create_time = this.create_time,
                expired_time = this.expired_time,
                remaining_time = this.remaining_time,
                using_time = this.using_time,
                custom_data = this.GetAttributes() ?? ""
            };
        }

        /// <summary>
        /// 初始化属性
        /// </summary>
        /// <param name="pairs"></param>
        public void InitAttributes(Dictionary<string, string> pairs)
        {
            _custom_data = new Dictionary<string, string>(pairs);
        }

        public string? GetAttributes()
        {
            if (_custom_data == null) { return null; }
            return ItemUtils.ToAttributeValues(_custom_data);
        }

        public string? GetAttributeValue(int key)
        {
            string? value = null;
            _custom_data?.TryGetValue($"{key}", out value);
            return value;
        }

        public string? GetAttributeValue(string key)
        {
            string? value = null;
            _custom_data?.TryGetValue(key.Trim(), out value);
            return value;
        }

        /// <summary>
        /// 设置属性
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetAttributeValue(string name, string value)
        {
            if (_custom_data == null)
            {
                return false;
            }
            _custom_data.Set(name, value.Trim());
            return true;
        }

        public bool IsExpired(DateTime now)
        {
            // 1. 检查绝对过期时间
            if (this.expired_time.HasValue)
            {
                return this.expired_time.Value < now;
            }
            
            // 2. 检查相对剩余时间
            if (this.remaining_time.HasValue)
            {
                return this.remaining_time.Value < now;
            }
            
            // 3. 没有过期信息 - 视为永不过期
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum InventoryItemAttributeIndex
    {
        // 物品升级，不涉及品质升级，只是增加属性
        Upgrade​​               = 0x1000, //单纯的升级，此处为升级等级
        Upgrade​​Value          = 0x1001, //如果涉及物品升级经验或其它使用
        // 物品强化，不涉及品质升级，只是强化属性
        Enhance​​               = 0x2000, //单纯的强化，此处为强化等级
        Enhance​​Value          = 0x2001, //如果涉及物品强化经验或其它使用(通常不使用)
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
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> _GetUserInventoryItems(string? user_uid, List<UserInventoryItem> items,
                                    AMToolkits.Game.ItemType type = AMToolkits.Game.ItemType.None,
                                    bool has_using_item = false)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            // 1:
            if (await DBGetUserInventoryItems(user_uid, items, (int)type) < 0)
            {
                return -1;
            }

            
            // 2: 获取使用物品
            if (has_using_item)
            {
                List<UserInventoryItem> using_items = new List<UserInventoryItem>();
                using_items.AddRange(items.Where(v => v.using_time != null).ToList());
                if (using_items.Count == 0)
                {
                    var list = items.Where(v => v.index == GameSettingsInstance.Settings.User.ItemDefaultEquipmentIndex).ToList();
                    using_items.AddRange(list);
                }

                items.Clear();
                items.AddRange(using_items);
            }

            //
            return items.Count;
        }

        public async Task<int> _GrantBeginnerKitItems(string? user_uid, string kit_id = "1000",
                        List<AMToolkits.Game.GeneralItemData>? items = null, string reason = "kits")
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            if (items == null)
            {
                items = new List<GeneralItemData>();
            }
            items.Clear();

            // 目前只有一个装备，可以写死。
            var kit_items = AMToolkits.Game.ItemUtils.ParseGeneralItem("101|1");
            if (kit_id == "1000" && kit_items != null)
            {
                items.AddRange(kit_items);
            }
            else
            {
                return 0;
            }


            // 获取用户
            var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return -1;
            }

            items = UserManager.Instance.InitGeneralItemData(items);
            if (items == null)
            {
                return 0;
            }

            // 需要对齐
            int index = 1000;
            foreach (var v in items)
            {
                v.NID = ++index;
            }

            string print = "";
            print = string.Join(";", items.Select(v => $"[{v.NID}] {v.ID} - {v.GetTemplateData<Game.TItems>()?.Name} ({v.Count})"));
            _logger?.Log($"{TAGName} (GrantBeginnerKitItem) (User:{user_uid}) {print} [{reason}]");

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFAddInventoryItems(user_uid, user.CustomID, items, reason);
            if (result == null || result.Data?.ItemList == null)
            {
                _logger?.LogError($"{TAGName} (GrantBeginnerKitItem) (User:{user_uid}) {print} Failed [{reason}]");
                return -1;
            }

            // 同步数据
            foreach (var v in result.Data.ItemList)
            {
                var item = items.FirstOrDefault(i => i.NID > 0 && i.NID == v.NID);
                if (item != null)
                {
                    item.IID = v.IID;
                    item.Count = v.Count;
                    item.NID = -1;
                }
            }

            // 增加数据库记录
            if (await _DBAddUserInventoryItems(user_uid, items) < 0)
            {
                _logger?.LogError($"{TAGName} (GrantBeginnerKitItem) (User:{user_uid}) {print} Failed  [{reason}]");
                return -1;
            }

            print = string.Join(";", items.Select(v => $"{v.IID} {v.ID} - {v.GetTemplateData<Game.TItems>()?.Name} ({v.Count})"));
            _logger?.Log($"{TAGName} (GrantBeginnerKitItem) (User:{user_uid}) {print} Success");

            // 
            return items.Count;
        }


        /// <summary>
        /// 物品增加
        /// </summary>
        public async Task<int> _AddUserInventoryItems(string? user_uid,
                        List<AMToolkits.Game.GeneralItemData>? items, string reason = "")
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            if (items == null || items.Count == 0)
            {
                return -1;
            }

            // 获取用户
            var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return -1;
            }

            // 需要对齐
            int index = 1000;
            foreach (var v in items)
            {
                v.NID = ++index;
            }

            string print = "";
            print = string.Join(";", items.Select(v => $"[{v.NID}] {v.ID} - {v.GetTemplateData<Game.TItems>()?.Name} ({v.Count})"));
            _logger?.Log($"{TAGName} (AddUserInventoryItems) (User:{user_uid}) {print} [{reason}]");

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFAddInventoryItems(user_uid, user.CustomID, items, reason);
            if (result == null || result.Data?.ItemList == null)
            {
                _logger?.LogError($"{TAGName} (AddUserInventoryItems) (User:{user_uid}) {print} Failed [{reason}]");
                return -1;
            }

            // 同步数据
            foreach (var v in result.Data.ItemList)
            {
                var item = items.FirstOrDefault(i => i.NID > 0 && i.NID == v.NID);
                if (item != null)
                {
                    item.IID = v.IID;
                    item.Count = v.Count;
                    item.NID = -1;
                }
            }

            // 增加数据库记录
            if (await _DBAddUserInventoryItems(user_uid, items) < 0)
            {
                _logger?.LogError($"{TAGName} (AddUserInventoryItems) (User:{user_uid}) {print} Failed  [{reason}]");
                return -1;
            }

            print = string.Join(";", items.Select(v => $"{v.IID} {v.ID} - {v.GetTemplateData<Game.TItems>()?.Name} ({v.Count})"));
            _logger?.Log($"{TAGName} (AddUserInventoryItems) (User:{user_uid}) {print} Success");

            // 
            return items.Count;
        }

        /// <summary>
        /// 物品更新
        /// </summary>
        public async Task _UpdateUserInventoryItems(string? user_uid, List<AMToolkits.Game.GeneralItemData>? items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return;
            }

            if (items == null)
            {
                return;
            }

            if (await _DBUpdateUserInventoryItems(user_uid, items) < 0)
            {
                _logger?.LogError($"{TAGName} (UpdateUserInventoryItems) (User:{user_uid}) Failed");
                return;
            }
        }

        /// <summary>
        /// 物品消耗
        /// </summary>
        public async Task<int> _ConsumableUserInventoryItems(string? user_uid,
                        List<UserInventoryItem>? items,
                        string reason = "")
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            // 获取用户
            var user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return -1;
            }

            var consumable_items = this.InitGeneralItemData(items);
            if (consumable_items == null || consumable_items.Count == 0)
            {
                return 0;
            }

            // 需要对齐
            int index = 1000;
            foreach (var v in consumable_items)
            {
                v.NID = ++index;
            }

            string print = "";
            print = string.Join(";", consumable_items.Select(v => $"[{v.NID}] {v.ID} {v.IID} - {v.GetTemplateData<Game.TItems>()?.Name} ({v.Count})"));
            _logger?.Log($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) {print} ");

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFConsumableInventoryItems(user_uid, user.CustomID, consumable_items, reason);
            if (result == null || result.Data?.ItemList == null)
            {
                _logger?.LogError($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) {print} Failed");
                return -1;
            }


            // 同步数据
            // 返回物品剩余数量，为0代表该物品被删除
            foreach (var v in result.Data.ItemList)
            {
                var item = consumable_items.FirstOrDefault(i => i.NID > 0 && i.NID == v.NID);
                if (item != null)
                {
                    item.Count = v.Count;
                    item.NID = -1;
                }

                var r_item = items?.FirstOrDefault(i => i.iid == v.IID);
                if (r_item != null)
                {
                    r_item.count = v.Count;
                }
            }

            // 从数据库中消耗
            if ((await _DBConsumableUserInventoryItem(user_uid, consumable_items, reason)) < 0)
            {
                _logger?.LogError($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) {print} Failed");
                return -1;
            }
            return 1;
        }

        public async Task<int> _ConsumableUserInventoryItem(string? user_uid,
                        int item_index, int item_amount,
                        List<UserInventoryItem> list,
                        List<UserInventoryItem> items,
                        string reason = "")
        {
            items.Clear();

            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            var template_item = template_data?.Get(item_index);
            if (template_item == null)
            {
                return -1;
            }

            // 物品不存在
            // 排序是否具有过期时效
            var consumable_items = list.Where(v => v.index == template_item.Id && !v.IsExpired(DateTime.Now)).ToList();
            if (consumable_items.Count == 0)
            {
                _logger?.LogWarning($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) Not found ({template_item.Id} - {template_item.Name})");
                return 0;
            }

            // 优化排序：按过期时间升序排序（最早过期的在前）
            consumable_items = consumable_items
                .OrderBy(v =>
                {
                    // 1. 优先使用 expired_time
                    if (v.expired_time.HasValue)
                    {
                        return v.expired_time.Value;
                    }
                    // 2. 
                    if (v.remaining_time.HasValue)
                    {
                        return v.remaining_time.Value;
                    }

                    return DateTime.MaxValue;
                })
                .ToList();

            int available_count = consumable_items.Sum(v =>
            {
                return v.count;
            });

            // 计算总可用数量
            if (available_count < item_amount)
            {
                _logger?.LogWarning($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) Count {available_count}, Amount {item_amount} " +
                    $"  ({template_item.Id} - {template_item.Name})");
                return 0;
            }

            // 判断是否可以消耗，逐个消耗物品
            int remaining = item_amount;
            List<UserInventoryItem> revoked = new List<UserInventoryItem>();
            List<UserInventoryItem> updated = new List<UserInventoryItem>();
            for (int i = 0; i < consumable_items.Count; i++)
            {
                if (remaining <= 0) { break; }

                int consume_amount = Math.Min(consumable_items[i].count, remaining);
                remaining -= consume_amount;

                if (consume_amount < consumable_items[i].count)
                {
                    consumable_items[i].count = remaining;
                    updated.Add(consumable_items[i]);
                }
                else
                {
                    consumable_items[i].count = 0;
                    revoked.Add(consumable_items[i]);
                }
            }

            consumable_items.Clear();
            consumable_items.AddRange(updated);
            consumable_items.AddRange(revoked);

            // 从PlayFab消耗
            int result_code = await _ConsumableUserInventoryItems(user_uid, consumable_items, reason);
            if (result_code < 0)
            {
                return -1;
            }

            items.AddRange(consumable_items);
            return result_code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="list"></param>
        /// <param name="items"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<int> _ConsumableUserInventoryItems(string? user_uid,
                            List<AMToolkits.Game.GeneralItemData> list,
                            List<UserInventoryItem> items,
                            string reason = "none")
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            //
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();

            int result_code = 0;

            // 0: 检测物品是否有非道具
            bool has_item_invalid = list.Any(item =>
            {
                // 该物品必须为道具
                if (!(item.GetTemplateData<Game.TItems>()?.Type == (int)AMToolkits.Game.ItemType.Item ||
                      item.GetTemplateData<Game.TItems>()?.Type == (int)AMToolkits.Game.ItemType.Item_1))
                {
                    return true;
                }
                return false;
            });

            if (has_item_invalid)
            {
                return -10;
            }

            List<UserInventoryItem> item_list = new List<UserInventoryItem>();
            // 获取道具物品列表
            if ((result_code = await DBGetUserInventoryItems(user_uid, list, item_list)) < 0)
            {
                _logger?.LogError($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) Failed ");
                return -1;
            }


            foreach (var item in list)
            {
                string print = $"{item.ID} - {item.GetTemplateData<Game.TItems>()?.Name} ({item.Count})";
                List<UserInventoryItem> consumable_items = new List<UserInventoryItem>();
                result_code = await _ConsumableUserInventoryItem(user_uid,
                    item.ID, item.Count, item_list, consumable_items, reason);
                if (result_code < 0)
                {
                    _logger?.LogError($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) {print} Failed ");
                    return -1;
                }

                //
                items.AddRange(consumable_items);
            }

            return result_code;

        }
        #endregion

        #region Client General

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public List<AMToolkits.Game.GeneralItemData>? InitGeneralItemData(IEnumerable<AMToolkits.Game.GeneralItemData>? items)
        {
            if (items == null)
            {
                return null;
            }

            // 目前只有类型需要关联
            var item_templates_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            foreach (var item in items)
            {
                var template_data = item_templates_data?.Get(item.ID);
                if (template_data != null)
                {
                    item.Type = template_data.Type;
                    item.InitTemplateData<Game.TItems>(template_data);
                }
            }

            return items.ToList();
        }

        /// <summary>
        ///  不包括物品属性
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public List<AMToolkits.Game.GeneralItemData>? InitGeneralItemData(IEnumerable<UserInventoryItem>? items)
        {
            if (items == null)
            {
                return null;
            }

            List<GeneralItemData>? list = new List<GeneralItemData>();
            // 目前只有类型需要关联
            var item_templates_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            foreach (var item in items)
            {
                var data = new AMToolkits.Game.GeneralItemData();
                data.IID = item.iid;
                data.ID = item.index;
                data.Count = item.count;

                list.Add(data);
            }

            list = this.InitGeneralItemData(list);

            return list;
        }


        /// <summary>
        /// 获取物品列表
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> GetUserInventoryItems(string? user_uid, List<NUserInventoryItem> items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            //
            List<UserInventoryItem> list = new List<UserInventoryItem>();
            if (await _GetUserInventoryItems(user_uid, list, ItemType.None) < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserInventoryItems) (User:{user_uid}) Failed");
                return -1;
            }

            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();

            UserInventoryItem? current = null;
            // 转换为可通用的物品类
            foreach (var v in list)
            {
                var item_template_data = template_data?.Get(v.index);
                items.Add(v.ToNItem());

                if (item_template_data?.Type == (int)AMToolkits.Game.ItemType.Equipment)
                {
                    if (v.using_time != null)
                    {
                        current = v;
                    }
                }
            }

            // 物品必要检测
            if (current == null)
            {
                current = list.FirstOrDefault(v => v.index == GameSettingsInstance.Settings.User.ItemDefaultEquipmentIndex);
                var item_template_data = template_data?.Get(current?.index ?? AMToolkits.Game.ItemConstants.ID_NONE);
                if (current == null || item_template_data == null)
                {
                    _logger?.LogError($"{TAGName} (GetUserInventoryItems) (User:{user_uid}) Not found Default Equipment");
                }
                else
                {
                    // 修复使用物品时间丢失
                    // 
                    current.using_time = DateTime.Now;
                    int index = items.FindIndex(v => v.iid == current.iid);
                    if(index >= 0)
                    {
                        items[index] = current.ToNItem();
                    }

                    await this.DBUsingUserInventoryItem(user_uid, current.iid, item_template_data, null);
                }
            }

            //
            return items.Count;
        }

        /// <summary>
        /// 消耗物品道具
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="item_index"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> ConsumableUserInventoryItem(string? user_uid, int item_index, int item_amount,
                                    List<NUserInventoryItem> items, string reason = "none")
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            //
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            var item_template_data = template_data?.Get(item_index);
            if (item_template_data == null)
            {
                return -1;
            }

            // 该物品必须为道具
            if (!(item_template_data.Type == (int)AMToolkits.Game.ItemType.Item || item_template_data.Type == (int)AMToolkits.Game.ItemType.Item_1))
            {
                return -10;
            }

            int result_code = 0;
            List<UserInventoryItem> list = new List<UserInventoryItem>();
            // 获取道具物品列表
            if ((result_code = await DBGetUserInventoryItems(user_uid, list, item_template_data.Type)) < 0)
            {
                _logger?.LogError($"{TAGName} (ConsumableUserInventoryItems) (User:{user_uid}) Failed");
                return -1;
            }

            List<UserInventoryItem> consumable_items = new List<UserInventoryItem>();
            result_code = await _ConsumableUserInventoryItem(user_uid, item_index, item_amount, list, consumable_items, reason);
            if (result_code < 0)
            {
                return -1;
            }

            //
            foreach (var item in consumable_items)
            {
                items.Add(item.ToNItem());
            }

            return result_code;

        }

        /// <summary>
        /// 使用物品目前处理了，单类必选一，比如装备只能选择其中一个
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="item_iid"></param>
        /// <param name="item_index"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> UsingUserInventoryItem(string? user_uid, string item_iid, int item_index,
                                    List<NUserInventoryItem> items)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            //
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            var template_item = template_data?.Get(item_index);
            if (template_item == null)
            {
                return -1;
            }

            // 该物品不允许使用
            if (!template_item.Useable)
            {
                return -10;
            }

            // 0:
            // 查看是否有特效
            var effect_list = new List<string>();
            var effects = AMToolkits.Game.ValuesUtils.ParseValues(template_item.EffectValues);
            if (!effects.IsNullOrEmpty())
            {
                effect_list.AddRange(effects);

                // 检测特效是否存在
                if (await GameEffectsManager.Instance._CheckUserEffects(user_uid, effect_list) < 0)
                {
                    return -100;
                }
            }

            int result_code = 0;
            List<UserInventoryItem> list = new List<UserInventoryItem>();
            if (template_item.Type == (int)AMToolkits.Game.ItemType.Equipment)
            {
                if ((result_code = await DBUsingUserInventoryItem(user_uid, item_iid, template_item, list)) < 0)
                {
                    _logger?.LogError($"{TAGName} (UsingUserInventoryItems) (User:{user_uid}) Failed");
                    return -1;
                }
            }
            // 物品
            else if (template_item.Type == (int)AMToolkits.Game.ItemType.Item || template_item.Type == (int)AMToolkits.Game.ItemType.Item_1)
            {
                if ((result_code = await DBUsingUserInventoryItem(user_uid, item_iid, template_item, list, 1)) < 0)
                {
                    _logger?.LogError($"{TAGName} (UsingUserInventoryItems) (User:{user_uid}) Failed");
                    return -1;
                }

                // 更新成功，同步物品
                var using_item = list.FirstOrDefault();
                if (result_code == 7 && using_item != null)
                {
                    // 如果是使用次数的物品，不消耗。而是等游戏中扣除
                    if (template_item.SubType == (int)AMToolkits.Game.ItemSubType.RemainingUses)
                    {

                    }
                    else
                    {
                        // 一般物品每次都消耗1
                        using_item.count = 1;
                        List<UserInventoryItem> consumable_items = new List<UserInventoryItem>();
                        consumable_items.Add(using_item);
                        if (await this._ConsumableUserInventoryItems(user_uid, consumable_items, "using") < 0)
                        {
                            _logger?.LogError($"{TAGName} (UsingUserInventoryItems) (User:{user_uid}) " +
                                $" Consumable Item ({using_item.iid}:{using_item.index}) {using_item.name} x{using_item.count} Failed");
                        }


                    }
                }
            }

            // 物品不存在
            if (result_code == 0)
            {
                return 0;
            }

            // 转换为可通用的物品类
            foreach (var v in list)
            {
                items.Add(v.ToNItem());
            }

            // 物品已经在使用
            if (result_code == 1)
            {
                return -100;
            }

            if (effect_list.Count > 0)
            {
                if (await GameEffectsManager.Instance._AddUserEffects(user_uid, effect_list, template_item.Remaining,
                                list) < 0)
                {
                    _logger?.LogWarning($"{TAGName} (UsingUserInventoryItem) (User:{user_uid}) {item_iid} - {item_index} {template_item.Name} " +
                                        $" Add Effect:${AMToolkits.Game.ValuesUtils.ToValues(effects)} Failed");
                }
            }


            return result_code;

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="prerequisite"></param>
        /// <returns></returns>
        public (int, int) _GetUserInventoryUpgradeItems(UserInventoryItem item,
                    List<AMToolkits.Game.GeneralItemData> prerequisite)
        {
            var items_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItemEquipment>();
            if (template_data == null || items_template_data == null)
            {
                return (-1, -1);
            }

            var list = template_data.All(v => v.ItemId == item.index)?.ToList()
                                .OrderBy(v => v.Level)
                                .ToList();
            if (list == null || list.Count == 0)
            {
                return (0, -1);
            }

            int attribute_value = 0;
            var value = item.GetAttributeValue($"{(int)InventoryItemAttributeIndex.Upgrade}");
            if (!value.IsNullOrWhiteSpace())
            {
                if (!int.TryParse(value, out attribute_value))
                {
                    attribute_value = -1;
                }
            }
            if (attribute_value < 0)
            {
                _logger?.LogError($"{TAGName} (GetUserInventoryUpgradeItems) {item.iid} {item.index} - {item.name} Attribute:{value} Error");
                return (-1, -1);
            }

            int index = 0;
            if (attribute_value > 0)
            {
                index = list.FindIndex(v => v.Id == attribute_value);
                index = index + 1; //当前属性的下一级
            }

            // 已经满级了
            if(index >= list.Count)
            {
                return (-100, list[list.Count-1].Id);
            }

            var upgrade_items = AMToolkits.Game.ItemUtils.ParseGeneralItem(list[index].UpgradeCost);
            if (upgrade_items == null)
            {
                return (0, list[index].Id);
            }

            // 需要初始化模版数据
            foreach (var v in upgrade_items)
            {
                var template_item = items_template_data.Get(v.ID);
                v.InitTemplateData<Game.TItems>(template_item);
            }
            
            prerequisite.AddRange(upgrade_items);
            return (prerequisite.Count, list[index].Id);
        }

        
        /// <summary>
        /// 升级物品
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="item_iid"></param>
        /// <param name="item_index"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<int> UpgradeUserInventoryItem(string? user_uid, string item_iid, int item_index,
                                    List<NUserInventoryItem> items,
                                    List<NUserInventoryItem> consumed,
                                    Dictionary<string, object?> attach_data)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return -1;
            }
            user_uid = user_uid.Trim();

            //
            var template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItems>();
            var item_template_data = template_data?.Get(item_index);
            if (item_template_data == null)
            {
                return -1;
            }

            // 该物品不允许升级
            // 限时物品，过期物品不可以升级
            if (item_template_data.Type != (int)AMToolkits.Game.ItemType.Equipment ||
                item_template_data.Remaining > 0 || item_template_data.Expired > 0)
            {
                return -10;
            }

            int result_code = 0;
            List<UserInventoryItem> list = new List<UserInventoryItem>();
            if ((result_code = await DBGetUserInventoryItem(user_uid, item_iid, item_template_data, list)) < 0)
            {
                _logger?.LogError($"{TAGName} (UpgradeUserInventoryItems) (User:{user_uid}) {item_template_data.Id} - {item_template_data.Name} Failed");
                return -1;
            }

            // 1. 物品不存在
            var item = list.FirstOrDefault();
            if (result_code == 0 || list.Count == 0 || item == null)
            {
                return 0;
            }

            // 2. 
            int upgrade_index = -1;
            List<AMToolkits.Game.GeneralItemData> prerequisite_items = new List<GeneralItemData>();
            (result_code, upgrade_index) = _GetUserInventoryUpgradeItems(item, prerequisite_items);
            if (result_code <= 0 || upgrade_index < 0)
            {
                return result_code;
            }

            var vc_list = prerequisite_items.Where(v => AMToolkits.Game.ItemUtils.HasVirtualCurrency(v.ID)).ToList();
            if (vc_list.Count > 0)
            {
                prerequisite_items.RemoveAll(v => vc_list.Contains(v));

                //
                bool is_all_vc = true;
                foreach (var vc in vc_list)
                {
                    Dictionary<string, object?>? result_vc = null;
                    if (vc.ID == AMToolkits.Game.ItemConstants.ID_GD && vc.Count > 0 &&
                        (result_vc = await UserManager.Instance._CheckVirtualCurrency(user_uid, vc.Count, VirtualCurrency.GD)) != null)
                    {
                    }
                    else if (vc.ID == AMToolkits.Game.ItemConstants.ID_GM && vc.Count > 0 &&
                        (result_vc = await UserManager.Instance._CheckVirtualCurrency(user_uid, vc.Count, VirtualCurrency.GM)) != null)
                    {
                    }
                    else
                    {
                        is_all_vc = false;
                    }

                }

                if (!is_all_vc)
                {
                    return -101; // 条件不满足
                }
            }

            // 获取背包物品
            List<UserInventoryItem> inventory_list = new List<UserInventoryItem>();
            if (prerequisite_items.Count > 0)
            {
                if ((result_code = await DBGetUserInventoryItems(user_uid, prerequisite_items, inventory_list)) < 0)
                {
                    _logger?.LogError($"{TAGName} (UpgradeUserInventoryItems) (User:{user_uid}) {item_template_data.Id} - {item_template_data.Name} Failed");
                    return -1;
                }

                // 
                bool is_all_items = true;
                foreach (var v in prerequisite_items)
                {
                    var vlist = inventory_list.Where(vi => vi.index == v.ID).ToList();
                    int count = vlist.Sum(vi => vi.count);
                    if (count < v.Count) // 只要有一个物品不满足就直接返回条件不足
                    {
                        is_all_items = false;
                        break;
                    }
                }

                if (!is_all_items)
                {
                    return -101; // 条件不满足
                }
            }

            // 消耗物品
            List<UserInventoryItem> consumable_items = new List<UserInventoryItem>();
            if (prerequisite_items.Count > 0)
            {
                // 
                if ((result_code = await this._ConsumableUserInventoryItems(user_uid, prerequisite_items, consumable_items, "upgrade")) < 0)
                {
                    _logger?.LogError($"{TAGName} (UpgradeUserInventoryItems) (User:{user_uid}) {item_template_data.Id} - {item_template_data.Name} Failed");
                    return -1;
                }


                // 没有物品可消耗
                if (result_code == 0)
                {
                    _logger?.LogError($"{TAGName} (UpgradeUserInventoryItems) (User:{user_uid}) {item_template_data.Id} - {item_template_data.Name} Failed");
                    return 0;
                }

            }

            // 消耗货币
            if(vc_list.Count > 0)
            {
                vc_list.ForEach(v => v.Count = -v.Count); //需要扣钱
                Dictionary<string, object?> result_vc = new Dictionary<string, object?>();
                if ((result_code = await this._UpdateVirtualCurrency(user_uid, vc_list, result_vc, "upgrade")) < 0)
                {
                    _logger?.LogError($"{TAGName} (UpgradeUserInventoryItems) (User:{user_uid}) {item_template_data.Id} - {item_template_data.Name} VirtualCurrency Failed");
                    return -1;
                }

                // 没有物品可消耗
                if (result_code == 0)
                {
                    _logger?.LogError($"{TAGName} (UpgradeUserInventoryItems) (User:{user_uid}) {item_template_data.Id} - {item_template_data.Name} VirtualCurrency Failed");
                    return 0;
                }

                attach_data.AddRange(result_vc);
            }

            // 需要根据配置表里物品升级属性对应索引来写这个值
            // Fixed bugs：这里是数值，不是文本
            item.SetAttributeValue($"{(int)InventoryItemAttributeIndex.Upgrade}", $"{upgrade_index}");

            //
            result_code = await DBUpdateUserInventoryItemCustomData(user_uid, list);
            if ((result_code = await DBGetUserInventoryItem(user_uid, item_iid, item_template_data, list)) < 0)
            {
                _logger?.LogError($"{TAGName} (UpgradeUserInventoryItems) (User:{user_uid}) {item_template_data.Id} - {item_template_data.Name} Failed");
                return -1;
            }

            // 装备
            foreach (var v in list)
            {
                item_template_data = template_data?.Get(v.index);
                items.Add(v.ToNItem());
            }

            // 消耗道具
            // 转换为可通用的物品类
            foreach (var v in consumable_items)
            {
                item_template_data = template_data?.Get(v.index);
                consumed.Add(v.ToNItem());
            }

            

            return result_code;

        }

        #endregion
    }
}