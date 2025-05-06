using WebSocketSharp;
using WebSocketSharp.Server;


namespace Server.Packets
{
    /// <summary>
    /// 
    /// </summary>
    public class Echo : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
        }

        protected override void OnMessage (MessageEventArgs msg)
        {
            this.Send (msg.Data);
        }
    }
}