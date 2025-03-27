using System.Net;
using Microsoft.AspNetCore.Http;

using AMToolkits.Extensions;
using AMToolkits.Utility;

namespace Server
{
    public partial class ServerApplication
    {
        protected virtual async Task HandleHello(HttpContext context)
        {
            var address = context.GetClientAddress();

            var result = new {
                Status = "success",
                DateTime = DateTime.UtcNow,
                Address = address.ToString()
            };

            await context.ResponseJsonAsync(result);
        }

        protected virtual async Task HandlePing(HttpContext context)
        {
            string value ;
            context.QueryString("timestamp", out value, "0");
            long remote_timestamp = long.Parse(value);
            long server_timestamp = Utils.GetLongTimestamp();
            
            float delay = Utils.DiffTimestamp(remote_timestamp, server_timestamp);
            if(delay < Utils.NETWORK_DELAY_MIN) {
                delay = Utils.NETWORK_DELAY_MIN;
            }

            var result = new {
                Status = "success",
                Timestamp = server_timestamp,
                Delay = delay
            };

            await context.ResponseJsonAsync(result);
        }
    }
}