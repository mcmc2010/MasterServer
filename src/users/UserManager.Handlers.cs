using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;
using Microsoft.VisualBasic;


namespace Server
{
    #region User
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NAuthUserRequest
    {
        [JsonPropertyName("uid")]
        public string UID = "";
        [JsonPropertyName("type")]
        public string Type = "NONE";
        [JsonPropertyName("session_uid")]
        public string SessionUID = "";
        [JsonPropertyName("session_token")]
        public string SessionToken = "";
    }

    [System.Serializable]
    public class NAuthUserResponse
    {
        [JsonPropertyName("code")]
        public int Code = 0;
        [JsonPropertyName("uid")]
        public string UID = "";
        [JsonPropertyName("server_uid")]
        public string ServerUID = "";
        [JsonPropertyName("passphrase")]
        public string Passphrase = "";
        [JsonPropertyName("token")]
        public string Token = "";
        [JsonPropertyName("time")]
        public string DateTime = "";
        [JsonPropertyName("hash")]
        public string Hash = "";
        [JsonPropertyName("privilege_level")]
        public int PrivilegeLevel = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NUserProfileRequest
    {
        [JsonPropertyName("uid")]
        public string UID = "";
    }


    [System.Serializable]
    public class NUserUpdateProfileRequest
    {
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl = "";
    }


