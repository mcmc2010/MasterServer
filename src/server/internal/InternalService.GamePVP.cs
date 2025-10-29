
using Game;

namespace Server
{
    public partial class InternalService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="room_type"></param>
        /// <param name="room_level"></param>
        /// <param name="start_time"></param>
        /// <param name="end_time"></param>
        /// <param name="winner"></param>
        /// <param name="loser"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _UpdateGamePVPRecord(string id,
                        int room_type, int room_level,
                        DateTime? start_time, DateTime? end_time,
                        NGamePVPPlayerData? winner, NGamePVPPlayerData? loser)
        {
            if (winner == null || loser == null)
            {
                return -1;
            }

            if (start_time == null)
            {
                start_time = DateTime.Now;
            }

            winner.UserID = winner.UserID.Trim();
            winner.IsVictory = true;
            loser.UserID = loser.UserID.Trim();
            loser.IsVictory = false;

            // 计算耗时
            float duration = 0.0f;
            if (end_time != null)
            {
                duration = (float)((end_time - start_time)?.TotalSeconds ?? 0.0);
                if (duration < 0.0f) { duration = 0.0f; }
            }

            // 更新数据库记录
            if (await DBUpdateGamePVPRecord(id, room_type, room_level, start_time, end_time, winner, loser) < 0)
            {
                return -1;
            }

            // 更新玩家游戏数据
            if (winner.AIPlayerIndex == 0 &&
                await DBUpdateGamePVPData(id, room_type, room_level, start_time, end_time, winner) < 0)
            {
                return -1;
            }
            if (loser.AIPlayerIndex == 0 &&
                await DBUpdateGamePVPData(id, room_type, room_level, start_time, end_time, loser) < 0)
            {
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns>0.0-1.0</returns>
        protected float _CalculationExperienceRatio(List<GameEffectItem> list)
        {
            // 特效加成
            float ratio = 0.0f;
            foreach (var v in list)
            {
                double value = 0.0;
                if (double.TryParse(v.GetTemplateData<TGameEffects>()?.EffectValue ?? "0.0", out value))
                {
                    ratio += (float)value;
                }
            }

            ratio = ratio * 0.01f;
            return ratio;
        }

        protected float _CalculationExperienceRatio(List<UserInventoryItem> items)
        {
            var templates_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItemEquipment>();

            // 特效加成
            float ratio = 0.0f;
            foreach (var v in items)
            {
                double value = 0.0;
                string? attr = v.GetAttributeValue((int)InventoryItemAttributeIndex.Upgrade);
                int index = -1;
                int.TryParse(attr ?? "0", out index);

                var template_item = templates_data?.Get(index);
                if (template_item == null)
                {
                    continue;
                }

                value = template_item.ExpBuff;

                ratio += (float)value;
            }

            ratio = ratio * 0.01f;
            return ratio;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        protected float _CalculationVirtualCurrencyRatio(List<GameEffectItem> list)
        {
            // 特效加成
            float ratio = 0.0f;
            foreach (var v in list)
            {
                double value = 0.0;
                if (double.TryParse(v.GetTemplateData<TGameEffects>()?.EffectValue ?? "0.0", out value))
                {
                    ratio += (float)value;
                }
            }

            ratio = ratio * 0.01f;
            return ratio;
        }
        
        protected float _CalculationVirtualCurrencyRatio(List<UserInventoryItem> items)
        {
            var templates_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TItemEquipment>();

            // 特效加成
            float ratio = 0.0f;
            foreach (var v in items)
            {
                double value = 0.0;
                string? attr = v.GetAttributeValue((int)InventoryItemAttributeIndex.Upgrade);
                int index = -1;
                int.TryParse(attr ?? "0", out index);

                var template_item = templates_data?.Get(index);
                if (template_item == null)
                {
                    continue;
                }

                value = template_item.CoinBuff;

                ratio += (float)value;
            }

            ratio = ratio * 0.01f;
            return ratio;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="room_type"></param>
        /// <param name="room_level"></param>
        /// <param name="player_data"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _SettlementGamePVPResult(string id,
                        int room_type, int room_level,
                        NGamePVPPlayerData? player_data)
        {
            if (player_data == null || player_data.AIPlayerIndex > 0)
            {
                return 0;
            }

            var templates_data_game = AMToolkits.Utility.TableDataManager.GetTableData<Game.TNormalGame>();
            var templates_data_rank = AMToolkits.Utility.TableDataManager.GetTableData<Game.TRankGame>();
            AMToolkits.Utility.ITableData? template_item = templates_data_game?.Get(v => v.Value == $"{room_level}");
            if(room_type == 2)
            {
                template_item = templates_data_rank?.Get(v => v.Id == player_data.RankLevel);
            }
            if(template_item == null)
            {
                return 0;
            }

            
            // 0:
            List<UserInventoryItem> using_items = new List<UserInventoryItem>();
            List<GameEffectItem> list = new List<GameEffectItem>();

            // 经验加成
            if (await UserManager.Instance._GetUserGameEffects(player_data.UserID, list, (int)GameEffectType.Experience) < 0)
            {
                return -1;
            }
            if (await UserManager.Instance._GetUserInventoryItems(player_data.UserID, using_items, AMToolkits.Game.ItemType.Equipment, true) < 0)
            {
                return -1;
            }


            // 1:
            float experience = 0.0f;
            float experience_ratio = _CalculationExperienceRatio(list);
            experience_ratio = experience_ratio + _CalculationExperienceRatio(using_items);
            if (room_type == 2)
            {
                var template_item_rank = template_item as Game.TRankGame;
                if (template_item_rank == null)
                {
                    return 0;
                }
                
                //experience = template_item_rank.RankReward
            }
            else
            {
                var template_item_game = template_item as Game.TNormalGame;
                if(template_item_game == null)
                {
                    return 0;
                }
                experience = template_item_game.ExpWin;
                if (!player_data.IsVictory)
                {
                    experience = -template_item_game.ExpLose;
                }
            }

            if (player_data.IsVictory)
            {
                experience = experience + experience * experience_ratio;
            }

            UserExperienceResult experience_result = new UserExperienceResult();
            if (await UserManager.Instance._AddUserExperience(player_data.UserID, experience, experience_result) < 0)
            {
                return 0;
            }
            experience_result.ratio = experience_ratio;

            // 2:
            // 金币加成
            list.Clear();
            if (await UserManager.Instance._GetUserGameEffects(player_data.UserID, list, (int)GameEffectType.Economy) < 0)
            {
                return -1;
            }

            float virtual_amount = 0.0f;
            float virtual_currency_ratio = _CalculationVirtualCurrencyRatio(list);
            virtual_currency_ratio = virtual_currency_ratio + _CalculationVirtualCurrencyRatio(using_items);
            if (room_type == 2)
            {

            }
            else
            {
                // var item = AMToolkits.Game.ItemUtils.ParseGeneralItem(template_item.Award)?.FirstOrDefault();
                // virtual_amount = item?.Count ?? 0;
                // if (!player_data.IsVictory)
                // {
                //     //virtual_amount = -virtual_amount;
                //     virtual_amount = 0.0f;
                // }
            }

            if (player_data.IsVictory)
            {
                virtual_amount = virtual_amount + virtual_amount * virtual_currency_ratio;
            }

            
            
            return 1;
        }
    }
}