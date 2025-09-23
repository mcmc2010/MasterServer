
using Logger;
using Microsoft.AspNetCore.Builder;
using System.Text.Json.Serialization;
using AMToolkits.Extensions;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public enum GameEffectType
    {
        None = 0,
        Experience = 1,
        Economy = 2,
        Pass = 1000,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NGameEffectData
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("id")]
        public int ID = -1;
        [JsonPropertyName("name")]
        public string Name = "";

        [JsonPropertyName("user_id")]
        public string UserID = "";
        [JsonPropertyName("type")]
        public int EffectType = 0;
        [JsonPropertyName("sub_type")]
        public int EffectSubType = 0;
        [JsonPropertyName("group_index")]
        public int GroupIndex = 0;

        [JsonPropertyName("value")]
        public string EffectValue = "";


        [JsonPropertyName("create_time")]
        public DateTime? CreateTime = null;
        [JsonPropertyName("last_time")]
        public DateTime? LastTime = null;
        [JsonPropertyName("end_time")]
        public DateTime? EndTime = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class GameEffectsManager : AMToolkits.SingletonT<GameEffectsManager>, AMToolkits.ISingleton
    {
        [AMToolkits.AutoInitInstance]
        protected static GameEffectsManager? _instance;

        private string[]? _arguments = null;
        private ServerConfig? _config = null;
        private Logger.LoggerEntry? _logger = null;

        public GameEffectsManager()
        {

        }

        protected override void OnInitialize(object[] paramters)
        {
            _arguments = AMToolkits.CommandLineArgs.FirstParser(paramters);

            var config = paramters[1] as ServerConfig;
            if (config == null)
            {
                System.Console.WriteLine("[GameEffectsManager] Config is NULL.");
                return;
            }
            _config = config;
            _logger = Logger.LoggerFactory.Instance;
        }


        public void OnRegisterHandlers(object? sender, HandlerEventArgs args)
        {
            _logger?.Log($"{TAGName} Register Handlers");

            //
            //args.app?.MapPost("api/game/events/final", HandleGameEventFinal);

        }

    }
}