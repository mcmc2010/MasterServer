using WebSocketSharp;
using WebSocketSharp.Server;


namespace Server.Packets
{
    /// <summary>
    /// 
    /// </summary>
    public class Chat : Server.Packets.Base
    {
        protected int Authentication()
        {
            string token = this.QueryString["token"] ?? "";
            string text = token.ToString().Trim();
            
            string key = "";
            string hash = "";

            string[] values = text.Split(":");
            if(values.Length > 0) {
                key = values[0].Trim();
            }
            if(values.Length > 1) {
                hash = values[1].Trim();
            }

            var user = UserManager.Instance.GetAuthUser(key, hash);
            if(user != null)
            {
                return 1;
            }
            return 0;
        }

        protected override void OnOpen()
        {
            if(Authentication() <= 0)
            {
                this.Close(CloseStatusCode.PolicyViolation, "Access Denied");
                return;
            }

            base.OnOpen();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
        }

        protected override void OnMessage (MessageEventArgs msg)
        {
            base.OnMessage(msg);

            if(!this.HasPacket)
            {
                return;
            }
        }
    }
}