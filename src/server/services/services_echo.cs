
using WebSocketSharp;
using WebSocketSharp.Server;


namespace Server.Services
{
    /// <summary>
    /// 不继承基础类
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

        /// <summary>
        /// 回显不需要处理数据包
        /// </summary>
        /// <param name="msg"></param>
        protected override void OnMessage (MessageEventArgs msg)
        {
            this.Send (msg.Data);
        }
    }
}