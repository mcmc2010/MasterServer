using System.Net;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;

namespace Server
{
    public partial class UserManager
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleUserAuth(HttpContext context)
        {
            if(ServerApplication.Instance.CheckSecretKey(context) < 0)
            {
                await context.ResponseError(HttpStatusCode.Unauthorized, ErrorMessage.NotAllowAccess_Unauthorized_NotKey);
                return;
            }

            await context.ResponseResult(new {
                Code = 0
            });
        }
    }
}