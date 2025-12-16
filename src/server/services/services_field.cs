


using WebSocketSharp;

namespace Server.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class FieldService : Services.Base
    {
        private string _session_id = "";
        public string SessionID => _session_id;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void InitSession(string id)
        {
            _session_id = id;
        }

        public void FreeSession()
        {
            _session_id = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public (String key, String hash) GetAuthorizationData()
        {
            string token = this.QueryString["token"] ?? "";
            string text = token.ToString().Trim();
            if (text.Length == 0)
            {
                text = this.Headers["X-Authorization"] ?? "";
                text = text.Trim();
            }

            string key = "";
            string hash = "";

            string[] values = text.Split(":");
            if (values.Length > 0)
            {
                key = values[0].Trim();
            }
            if (values.Length > 1)
            {
                hash = values[1].Trim();
            }

            return (key, hash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        public void DoClose(CloseStatusCode code = CloseStatusCode.Normal, String reason = "none")
        {
            this.Close(code, reason);
        }
        

        protected override void OnOpen()
        {
            //
            base.OnOpen();
            
            Field.FieldServer.Instance.DoAccept(this);
        }
        
        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            Field.FieldServer.Instance.DoClose(this);
        }
    }
}