    [System.Serializable]
    public class NUserProfileResponse
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("data")]
        public UserProfile? Profile = null;
    }

    [System.Serializable]
    public class NUserProfileExtendResponse 
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("data")]
        public UserProfileExtend? Profile = null;
    }


    [System.Serializable]
    public class NUserChangeNameRequest
    {
        [JsonPropertyName("name")]
        public string Name = "";
    }

    [System.Serializable]
    public class NUserChangeNameResponse
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("name")]
        public string Name = "";

        [JsonPropertyName("last_name")]
        public string LastName = "";

        [JsonPropertyName("data")]
        public UserProfile? Profile = null;
    }

    #endregion

    #region User Inventory NProtocols
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NGetUserInventoryItemsRequest
    {
    }

    [System.Serializable]
    public class NGetUserInventoryItemsResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("items")]
        public List<NUserInventoryItem>? Items = null;
    }


    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NUsingUserInventoryItemsRequest
    {
        [JsonPropertyName("iid")]
        public string IID = "";
        [JsonPropertyName("index")]
        public int Index = 0;
    }

    [System.Serializable]
    public class NUsingUserInventoryItemsResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("items")]
        public List<NUserInventoryItem>? Items = null;
    }

    /// <summary>
    /// 升级物品
    /// </summary>
    [System.Serializable]
    public class NUpgradeUserInventoryItemsRequest
    {
        [JsonPropertyName("iid")]
        public string IID = "";
        [JsonPropertyName("index")]
        public int Index = 0;
    }

    [System.Serializable]
    public class NUpgradeUserInventoryItemsResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("items")]
        public List<NUserInventoryItem>? Items = null;
    }
    #endregion

    #region User Game Events NProtocols

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NGetUserGameEventsRequest
    {
    }

    [System.Serializable]
    public class NGetUserGameEventsResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("items")]
        public List<NGameEventData>? Items = null;
    }

    #endregion

    #region User Game Effects NProtocols

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NGetUserGameEffectsRequest
    {
    }

    [System.Serializable]
    public class NGetUserGameEffectsResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("items")]
        public List<NGameEffectData>? Items = null;
    }

    #endregion

    #region User Rank NProtocols
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NGetUserRankDataRequest
    {
    }

    [System.Serializable]
    public class NGetUserRankDataResponse
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("data")]
        public UserRankDataExtend? Data = null;
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    public partial class UserManager
    {
        #region User
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUserAuth(HttpContext context)
        {
            if (ServerApplication.Instance.CheckSecretKey(context) < 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotKey);
                return;
            }

            //
            var platform = "";
            context.GetOSPlatform(out platform);

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NAuthUserRequest>();

            string uid = AMToolkits.Utility.Guid.GeneratorID12N();

            long time = AMToolkits.Utils.GetLongTimestamp();
            int rand = AMToolkits.Utility.Guid.GeneratorID6();
            string passphrase = AMToolkits.Utility.Guid.GeneratorID8();
            string token = $"{uid}_{time}_{passphrase}_{rand}";
            token = AMToolkits.Hash.SHA256String(token);
            string date_time = AMToolkits.Utils.DateTimeToString(DateTime.UtcNow,
                    AMToolkits.Utils.DATETIME_FORMAT_LONG_STRING);

            // 0: 第三方验证平台
            // PlayFab验证
            int result_code = await PlayFabService.Instance.PFUserAuthentication(request?.UID ?? "",
                    request?.SessionUID ?? "",
                    request?.SessionToken ?? "");
            if (result_code < 0)
            {
                _logger?.LogWarning($"(User) Auth User (ClientUID:{request?.UID} - {request?.SessionUID}) Failed, Result: {result_code}");

                //
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotLogin);
                return;
            }

            // 1: 验证用户
            var user_data = new UserAuthenticationData()
            {
                server_uid = uid,
                client_uid = request?.UID ?? "",
                custom_id = request?.SessionUID ?? "",
                passphrase = passphrase,
                token = token,
                datetime = DateTime.Now,
                device = $"{platform}",

                //
                jwt_token = ""
            };
            if (result_code == 0)
            {
                user_data.is_test_user = true;
            }

            result_code = await this.AuthenticationAndInitUser(user_data);
            if (result_code <= 0)
            {
                _logger?.LogWarning($"(User) Auth User (ClientUID:{user_data.client_uid} - {user_data.server_uid}) Failed, Result: {result_code}");
            }

            //
            var result = new NAuthUserResponse
            {
                Code = result_code,
                UID = user_data.client_uid,
                ServerUID = user_data.server_uid,
                Passphrase = user_data.passphrase,
                Token = user_data.token,
                DateTime = date_time,
                Hash = user_data.jwt_token,
                PrivilegeLevel = user_data.privilege_level,
            };

            //
            await context.ResponseResult(result);
        }


        protected async Task HandleUserProfile(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NUserProfileRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NUserProfileExtendResponse
            {
                Code = 0,
            };

            UserProfileExtend profile = new UserProfileExtend();
            int result_code = await this.GetUserProfile(auth_data.id, request.UID.Trim(), profile);
            if (result_code > 0)
            {
                result.Profile = profile;
            }

            result.Code = result_code;

            await context.ResponseResult(result);
        }

        /// <summary>
        /// 用户属性修改
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUserUpdateProfile(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NUserUpdateProfileRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NUserProfileResponse
            {
                Code = 0,
            };

            UserProfile profile = new UserProfile();
            profile.AvatarUrl = request.AvatarUrl;
            int result_code = await UpdateUserProfile(auth_data.id, profile);
            if (result_code > 0)
            {
                result.Profile = profile;
            }

            result.Code = result_code;

            await context.ResponseResult(result);
        }

        /// <summary>
        /// 用户改名
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUserChangeName(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NUserChangeNameRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NUserChangeNameResponse
            {
                Code = 0,
                Name = request.Name,
                LastName = ""
            };

            UserProfile profile = new UserProfile();
            int result_code = await ChangeUserName(auth_data.id, request.Name, profile);
            if (result_code > 0)
            {
                result.Profile = profile;
                result.LastName = profile.Name;
                result.Profile.Name = request.Name;
            }

            result.Code = result_code;

            await context.ResponseResult(result);
        }

        #endregion

        #region User Inventory
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleGetUserInventoryItems(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NGetUserInventoryItemsRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NGetUserInventoryItemsResponse
            {
                Code = 0,
                Items = null
            };

            List<NUserInventoryItem> list = new List<NUserInventoryItem>();
            // 成功返回物品数量
            int result_code = await this.GetUserInventoryItems(auth_data.id, list);
            if (result_code <= 0)
            {
                result.Code = result_code;
            }
            else
            {
                result.Code = 1;
                result.Items = list;
            }
            //
            await context.ResponseResult(result);
        }


        /// <summary>
        /// 使用物品
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUsingUserInventoryItem(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NUsingUserInventoryItemsRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NUsingUserInventoryItemsResponse
            {
                Code = 0,
                Items = null
            };

            List<NUserInventoryItem> list = new List<NUserInventoryItem>();
            int result_code = await this.UsingUserInventoryItem(auth_data.id, request.IID, request.Index, list);
            if (result_code > 0)
            {
                result.Items = list;
            }

            result.Code = result_code;

            //
            await context.ResponseResult(result);
        }

        /// <summary>
        /// 升级物品
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUpgradeUserInventoryItem(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NUpgradeUserInventoryItemsRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NUpgradeUserInventoryItemsResponse
            {
                Code = 0,
                Items = null
            };

            List<NUserInventoryItem> list = new List<NUserInventoryItem>();
            int result_code = await this.UpgradeUserInventoryItem(auth_data.id, request.IID, request.Index, list);
            if (result_code > 0)
            {
                result.Items = list;
            }

            result.Code = result_code;

            //
            await context.ResponseResult(result);
        }

        #endregion

        #region Game Events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleGetUserGameEvents(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NGetUserGameEventsRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NGetUserGameEventsResponse
            {
                Code = 0,
                Items = null
            };

            List<NGameEventData> list = new List<NGameEventData>();
            // 成功返回物品数量
            int result_code = await this.GetUserGameEvents(auth_data.id, list);
            if (result_code <= 0)
            {
                result.Code = result_code;
            }
            else
            {
                result.Code = 1;
                result.Items = list;
            }
            //
            await context.ResponseResult(result);
        }
        #endregion

        #region Game Effects
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleGetUserGameEffects(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NGetUserGameEffectsRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NGetUserGameEffectsResponse
            {
                Code = 0,
                Items = null
            };

            List<NGameEffectData> list = new List<NGameEffectData>();
            // 成功返回物品数量
            int result_code = await this.GetUserGameEffects(auth_data.id, list);
            if (result_code <= 0)
            {
                result.Code = result_code;
            }
            else
            {
                result.Code = 1;
                result.Items = list;
            }
            //
            await context.ResponseResult(result);
        }
        #endregion


        #region Rank
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleGetUserRank(HttpContext context)
        {
            SessionAuthData auth_data = new SessionAuthData();
            if (await ServerApplication.Instance.AuthSessionAndResult(context, auth_data) <= 0)
            {
                return;
            }

            // 解析 JSON
            var request = await context.Request.JsonBodyAsync<NGetUserRankDataRequest>();
            if (request == null)
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            //
            var result = new NGetUserRankDataResponse
            {
                Code = 0,
                Data = null
            };

            // 成功返回
            UserRankDataExtend? extend = await this.GetUserRank(auth_data.id);
            if (extend == null)
            {
                result.Code = -1;
            }
            else
            {
                result.Code = 1;
                result.Data = extend;
            }

            //
            await context.ResponseResult(result);
        }
        #endregion

    }
}