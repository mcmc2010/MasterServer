using System.Text.Json.Serialization;


using AMToolkits.Extensions;

using Logger;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class PFNItemData
    {
        [JsonPropertyName("user_uid")]
        public string UID = "";
        [JsonPropertyName("playfab_uid")]
        public string PlayFabUID = "";
        [JsonPropertyName("items")]
        public AMToolkits.Game.GeneralItemData[]? ItemList = null;
    }

    [System.Serializable]
    public class PFResultItemData : PFResultBaseData
    {
        public PFNItemData? Data = null;
    }

    [System.Serializable]
    public class PFGetInventoryItemsResponse : AMToolkits.Net.HTTPResponseResult
    {
        public PFResultItemData? Data = null;
    }

    [System.Serializable]
    public class PFAddInventoryItemsResponse : AMToolkits.Net.HTTPResponseResult
    {
        public PFResultItemData? Data = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class PlayFabService
    {
        #region Inventory
        public async Task<PFResultItemData?> PFGetInventoryItems(string user_uid, string playfab_uid, string reason = "")
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }


            var response = await this.APICall<PFGetInventoryItemsResponse>("/internal/services/user/inventory/list",
                    new Dictionary<string, object>()
                    {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid },
                        { "reason", reason }
                    });
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) GetInventoryItems Failed: ({playfab_uid}) {_client_factory?.LastError?.Message}");
                return null;
            }

            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) GetInventoryItems Failed: ({playfab_uid}) [{response.Data?.Result}:{response.Data?.Error}]");
                return null;
            }

            return response.Data;
        }


        /// <summary>
        /// 增加物品
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async Task<PFResultItemData?> PFAddInventoryItems(string user_uid, string playfab_uid,
                                    List<AMToolkits.Game.GeneralItemData> list,
                                    string reason = "")
        {
            if (_status != AMToolkits.ServiceStatus.Ready)
            {
                return null;
            }

            if (list.Count == 0)
            {
                return null;
            }

            var response = await this.APICall<PFAddInventoryItemsResponse>("/internal/services/user/inventory/add",
                    new Dictionary<string, object>()
                    {
                        { "user_uid", user_uid },
                        { "playfab_uid", playfab_uid },
                        { "items", list },
                        { "reason", reason }
                    });
            if (response == null)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) AddInventoryItems Failed: ({playfab_uid}) {_client_factory?.LastError?.Message}");
                return null;
            }

            if (response.Data?.Result != AMToolkits.ServiceConstants.VALUE_SUCCESS)
            {
                _logger?.LogError($"{TAGName} (User:{user_uid}) AddInventoryItems Failed: ({playfab_uid}) [{response.Data?.Result}:{response.Data?.Error}]");
                return null;
            }

            return response.Data;
        }
        #endregion
    }
}