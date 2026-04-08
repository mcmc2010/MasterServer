
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;


namespace Server.Field
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        [JsonPropertyName("id")]
        public string ID = "";
        [JsonPropertyName("rid")]
        public int RID = 0;
        [JsonPropertyName("session_id")]
        public string SessionID = "";
    }
    

    /// <summary>
    /// 
    /// </summary>
    public partial class FieldServer
    {
        /// <summary>
        /// 
        /// </summary>
        protected ConcurrentDictionary<string, Services.FieldService> _list = new ConcurrentDictionary<string, Services.FieldService>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        public int DoAccept(Services.FieldService service)
        {
            string session_id = "";
            int result_code = AuthenticationSession(service, out session_id);
            if (result_code <= 0 || session_id.IsNullOrWhiteSpace())
            {
                service.DoClose(WebSocketSharp.CloseStatusCode.PolicyViolation, "manual");
                return 0;
            }

            if(_list.TryGetValue(session_id, out var value) && value != null)
            {
                service.DoClose(WebSocketSharp.CloseStatusCode.Abnormal, "repeated");
                return -1;
            }

            service.InitSession(session_id);
            _list.AddOrUpdate(session_id, service, (k, v) => service);
            return 1;
        }

        public void DoClose(Services.FieldService service)
        {
            if (service.SessionID.IsNullOrWhiteSpace())
            {
                return;
            }

            if (!_list.TryRemove(service.SessionID, out _))
            {
                return;
            }
            
            service.FreeSession();
        }

        protected int AuthenticationSession(Services.FieldService service, out String session_id)
        {
            session_id = "";
            var (key, hash) = service.GetAuthorizationData();
            var session = UserManager.Instance.RequireAuthenticationSession(key, hash);
            if (session == null)
            {
                return 0;
            }

            session_id = session.UserID;
            return 1;
        }

        /// <summary>
        /// 获取玩家的 FieldService 连接
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>FieldService 实例，如果未找到则返回 null</returns>
        public Services.FieldService? GetPlayerService(string userId)
        {
            if (_list.TryGetValue(userId, out var service))
            {
                return service;
            }
            return null;
        }
    }
}