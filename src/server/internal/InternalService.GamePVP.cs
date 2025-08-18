
namespace Server
{
    public partial class InternalService
    {
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
            if (winner.AIPlayerIndex == 0 && await DBUpdateGamePVPData(id, room_type, room_level, start_time, end_time, loser) < 0)
            {
                return -1;
            }

            return 1;
        }


    }
}