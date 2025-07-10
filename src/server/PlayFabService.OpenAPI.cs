
using System.Net;
using AMToolkits;
using AMToolkits.Extensions;
using AMToolkits.Net;

namespace Server
{
    public partial class PlayFabService
    {
        public bool APIResponseData<T>(T? response, out Dictionary<string, object?> result)
                where T : AMToolkits.Net.HTTPResponseResult
        {
            result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                { ServiceData.KEY_RESULT, ServiceData.VALUE_ERROR },
            };

            if (response == null) { return false; }
            if (response.Code != (int)HttpStatusCode.OK) { return false; }

            object? value = null;
            var type = response.GetType();
            var field = type.GetField("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
            if (field != null)
            {
                value = field.GetValue(response);
            }
            else
            {
                var property = type.GetProperty("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                if (property != null)
                {
                    value = property.GetValue(response);
                }
            }
            if (value == null)
            {
                return false;
            }

            if (value is Dictionary<string, object?> pairs)
            {
                result.AddRange(pairs);
                return true;
            }

            return false;
        }

        public async Task<T?> APIGet<T>(string endpoint,
                        Dictionary<string, object>? arguments = null,
                        Dictionary<string, object>? headers = null,
                        bool is_authentication = true)
                        where T : HTTPResponseResult
        {
            if (_client_factory == null)
            {
                return default(T);
            }

            string secret_key = _config?.PlayFab.OpenAPIKey ?? "";

            Dictionary<string, object> additional_headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                foreach (var v in headers)
                {
                    additional_headers[v.Key] = v.Value;
                }
            }

            if (is_authentication)
            {
                additional_headers["X-SecretKey"] = secret_key;
            }

            return await _client_factory.GetAsync<T>(endpoint, arguments, additional_headers);
        }
        
        public async Task<T?> APICall<T>(string endpoint, object? payload,
                        Dictionary<string, object>? arguments = null,
                        Dictionary<string, object>? headers = null,
                        bool is_authentication = true)
                        where T : HTTPResponseResult
        {
            if (_client_factory == null)
            {
                return default(T);
            }

            string secret_key = _config?.PlayFab.OpenAPIKey ?? "";

            Dictionary<string, object> additional_headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                foreach (var v in headers)
                {
                    additional_headers[v.Key] = v.Value;
                }
            }
            
            if (is_authentication)
            {
                additional_headers["X-SecretKey"] = secret_key;
            }

            return await _client_factory.PostAsync<T>(endpoint, payload, additional_headers, arguments);
        }
    }
}