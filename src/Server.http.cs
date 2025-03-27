using Microsoft.AspNetCore.Http;

namespace Server
{
    public partial class ServerApplication
    {
        public int CheckSecretKey(HttpContext context)
        {
            var headers = context.Request.Headers;
            if (!headers.TryGetValue("X-Secret-Key", out var value))
            {
                return -1;
            }

            bool is_root_key = false;
            string key = value.ToString().Trim().ToUpper();
            if(key.StartsWith("ROOT"))
            {
                is_root_key = true;

                string[] values = key.Split(":");
                if(values.Length > 1) {
                    key = values[1];
                }
            }

            if(key.Length != 16 && key.Length != 32 && key.Length != 64)
            {
                return -2;
            }

            if(is_root_key && key != ServerConfigLoader.Config.SecretKey)
            {
                return -7;
            }

            return 0;
        }
    }
}