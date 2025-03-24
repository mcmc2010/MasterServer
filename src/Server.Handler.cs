using System.Net;
using Microsoft.AspNetCore.Http;

using Utils;

namespace Server
{
    public partial class ServerApplication
    {
        protected virtual async Task HandleHello(HttpContext context)
        {
            var address = context.GetClientAddress();

            var result = new {
                Status = "success",
                Timestamp = DateTime.UtcNow,
                Address = address.ToString()
            };

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsJsonAsync(result);
        }
    }
}