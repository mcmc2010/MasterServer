/// 重新命名了排行榜
/// 

using Logger;



namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class LeaderboardManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected List<LeaderboardItem> ToLeaderboardItems(List<DatabaseResultItemSet>? rows)
        {
            List<LeaderboardItem> items = new List<LeaderboardItem>();
            if (rows != null)
            {
                foreach (var v in rows)
                {
                    LeaderboardItem? item = v.To<LeaderboardItem>();
                    if (item == null) { continue; }

                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected async Task<int> DBGetLeaderboardRecords(DatabaseQuery? query, string user_uid,
                            List<LeaderboardItem> items)
        {
            //
            List<DatabaseResultItemSet>? list = null;
            // 
            string sql =
                $"SELECT " +
                $"    `uid`, `id`, `name`, `type`, " +
                $"    `create_time`, `last_time`,  " +
                $"    `balance`, `cost`, `currency`, `rank`, `status` " +
                $"FROM `t_leaderboard` " +
                $"WHERE " +
                $" `id` = ? AND `status` > 0;";
            var result_code = query?.QueryWithList(sql, out list,
                user_uid);
            if (result_code < 0 || list == null)
            {
                return -1;
            }

            items.AddRange(this.ToLeaderboardItems(list));
            return items.Count;
        }

        /// <summary>
        /// 添加，没有做数据查询回滚，这里设置为私有函数
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBAddLeaderboardRecord(DatabaseQuery? query, string user_uid, string? name,
                            string currency, int balance)
        {
            if (query == null)
            {
                return -1;
            }

            int type = (int)LeaderboardType.Default;
            if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS ||
                currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT)
            {
                type = (int)LeaderboardType.Paid;
            }

            // 
            string sql =
            $"INSERT INTO `t_leaderboard` " +
            $"  (`id`,`name`, `type`, " +
            $"  `create_time`, `last_time`, " +
            $"  `balance`, `currency`, `rank`, " +
            $"  `status`) " +
            $"VALUES " +
            $"(?, ?, ?, " +
            $" CURRENT_TIMESTAMP,CURRENT_TIMESTAMP, " +
            $" ?, ?, ?, " +
            $"1); ";
            int result_code = query.Query(sql,
                    user_uid, name, type,
                    balance, currency, "");
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }


        /// <summary>
        /// 添加，没有做数据查询回滚，这里设置为私有函数
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBAddLeaderboardRecord(DatabaseQuery? query, string user_uid, string? name,
                            string currency, double cost)
        {
            if (query == null)
            {
                return -1;
            }

            int type = (int)LeaderboardType.Cost_0;
            if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS ||
                currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT)
            {
                type = (int)LeaderboardType.Cost_1;
            }

            // 
            string sql =
            $"INSERT INTO `t_leaderboard` " +
            $"  (`id`,`name`, `type`, " +
            $"  `create_time`, `last_time`, " +
            $"  `cost`, `currency`, `rank`, " +
            $"  `status`) " +
            $"VALUES " +
            $"(?, ?, ?, " +
            $" CURRENT_TIMESTAMP,CURRENT_TIMESTAMP, " +
            $" ?, ?, ?, " +
            $"1); ";
            int result_code = query.Query(sql,
                    user_uid, name, type,
                    cost, currency, "");
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// 添加段位，没有做数据查询回滚，这里设置为私有函数
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBAddLeaderboardRecord(DatabaseQuery? query, string user_uid, string? name,
                            int rank_level, int rank_value)
        {
            if (query == null)
            {
                return -1;
            }

            // 
            string sql =
                $"INSERT INTO `t_leaderboard` " +
                $"  (`id`,`name`, `type`, " +
                $"  `create_time`, `last_time`, " +
                $"  `rank`, " +
                $"  `status`) " +
                $"VALUES " +
                $"(?, ?, ?, " +
                $" CURRENT_TIMESTAMP,CURRENT_TIMESTAMP, " +
                $" ?, " +
                $"1); ";
            int result_code = query.Query(sql,
                    user_uid, name, (int)LeaderboardType.Rank,
                    $"{rank_level},{rank_value}");
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }


        /// <summary>
        /// 更新事件物品
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="id"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected async Task<int> DBUpdateLeaderboardRecord(DatabaseQuery? query, string user_uid, string? name,
                                LeaderboardItem item)
        {
            if (query == null)
            {
                return -1;
            }

            // 
            string sql =
            $"UPDATE `t_leaderboard` " +
            $"SET " +
            $" `name` = ?, `balance` = ?, `cost` = ?, `rank` = ?, `last_time` = CURRENT_TIMESTAMP " +
            $"WHERE `id` = ? AND `type` = ? AND `status` > 0 ";
            int result_code = query.Query(sql,
                    name, item.balance, item.cost, item.rank, 
                    user_uid, item.type);
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }


        /// <summary>
        /// 统计余额
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="name"></param>
        /// <param name="currency"></param>
        /// <param name="balance"></param>
        /// <returns></returns>
        public async Task<int> DBUpdateLeaderboardRecord(string user_uid, string? name,
                            string currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT,
                            int balance = 0)
        {
            if (balance == 0)
            {
                return 0;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                //
                List<LeaderboardItem> list = new List<LeaderboardItem>();
                if (await this.DBGetLeaderboardRecords(db, user_uid, list) < 0)
                {
                    return -1;
                }

                var item = list.FirstOrDefault(v => v.currency == currency &&
                    (v.type == (int)LeaderboardType.Default || v.type == (int)LeaderboardType.Paid));
                if (item == null)
                {
                    if (await DBAddLeaderboardRecord(db, user_uid, name, currency, balance) < 0)
                    {
                        return -1;
                    }
                }
                else
                {
                    item.balance = balance;
                    if (await DBUpdateLeaderboardRecord(db, user_uid, name, item) < 0)
                    {
                        return -1;
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (UpdateLeaderboardRecord) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }


                /// <summary>
        /// 统计消费
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="name"></param>
        /// <param name="currency"></param>
        /// <param name="balance"></param>
        /// <returns></returns>
        public async Task<int> DBUpdateLeaderboardRecord(string user_uid, string? name,
                            string currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT,
                            double cost = 0.00)
        {
            if (cost == 0.00)
            {
                return 0;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                //
                List<LeaderboardItem> list = new List<LeaderboardItem>();
                if (await this.DBGetLeaderboardRecords(db, user_uid, list) < 0)
                {
                    return -1;
                }

                var item = list.FirstOrDefault(v => v.currency == currency &&
                    (v.type == (int)LeaderboardType.Cost_0 || v.type == (int)LeaderboardType.Cost_1));
                if (item == null)
                {
                    if (await DBAddLeaderboardRecord(db, user_uid, name, currency, cost) < 0)
                    {
                        return -1;
                    }
                }
                else
                {
                    item.cost = cost;
                    if (await DBUpdateLeaderboardRecord(db, user_uid, name, item) < 0)
                    {
                        return -1;
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (UpdateLeaderboardRecord) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }


        public async Task<int> DBUpdateLeaderboardRecord(string user_uid, string? name,
                            int rank_level, int rank_value = 0)
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                //
                List<LeaderboardItem> list = new List<LeaderboardItem>();
                if (await this.DBGetLeaderboardRecords(db, user_uid, list) < 0)
                {
                    return -1;
                }

                var item = list.FirstOrDefault(v => v.type == (int)LeaderboardType.Rank);
                if (item == null)
                {
                    if (await DBAddLeaderboardRecord(db, user_uid, name, rank_level, rank_value) < 0)
                    {
                        return -1;
                    }
                }
                else
                {
                    item.rank = $"{rank_level},{rank_value}";
                    if (await DBUpdateLeaderboardRecord(db, user_uid, name, item) < 0)
                    {
                        return -1;
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (UpdateLeaderboardRecord) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }

        /// <summary>
        /// 每日排行榜是昨天
        /// </summary>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> DBGetLeaderboardWithDaily(LeaderboardType type,
                            List<LeaderboardItem> items)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                string sort = "balance";
                int limit_1 = 0;
                int limit_0 = GameSettingsInstance.Settings.Leaderboard.GoldLimitMin;
                if (type == LeaderboardType.Default)
                {
                    // nothing
                }
                else if (type == LeaderboardType.Paid)
                {
                    limit_0 = GameSettingsInstance.Settings.Leaderboard.GemsLimitMin;
                }
                else if (type == LeaderboardType.Cost_0 || type == LeaderboardType.Cost_1)
                {
                    sort = "cost";
                    limit_0 = 0;
                    limit_1 = GameSettingsInstance.Settings.Leaderboard.GoldLimitMin;
                    if (type == LeaderboardType.Cost_1)
                    {
                        limit_1 = GameSettingsInstance.Settings.Leaderboard.GemsLimitMin;
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
                $"FROM `t_leaderboard` " +
                $"WHERE " +
                $"	  `balance` >= ? AND `cost` >= ? AND `type` = ? AND " +
                $"    `status` > 0 " +
                $"    AND YEAR(`last_time`) = YEAR(CURDATE() - INTERVAL 0 MONTH) " +
                $"    AND MONTH(`last_time`) = MONTH(CURDATE() - INTERVAL 0 MONTH) " +
                $"ORDER BY ? DESC " +
                $"LIMIT 100; ";
                var result_code = db?.QueryWithList(sql, out list,
                    limit_0, limit_1, (int)type,
                    sort);
                if (result_code < 0)
                {
                    return -1;
                }

                items.AddRange(this.ToLeaderboardItems(list));

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
        protected async System.Threading.Tasks.Task<int> DBGetLeaderboardWithWeekly(LeaderboardType type,
                            List<LeaderboardItem> items)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                string sort = "balance";
                int limit_1 = 0;
                int limit_0 = GameSettingsInstance.Settings.Leaderboard.GoldLimitMin;
                if (type == LeaderboardType.Default)
                {
                    // nothing
                }
                else if (type == LeaderboardType.Paid)
                {
                    limit_0 = GameSettingsInstance.Settings.Leaderboard.GemsLimitMinWeekly;
                }
                else if (type == LeaderboardType.Cost_0 || type == LeaderboardType.Cost_1)
                {
                    sort = "cost";
                    limit_0 = 0;
                    limit_1 = GameSettingsInstance.Settings.Leaderboard.GoldLimitMin;
                    if (type == LeaderboardType.Cost_1)
                    {
                        limit_1 = GameSettingsInstance.Settings.Leaderboard.GemsLimitMinWeekly;
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
                $"FROM `t_leaderboard` " +
                $"WHERE " +
                $"	  `balance` >= ? AND `cost` >= ? AND `type` = ? AND " +
                $"    `status` > 0 " +
                $"    AND YEARWEEK(`last_time`, 1) = YEARWEEK(CURDATE() - INTERVAL 1 WEEK, 1) " +
                $"ORDER BY ? DESC " +
                $"LIMIT 100; ";
                var result_code = db?.QueryWithList(sql, out list,
                    limit_0, limit_1, (int)type,
                    sort);
                if (result_code < 0)
                {
                    return -1;
                }

                items.AddRange(this.ToLeaderboardItems(list));

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

