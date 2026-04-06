
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;
using Logger;


namespace Server.World
{
    /// <summary>
    /// 
    /// </summary>
    public partial class WorldServer
    {
        /// <summary>
        /// 
        /// </summary>
        protected ConcurrentDictionary<string, Services.WorldService> _list = new ConcurrentDictionary<string, Services.WorldService>();
        protected ConcurrentDictionary<string, object?> _user_list = new ConcurrentDictionary<string, object?>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public int DoAccept(Services.WorldService service)
        {
            string session_id = "";
            IUserSession? user = null;
            int result_code = AuthenticationSession(service, out session_id, out user);
            if (result_code <= 0 || session_id.IsNullOrWhiteSpace() || user == null)
            {
                service.DoClose(WebSocketSharp.CloseStatusCode.PolicyViolation, "manual");
                return 0;
            }

            // 检查是否已经在线（重复连接）
            if (IsPlayerOnline(user.UserID))
            {
                service.DoClose(WebSocketSharp.CloseStatusCode.Abnormal, "repeated");
                return -1;
            }

            // 初始化会话
            service.InitSession(session_id, user.UserID);
            
            // 添加到连接列表
            _list.AddOrUpdate(session_id, service, (k, v) => service);
            
            // 添加到在线玩家列表
            AddOnlinePlayer(session_id, user.UserID);

            // 广播上线通知
            var playerData = GetPlayerOnlineData(user.UserID);
            if (playerData != null)
            {
                _ = BroadcastPlayerOnlineNotify(playerData);
            }

            _logger?.Log($"[WorldServer] Player {user.UserID} connected");
            return 1;
        }

        public void DoClose(Services.WorldService service)
        {
            if (service.SessionID.IsNullOrWhiteSpace())
            {
                return;
            }

            // 获取玩家数据用于通知
            var playerData = GetPlayerOnlineData(service.UserID);

            // 从连接列表中移除
            _list.TryRemove(service.SessionID, out _);
            
            // 从在线玩家列表中移除
            RemoveOnlinePlayer(service.UserID);

            service.FreeSession();

            // 广播下线通知
            if (playerData != null)
            {
                _ = BroadcastPlayerOfflineNotify(playerData);
                _logger?.Log($"[WorldServer] Player {service.UserID} disconnected");
            }
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="session_id"></param>
        /// <returns></returns>
        protected int AuthenticationSession(Services.WorldService service,
                    out string session_id,
                    out IUserSession? user)
        {
            session_id = "";
            user = null;
            var (key, hash) = service.GetAuthorizationData();
            var session = UserManager.Instance.RequireAuthenticationSession(key, hash);
            if (session == null)
            {
                return 0;
            }

            //
            session_id = session.UID;
            user = session;
            return 1;
        }
    }
}