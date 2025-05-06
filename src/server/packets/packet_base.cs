using WebSocketSharp;
using WebSocketSharp.Server;


namespace Server.Packets
{
    /// <summary>
    /// 
    /// </summary>
    public class Base : WebSocketBehavior
    {
        protected byte[]? _data = null;
        public bool HasPacket {
            get {
                return _data != null;
            }
        }

        protected override void OnMessage (MessageEventArgs msg)
        {
            // 只处理二进制消息（Protobuf）
            if (!msg.IsBinary)
            {
                this.Close(CloseStatusCode.UnsupportedData, "Only supports binary data packets");
                return;
            }

            this._data = msg.RawData;
        }
    }
}