using WebSocketSharp;
using WebSocketSharp.Server;


namespace Server.Packets
{
    /// <summary>
    /// 
    /// </summary>
    public class Echo : WebSocketBehavior
    {
        protected override void OnMessage (MessageEventArgs e)
        {
            this.Send (e.Data);
        }
    }
}