
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;


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

            if (_user_list.TryGetValue(user.UserID, out var value) && value != null)
            {
                service.DoClose(WebSocketSharp.CloseStatusCode.Abnormal, "repeated");
                return -1;
            }

            //
            var user_data = new Dictionary<string, object?>()
            {
                { "session_id", session_id},
                { "user_id", user.UserID }
            };

            service.InitSession(session_id, user.UserID);
            _list.AddOrUpdate(session_id, service, (k, v) => service);
            _user_list.AddOrUpdate(user.UserID, user_data, (k, v) => user_data);
            return 1;
        }

        public void DoClose(Services.WorldService service)
        {
            if (service.SessionID.IsNullOrWhiteSpace())
            {
                return;
            }

            //
            _list.TryRemove(service.SessionID, out _);
            _user_list.TryRemove(service.UserID, out _);

            service.FreeSession();
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