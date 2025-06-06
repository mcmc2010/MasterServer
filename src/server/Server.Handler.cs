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
            
            float delay = remote_timestamp == 0 ? Utils.NETWORK_DELAY_MIN : Utils.DiffTimestamp(remote_timestamp, server_timestamp);


            await context.ResponseStatusAsync("success", "", server_timestamp, delay);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual async Task HandleServiceStatus(HttpContext context)
        {
            //
            var os_version = System.Environment.OSVersion.Version;

            // 获取工作线程和I/O线程的使用情况
            ThreadPool.GetMaxThreads(out int max_worker_threads, out int max_io_threads);
            ThreadPool.GetAvailableThreads(out int available_worker_threads, out int available_io_threads);

            var used_worker_threads = max_worker_threads - available_worker_threads;
            var used_io_threads = max_io_threads - available_io_threads;

            await context.ResponseResult(new {
                os_version = os_version,
                used_worker_threads = used_worker_threads,
                used_io_threads = used_io_threads,
                max_worker_threads = max_io_threads,
                max_io_threads = max_io_threads
            });
        }
    }
}