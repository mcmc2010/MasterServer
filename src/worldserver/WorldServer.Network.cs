

namespace Server.World
{
    /// <summary>
    /// 
    /// </summary>
    public partial class WorldServer
    {
                
        /// <summary>
        /// 广播给所有用户
        /// 仅仅是世界服务中的所有玩家
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task<int> BroadcastAsync(byte[] data, int index, int level = 0)
        {
            if (data.Length == 0)
            {
                return 0;
            }

            int count = 0;

            foreach (var pair in _list)
            {
                await pair.Value.BroadcastAsync(data, index, level);
                count++;
            }

            return count;
        }
    }
}