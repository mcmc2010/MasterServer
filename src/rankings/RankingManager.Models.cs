
using Logger;



namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class RankingManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected List<UserRankingItem> ToUserRankingItems(List<DatabaseResultItemSet>? rows)
        {
            List<UserRankingItem> items = new List<UserRankingItem>();
            if (rows != null)
            {
                foreach (var v in rows)
                {
                    UserRankingItem? item = v.To<UserRankingItem>();
                    if (item == null) { continue; }

                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// 每日排行榜是昨天
        /// </summary>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> DBGetRankingListWithDaily(RankingType ranking_type,
                            List<UserRankingItem> items)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                string sort = "balance";
                int limit_1 = 0;
                int limit_0 = GameSettingsInstance.Settings.Ranking.GoldLimitMin;
                if (ranking_type == RankingType.Paid)
                {
                    limit_0 = GameSettingsInstance.Settings.Ranking.GemsLimitMin;
                }
                else if (ranking_type == RankingType.Cost_0 || ranking_type == RankingType.Cost_1)
                {
                    sort = "cost";
                    limit_0 = 0;
                    limit_1 = GameSettingsInstance.Settings.Ranking.GoldLimitMin;
                    if (ranking_type == RankingType.Cost_1)
                    {
                        limit_1 = GameSettingsInstance.Settings.Ranking.GemsLimitMin;
                    }
                }
                else
                {
                    limit_0 = 0;
                    limit_1 = 0;
                    sort = "rank";
                }

                //
                List<DatabaseResultItemSet>? list = null;

                // 
                string sql =
                $"SELECT " +
                $"	  `uid`, `id`, `name`, `type`, " +
                $"    `balance`, `cost`, `currency`, `rank`, " +
                $"    `create_time`, `last_time` " +
                $"FROM `t_rankings` " +
                $"WHERE " +
                $"	  `balance` >= ? AND `cost` >= ? AND `type` = ? AND " +
                $"    `status` > 0 " +
                $"    AND YEAR(`last_time`) = YEAR(CURDATE() - INTERVAL 0 MONTH) " +
                $"    AND MONTH(`last_time`) = MONTH(CURDATE() - INTERVAL 0 MONTH) " +
                $"ORDER BY ? DESC " +
                $"LIMIT 100; ";
                var result_code = db?.QueryWithList(sql, out list,
                    limit_0, limit_1, (int)ranking_type,
                    sort);
                if (result_code < 0)
                {
                    return -1;
                }

                items = this.ToUserRankingItems(list);

                return items.Count;
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }


        /// <summary>
        /// 周榜为上周一周的排行
        /// </summary>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> DBGetRankingListWithWeekly(RankingType ranking_type,
                            List<UserRankingItem> items)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                string sort = "balance";
                int limit_1 = 0;
                int limit_0 = GameSettingsInstance.Settings.Ranking.GoldLimitMin;
                if (ranking_type == RankingType.Paid)
                {
                    limit_0 = GameSettingsInstance.Settings.Ranking.GemsLimitMinWeekly;
                }
                else if (ranking_type == RankingType.Cost_0 || ranking_type == RankingType.Cost_1)
                {
                    sort = "cost";
                    limit_0 = 0;
                    limit_1 = GameSettingsInstance.Settings.Ranking.GoldLimitMin;
                    if (ranking_type == RankingType.Cost_1)
                    {
                        limit_1 = GameSettingsInstance.Settings.Ranking.GemsLimitMinWeekly;
                    }
                }
                else
                {
                    limit_0 = 0;
                    limit_1 = 0;
                    sort = "rank";
                }

                //
                List<DatabaseResultItemSet>? list = null;

                // 
                string sql =
                $"SELECT " +
                $"	  `uid`, `id`, `name`, `type`, " +
                $"    `balance`, `cost`, `currency`, `rank`, " +
                $"    `create_time`, `last_time` " +
                $"FROM `t_rankings` " +
                $"WHERE " +
                $"	  `balance` >= ? AND `cost` >= ? AND `type` = ? AND " +
                $"    `status` > 0 " +
                $"    AND YEARWEEK(`last_time`, 1) = YEARWEEK(CURDATE() - INTERVAL 1 WEEK, 1) " +
                $"ORDER BY ? DESC " +
                $"LIMIT 100; ";
                var result_code = db?.QueryWithList(sql, out list,
                    limit_0, limit_1, (int)ranking_type,
                    sort);
                if (result_code < 0)
                {
                    return -1;
                }

                items = this.ToUserRankingItems(list);

                return items.Count;
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }
    }
}