
using System.Text.Json;


namespace AMToolkits.Net
{
    
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class JsonSerializerItem<T> where T : class
    {
        public string JsonString
        {
            get
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(this,
                                this.GetType(),
                                new System.Text.Json.JsonSerializerOptions()
                                {
                                    IgnoreReadOnlyFields = true,
                                    IgnoreReadOnlyProperties = true,
                                    IncludeFields = true,
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                });
                    return json;
                }
                catch (Exception ex)
                {
                    return $"(JSON) ({this.GetType().Name}) Error :" + ex.Message;
                }
            }
        }
    }
}