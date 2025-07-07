using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigItem_PlayFab
    {
        [YamlMember(Alias = "openapi_url")]
        public string OpenAPIUrl { get; set; } = "https://127.0.0.1:15443"; 
        [YamlMember(Alias = "openapi_key")]
        public string OpenAPIKey { get; set; } = "";
    }
}