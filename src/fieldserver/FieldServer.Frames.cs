using System.Collections.Concurrent;
using Logger;


namespace Server.Field
{
    /// <summary>
    /// 帧管理器 - 统一管理所有房间的帧循环
    /// 参考 Nakama 的帧同步机制
    /// </summary>
    public partial class FieldServer
    {
        /// <summary>
        /// 帧率（次/秒）
        /// </summary>
        public static int FrameRate { get; set; } = 30;
        
        /// <summary>
        /// 帧间隔（毫秒）
        /// </summary>
        private long _frame_interval = 33;
        
        /// <summary>
        /// 当前全局帧ID
        /// </summary>
        private int _global_frame_id = 0;
        
        /// <summary>
        /// 上次 Tick 时间
        /// </summary>
        private long _last_tick_time = 0;
        
        /// <summary>
        /// 帧循环是否正在运行
        /// </summary>
        private bool _is_frame_running = false;
        
        /// <summary>
        /// 帧循环任务
        /// </summary>
        private System.Threading.Tasks.Task? _frame_task = null;
        
        /// <summary>
        /// 初始化帧管理器
        /// </summary>
        public void InitializeFrameManager()
        {
            // 计算帧间隔
            _frame_interval = (long)(1000.0f / FrameRate);
            _global_frame_id = 0;
            _last_tick_time = 0;
            _is_frame_running = false;
            
            _logger?.Log($"[FieldServer] Frame manager initialized with {FrameRate} FPS");
        }
        
        /// <summary>
        /// 启动帧循环
        /// </summary>
        public void StartFrameLoop()
        {
            if (_is_frame_running)
            {
                return;
            }
            
            _is_frame_running = true;
            _last_tick_time = AMToolkits.Utils.GetLongTimestamp();
            _global_frame_id = 0;
            
            _logger?.Log($"[FieldServer] Frame loop started with {FrameRate} FPS");
            
            // 启动帧循环任务
            _frame_task = System.Threading.Tasks.Task.Run(async () =>
            {
                while (_is_frame_running && !ServerApplication.Instance.HasQuiting)
                {
                    var currentTime = AMToolkits.Utils.GetLongTimestamp();
                    var elapsed = currentTime - _last_tick_time;
                    
                    // 检查是否到达下一 Tick
                    if (elapsed >= _frame_interval)
                    {
                        // 处理当前帧
                        await ProcessGlobalFrame(_global_frame_id);
                        
                        // 更新帧ID和时间
                        _global_frame_id++;
                        _last_tick_time = currentTime;
                    }
                    
                    // 短暂休眠，避免 CPU 占用过高
                    await System.Threading.Tasks.Task.Delay(1);
                }
                
                _logger?.Log("[FieldServer] Frame loop stopped");
            });
        }
        
        /// <summary>
        /// 停止帧循环
        /// </summary>
        public void StopFrameLoop()
        {
            _is_frame_running = false;
            
            if (_frame_task != null)
            {
                try
                {
                    _frame_task.Wait(1000);
                }
                catch
                {
                    // 忽略等待异常
                }
                _frame_task = null;
            }
            
            _logger?.Log("[FieldServer] Frame loop stopped");
        }
        
        /// <summary>
        /// 处理全局帧
        /// </summary>
        /// <param name="frameId">帧ID</param>
        private async System.Threading.Tasks.Task ProcessGlobalFrame(int frameId)
        {
            try
            {
                // 通知所有运行中的房间处理帧
                if (_room_manager != null)
                {
                    await _room_manager.ProcessAllRoomsFrame(frameId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[FieldServer] Global frame {frameId} processing error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取当前全局帧ID
        /// </summary>
        public int GlobalFrameId => _global_frame_id;
        
        /// <summary>
        /// 帧循环是否正在运行
        /// </summary>
        public bool IsFrameRunning => _is_frame_running;
    }
}