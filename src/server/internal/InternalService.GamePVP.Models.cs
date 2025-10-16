
using System.Reflection.Emit;
using AMToolkits.Extensions;
using Logger;


namespace Server
{
    public partial class InternalService
    {
        /// <summary>
        /// 更新玩家游戏数据
        ///         - HOL 数据：局数，获胜局数，连胜，最高连胜
        /// </summary>
        /// <param name="id"></param>
        /// <param name="room_type"></param>
        /// <param name="room_level"></param>
        /// <param name="start_time"></param>
        /// <param name="end_time"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> DBUpdateGamePVPData(string id,
                int room_type, int room_level,
                DateTime? start_time, DateTime? end_time,
                NGamePVPPlayerData data)
        {
            if (data.UserID.IsNullOrWhiteSpace())
            {
                return -1;
            }

            // 只有获胜才递增
            int added_count = 0;
            if (data.IsVictory)
            {
                added_count = 1;
            }

            var db = DatabaseManager.Instance.New();
            try
            {
                // 1: 获取记录
                string sql =
                    $"SELECT " +
                    $"	`played_count`, `played_win_count`, `winning_streak_count`, `winning_streak_highest`, " +
                    $"  `create_time`, `last_time` " +
                    $"FROM `t_hol` " +
                    $"WHERE id = ? AND `season` = ? AND status > 0;";
                var result_code = db?.Query(sql, data.UserID,
                        GameSettingsInstance.Settings.Season.Code);
                if (result_code < 0)
                {
                    return -1;
                }

                // 增加连胜纪录
                int winning_streak_count = (int)(db?.ResultItems["winning_streak_count"]?.Number ?? 0);
                int winning_streak_highest = (int)(db?.ResultItems["winning_streak_highest"]?.Number ?? 0);
                if (data.IsVictory)
                {
                    winning_streak_count++;
                }
                else
                {
                    winning_streak_count = 0;
                }
                if (winning_streak_highest < winning_streak_count)
                {
                    winning_streak_highest = winning_streak_count;
                }

                // 2: 更新记录
                sql =
                    $"UPDATE `t_hol` SET " +
                    $"    `played_count` = `played_count` + 1, " +
                    $"    `played_win_count` = `played_win_count` + ?, " +
                    $"    `winning_streak_count` =  ?, " +
                    $"    `winning_streak_highest` =  ?, " +
                    $"    `last_time` = CURRENT_TIMESTAMP " +
                    $"WHERE `id` = ? AND `season` = ? AND `status` > 0;";
                result_code = db?.Query(sql,
                    added_count,
                    winning_streak_count, winning_streak_highest,
                    data.UserID,
                    GameSettingsInstance.Settings.Season.Code);
                if (result_code < 0)
                {
                    return -1;
                }

                return 1;
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (UpdateGamePVPData) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="id"></param>
        /// <param name="room_type"></param>
        /// <param name="room_level"></param>
        /// <param name="start_time"></param>
        /// <param name="end_time"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> DBAddGamePVPRecord(DatabaseQuery? query, string id,
                int room_type, int room_level,
                DateTime? start_time, DateTime? end_time,
                NGamePVPPlayerData data)
        {
            if (query == null)
            {
                return -1;
            }

            int tid = 0;
            if (data.AIPlayerIndex > 0)
            {
                tid = data.AIPlayerIndex;
            }
            string type = "normal";

            // 
            string sql =
                $"INSERT INTO `t_gamepvp` " +
                $"  (`sn`, `id`, `name`, `tid`,  " +
                $"  `type`, `level`, `room_id`, `game_status`,  " +
                $"  `create_time`,`last_time`,`end_time`, " +
                $"  `match_status`, " +
                $"  `status`) " +
                $"VALUES " +
                $"  (?, ?, ?, ?, " +
                $"   ?, ?, ?, ?, " +
                $"   ?, ?, ?, " +
                $" 'completed', " +
                $" 1); ";
            int result_code = query.Query(sql,
                    id, data.UserID, data.Name, tid,
                    type, room_level, 0, data.IsVictory ? "victory" : "failure",
                    start_time, end_time ?? start_time, end_time);
            if (result_code < 0)
            {
                return -1;
            }

            return 1;
        }

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
        public async System.Threading.Tasks.Task<int> DBUpdateGamePVPRecord(string id,
                int room_type, int room_level,
                DateTime? start_time, DateTime? end_time,
                NGamePVPPlayerData winner, NGamePVPPlayerData loser)
        {
            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                // 添加获胜玩家记录
                if (await DBAddGamePVPRecord(db, id, room_type, room_level, start_time, end_time, winner) < 0)
                {
                    db?.Rollback();
                    return -1;
                }
                // 添加失败玩家记录
                if (await DBAddGamePVPRecord(db, id, room_type, room_level, start_time, end_time, loser) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                //
                db?.Commit();
                return 1;
            }
            catch (Exception e)
            {
                db?.Rollback();
                _logger?.LogError($"{TAGName} (UpdateGamePVPRecord) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
        }
    }
}