
## InternalService 说明

增加物品和消耗物品需要注意，请求是消耗物品及数量，服务端反馈是当前剩余物品及数量。如果是已经消耗完，该物品被删除，数量是0.

### 更新日志：
 - 原有api/internal/game/pvp/completed 不再使用，避免重复结算或统计

### 协议新增：
1. 通用定义，适用在增加物品和消耗物品
```csharp
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class NUserInventoryItemsResult
    {

        [JsonPropertyName("virtual_currency")]
        public Dictionary<int, object?>? VirtualCurrency = null;
        
        [JsonPropertyName("items")]
        public string ItemValues = "";
    }
```

2. 增加物品NProtocol协议
```csharp
    /// <summary>
    /// 增加物品
    /// Endpoint: api/internal/user/inventory/add
    /// </summary>
    [System.Serializable]
    public class NAddUserInventoryItemsRequest
    {
        /// <summary>
        /// 玩家账号
        /// </summary>
        [JsonPropertyName("user_uid")]
        public string UserID = "";

        /// <summary>
        /// 目前是PlayfabID
        /// </summary>
        [JsonPropertyName("custom_uid")]
        public string CustomID = "";

        /// <summary>
        /// 物品列表：ID|CNT, ...
        /// </summary>
        [JsonPropertyName("items")]
        public string Items = "";
    }

    [System.Serializable]
    public class NAddUserInventoryItemsResponse
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("data")]
        public NUserInventoryItemsResult? Data = null;
    }

```

3. 消耗物品协议NProtocol协议
```csharp
    /// <summary>
    /// 消耗物品
    /// Endpoint: api/internal/user/inventory/consumable
    /// </summary>
    [System.Serializable]
    public class NConsumableUserInventoryItemsRequest
    {
        /// <summary>
        /// 玩家账号
        /// </summary>
        [JsonPropertyName("user_uid")]
        public string UserID = "";

        /// <summary>
        /// 目前是PlayfabID
        /// </summary>
        [JsonPropertyName("custom_uid")]
        public string CustomID = "";

        /// <summary>
        /// 物品列表：ID|CNT, ...
        /// </summary>
        [JsonPropertyName("items")]
        public string Items = "";
    }

    [System.Serializable]
    public class NConsumableUserInventoryItemsResponse
    {
        [JsonPropertyName("code")]
        public int Code;

        [JsonPropertyName("data")]
        public NUserInventoryItemsResult? Data = null;
    }
```