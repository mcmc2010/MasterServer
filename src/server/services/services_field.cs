


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