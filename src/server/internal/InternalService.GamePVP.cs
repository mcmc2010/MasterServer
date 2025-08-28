
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
            if (winner.AIPlayerIndex == 0 && await DBUpdateGamePVPData(id, room_type, room_level, start_time, end_time, winner) < 0)
            {
                return -1;
            }
            if (loser.AIPlayerIndex == 0 && await DBUpdateGamePVPData(id, room_type, room_level, start_time, end_time, loser) < 0)
            {
                return -1;
            }

            return 1;
        }


    }
}