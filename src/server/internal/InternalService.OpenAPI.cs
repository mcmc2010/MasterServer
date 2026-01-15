using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using AMToolkits.Extensions;
using Logger;
using Microsoft.AspNetCore.Http.Extensions;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class InternalService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string?>? URLParseArguments(string url)
        {
            var list = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 创建一个 Uri 对象
                Uri uri = new Uri(url);

                // 获取查询字符串
                string query_string = uri.Query;

                if (string.IsNullOrEmpty(query_string) || query_string == "?")
                {
                    return null;
                }

                // 查找问号后的查询字符串
                int question = query_string.IndexOf('?');
                if (question < 0)
                {
                    return list;
                }
                query_string = query_string.Substring(question + 1);

                // 步骤1：先对整个字符串进行URL解码
                string decoded = System.Web.HttpUtility.UrlDecode(query_string);
                // 步骤2：按&分割参数对
                string[] arguments = decoded.Split('&');
                foreach (string pair in arguments)
                {
                    if (string.IsNullOrWhiteSpace(pair))
                    {
                        continue;
                    }

                    //
                    string v = pair;
                    // 1. 处理 \xXX 转义
                    v = System.Text.RegularExpressions.Regex.Replace(v, @"\\x([0-9A-Fa-f]{2})", match =>
                    {
                        string hex = match.Groups[1].Value;
                        return ((char)Convert.ToInt32(hex, 16)).ToString();
                    });

                    // 步骤3：按=分割key和value
                    int equal = v.IndexOf('=');
                    if (equal > 0)
                    {
                        string key = v.Substring(0, equal);
                        string value = v.Substring(equal + 1);

                        list[key] = value;
                    }
                    else
                    {
                        // 处理没有value的参数（如 ?key1&key2=value）
                        list[pair] = "";
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger?.LogError("[InternalService] (URLParseArguments) Parse Error: " + ex.Message, ex);
                return null;
            }
        }
        
        private Dictionary<string, string?>? URLParseMetadata(string? meta)
        {
            try
            {
                meta = (meta ?? "").Trim();
                var body = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(meta,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    IgnoreReadOnlyFields = true,
                                    IncludeFields = true,
                                    // PropertyNameCaseInsensitive = true,    // 启用不区分大小写的属性匹配
                                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,  // 自动跳过注释
                                    // PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, // 不使用驼峰命名
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                                }
                );
                if(body == null)
                {
                    return null;
                }
                return new Dictionary<string, string?>(body, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger?.LogError("[InternalService] (URLParseArguments) Parse Error: " + ex.Message, ex);
                return null;
            } 
        }
        
        /// <summary>
        /// 支付接口
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task HandleOpenAPIPaymentResult(HttpContext context)
        {
            string value = "";
            var arguments = this.URLParseArguments(context.Request.GetEncodedUrl());

            //
            context.QueryString("appid", out value);
            string appid = value.Trim();
            //
            context.QueryString("openid", out value);
            string openid = value.Trim();

            //
            float amount = 0.0f;
            //
            context.QueryString("pay_channel", out value);
            string paychannel = value.Trim().ToLower();
            if (paychannel.ToLower().Contains("wechat"))
            {
                context.QueryString("paychannelsubid", out value);
                string paychannel_subid = value.Trim();

                context.QueryString("zoneid", out value);
                string zoneid = value.Trim();

                context.QueryString("sig", out value);
                string sig = value.Trim();

                context.QueryString("billno", out value);
                string billing_nid = value.Trim();

                context.QueryString("channel_orderid", out value);
                string channel_nid = value.Trim();

                context.QueryString("ts", out value);
                int.TryParse(value, out int timestamp);


                context.QueryString("amt", out value);
                float.TryParse(value.Trim(), out float v);
                amount = v * 0.01f;

                //
                string? meta = "";
                arguments?.TryGetValue("appmeta", out meta);
                string[] meta_v = (meta ?? "").Trim().Split("*");
                if (meta_v.Length > 0)
                {
                    meta = meta_v[0];
                }

                var metadata = URLParseMetadata(meta);

                var datetime = DateTimeOffset.FromUnixTimeSeconds((long)timestamp).DateTime;
                await PaymentManager.Instance.ExtractTransaction_VX100(metadata, new Dictionary<string, string?>()
                {
                    { "name",       "wechat" },
                    { "nid",        channel_nid },
                    { "bnid",       billing_nid },
                    { "timestamp",  $"{timestamp}" }
                });
            }
            else
            {
                await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
                return;
            }

            // // 解析 JSON
            // var request = await context.Request.JsonBodyAsync<NUserWalletDataRequest>();
            // if (request == null)
            // {
            //     await context.ResponseError(HttpStatusCode.BadRequest, ErrorMessage.UNKNOW);
            //     return;
            // }


            // //
            // var result = new NUserWalletDataResponse
            // {
            //     Code = 0,
            // };


            // // 
            // var result_data = await this._GetWalletData(request.UserID, request.CustomID);
            // if (result_data == null) {
            //     result.Code = -1;
            // }
            // else
            // {
            //     //
            //     result.Code = 1;
            //     result.Data = result_data;
            // }

            // //
            // await context.ResponseResult(result);
            
        }
        

    }
}