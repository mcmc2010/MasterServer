
using AMToolkits.Utility;
using Logger;


namespace Server
{

    /// <summary>
    /// 
    /// </summary>
    public class UserBaseData
    {
        public string server_uid = "";
        public string client_uid = "";
        public string passphrase = "";
        public string token = "";
        public DateTime datetime = DateTime.Now;
        public string device = "";

        public int privilege_level = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public class UserAuthenticationData : UserBaseData
    {
        ///
        public string custom_id = "";
        ///
        public string jwt_token = "";
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        protected async Task<int> AuthenticationAndInitUser(UserAuthenticationData user_data)
        {
            // DB 验证
            int result_code = this.DBAuthUser(user_data);
            if (result_code < 0)
            {
                user_data.passphrase = "";
                user_data.token = "";
                return result_code;
            }

            // 2: HOL
            result_code = this.DBInitHOL(user_data);
            if (result_code < 0)
            {
                return result_code;
            }

            // 3: JWT
            string hash = "";
            if (_config?.JWTEnabled == true)
            {
                hash = JWTAuth.JWTSignData(new Dictionary<string, object>() {
                    { "uid", user_data.client_uid },
                    { "server_uid", user_data.server_uid },
                    { "token", user_data.token }
                }, _config?.JWTSecretKey ?? "", _config?.JWTExpired ?? -1);
                // JWT 认证失败
                if (hash.Length == 0)
                {
                    //result_code = -100;
                }
            }
            user_data.jwt_token = hash;

            // 4:
            // Add User To Manager
            var user = this.AllocT<UserBase>();
            user.ID = user_data.server_uid;
            user.ClientID = user_data.client_uid;
            user.AccessToken = user_data.token;
            user.Passphrase = user_data.passphrase;

            user.PrivilegeLevel = user_data.privilege_level;

            // 自定义ID，这里目前是PlayFabId
            user.CustomID = user_data.custom_id;

            this.AddUser(user);

            //
            _logger?.Log($"(User) Auth User:{user_data.client_uid} - {user_data.server_uid}, Token:{user_data.token} Result: {result_code}");
            if (user_data.privilege_level >= 7)
            {
                _logger?.LogWarning($"(User) Admin:{user_data.server_uid}, Level:{user_data.privilege_level}");
            }

            // 5.
            var items = await PlayFabService.Instance.PFGetInventoryItems(user_data.server_uid, user_data.custom_id, "user_init");
            if (items != null && items.Data?.ItemList != null)
            {
                int result_count = 0;
                if ((result_count = await this._DBUpdateUserInventoryItems(user_data.server_uid, [.. items.Data.ItemList])) < 0)
                {
                    _logger?.LogWarning($"(User:{user_data.server_uid}) UpdateInventoryItems Faile (Count: {items.Data.ItemList.Length}) Result:${result_count}");
                }
            }
            else
            {
                _logger?.LogWarning($"(User:{user_data.server_uid}) GetInventoryItems Failed");
            }

            return result_code;
        }
    }
}