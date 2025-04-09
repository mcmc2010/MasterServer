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
            await context.ResponseStatusAsync("success", "Hello world", HttpStatusCode.OK);
        }

        protected virtual async Task HandlePing(HttpContext context)
        {
            string value ;
            context.QueryString("timestamp", out value, "0");
            long remote_timestamp = long.Parse(value);
            long server_timestamp = Utils.GetLongTimestamp();
            
            float delay = Utils.DiffTimestamp(remote_timestamp, server_timestamp);


            await context.ResponseStatusAsync("success", "", server_timestamp, delay);
        }
    }
}