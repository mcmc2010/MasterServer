using WebSocketSharp;
using WebSocketSharp.Server;
using Google.Protobuf;

namespace Server.Services
{
    public interface IService
    {
        public string NetworkID { get; }

        public Task<int> BroadcastAsync(byte[] data, int index, int level);
    }

    /// <summary>
    /// 
    /// </summary>
    public class Base : WebSocketBehavior, IService
    {
        public string NetworkID { get { return this.ID; } }

        protected int _packet_index = 0;
        protected byte[]? _packet_data = null;

        public bool HasPacket {
            get {
                return _packet_data != null && _packet_data.Length > 0;
            }
        }
        public byte[]? PacketData {
            get {
                return _packet_data;
            }
        }

        public async Task<int> BroadcastAsync(byte[] data, int index = 0, int level = 0)
        {
            return this.SendData(data, index, level);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        protected int SendData(byte[] data, int index = 0, int level = 0)
        {
            var packet = new Protocols.PacketData();
            packet.Header = new Protocols.PacketHeader() {
                Index = index,
                Level = level
            };

            // 加密处理，暂时不加密
            if(level > 0)
            {
                packet.Data = Google.Protobuf.ByteString.CopyFrom(data);
            }
            else
            {
                packet.Data = Google.Protobuf.ByteString.CopyFrom(data);
            }

            var buffer = packet.ToByteArray();

            // 
            if(!this.IsAlive || this.ReadyState != WebSocketState.Open)
            {
                return 0;
            }

            this.Send(buffer);

            return buffer.Length;
        }

        protected override void OnMessage (MessageEventArgs msg)
        {
            // 只处理二进制消息（Protobuf）
            if (!msg.IsBinary)
            {
                this.Close(CloseStatusCode.UnsupportedData, "Only supports binary data packets");
                return;
            }

            // 解密后的数据
            this._packet_index = 0;
            this._packet_data = msg.RawData;
            
            //
            var packet = Protocols.PacketData.Parser.ParseFrom(msg.RawData);
            this._packet_index = packet.Header.Index;
            if(packet.Header.Level > 0)
            {
                // 暂时没有处理协议加密
                this._packet_data = packet.Data.ToByteArray();
            }
            else
            {
                this._packet_data = packet.Data.ToByteArray();
            }
        }

        public T? GetPacketT<T>() where T : Google.Protobuf.IMessage<T>
        {
            if(this._packet_data == null || this._packet_data.Length == 0) {
                return default(T);
            }
            
            try
            {
                // 1. 获取类型T的Parser属性（静态属性）
                Type type = typeof(T);
                var property = type.GetProperty("Parser", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if(property == null) {
                    return default(T);
                }

                // 2. 获取Parser实例
                var parser = (MessageParser<T>)property.GetValue(null)!; // 静态属性传null
                var packet = parser.ParseFrom(this._packet_data);

                // 3
                return packet;
            } catch(Exception e) {
                System.Console.WriteLine(e.Message);
            }   
            return default(T);
        }
    }
}