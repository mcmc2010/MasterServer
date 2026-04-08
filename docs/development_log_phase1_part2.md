# 开发日志 - Phase 1 Part 2

## 日期：2026年4月8日

## 任务：完成 RoomManager 帧同步机制

### 问题描述
在实现基于 Nakama 帧同步机制的 RoomManager 时，遇到以下编译错误：
1. `Protocols.Field` 命名空间不存在
2. `FieldServer.GetPlayerService()` 方法不存在

### 解决方案

#### 1. 创建 Field 协议定义文件
创建了 `data/protocols/field.proto` 文件，定义了以下消息类型：
- `PlayerInputData` - 玩家输入数据
- `FrameUpdateMessage` - 帧更新消息
- `GameStartMessage` - 游戏开始消息
- `GameEndMessage` - 游戏结束消息
- `PlayerInputRequest` - 玩家输入请求
- `PlayerInputResponse` - 玩家输入响应

#### 2. 添加 PacketHandleIndex 枚举
在 `src/server/packets/PacketHandlers.cs` 中添加了 Field 相关的消息索引：
- `FrameUpdate = 0x3000` - 帧更新消息
- `GameStart = 0x3001` - 游戏开始消息
- `GameEnd = 0x3002` - 游戏结束消息
- `PlayerInputRequest = 0x3003` - 玩家输入请求
- `PlayerInputResponse = 0x3004` - 玩家输入响应

#### 3. 添加 GetPlayerService 方法
在 `src/fieldserver/FieldServer.Players.cs` 中添加了 `GetPlayerService(string userId)` 方法，用于根据用户ID获取对应的 FieldService 连接。

#### 4. 修复命名空间引用
修改了 `src/fieldserver/RoomManager.Room.cs` 文件，确保使用 `Protocols.Field` 命名空间。

**重要说明**：根据项目架构设计，Field 服务应该使用独立的 `Protocols.Field` 命名空间，而不是 `Protocols.World.Field`。这是因为：
- Master 服务包含的 World 只是一个基本服务
- 如果 World 基数变大，可能需要将 Field 服务分离出去
- 保持命名空间独立有利于未来的架构扩展

### 技术细节

#### 帧同步机制
基于 Nakama 的帧同步设计，实现了以下核心功能：
- **帧循环**：以固定 Tick 率（默认 30 次/秒）运行游戏逻辑
- **输入收集**：收集每个玩家在当前帧的输入
- **输入验证**：验证玩家身份和输入有效性
- **状态更新**：根据输入更新游戏状态
- **帧广播**：将帧数据广播给房间内所有玩家

#### 房间生命周期
- **等待状态**：房间等待玩家加入
- **准备状态**：玩家数量足够，准备开始游戏
- **运行状态**：游戏进行中，帧循环运行
- **暂停状态**：游戏暂停
- **结束状态**：游戏结束

#### 玩家管理
- 玩家加入/离开房间
- 心跳检测，自动移除超时玩家
- 输入缓冲区管理

### 文件变更

#### 新增文件
- `data/protocols/field.proto` - Field 协议定义

#### 修改文件
- `src/server/packets/PacketHandlers.cs` - 添加 Field 消息索引
- `src/fieldserver/FieldServer.Players.cs` - 添加 GetPlayerService 方法
- `src/fieldserver/RoomManager.Room.cs` - 修复命名空间引用

### 编译状态
✅ 编译成功，无错误

### 命名空间设计说明
根据项目架构设计，Field 服务使用独立的 `Protocols.Field` 命名空间，而不是 `Protocols.World.Field`。这是因为：
- Master 服务包含的 World 只是一个基本服务
- 如果 World 基数变大，可能需要将 Field 服务分离出去
- 保持命名空间独立有利于未来的架构扩展

### 架构调整：统一帧管理

#### 问题
原实现中每个房间独立管理帧循环，导致：
- 资源浪费（多个定时器）
- 难以统一控制
- 难以实现全局帧同步

#### 解决方案
将帧循环从房间移到 Field 服务统一管理：

1. **FieldServer.Frames.cs**：
   - 实现统一的帧管理器
   - 以固定频率（30fps）运行
   - 管理所有运行中的房间

2. **Room 类**：
   - 移除独立的帧循环（StartFrameLoop/StopFrameLoop）
   - 添加 `ProcessFrame(int frameId)` 方法供外部调用
   - 房间只负责处理游戏逻辑

3. **RoomManager**：
   - 添加 `ProcessAllRoomsFrame(int frameId)` 方法
   - 统一驱动所有运行中的房间处理帧

4. **FieldServer**：
   - 在 StartWorking 中初始化帧管理器并启动帧循环
   - 在退出时停止帧循环

#### 架构流程
```
Field 服务（统一帧循环）
    ↓ 每秒30次
RoomManager.ProcessAllRoomsFrame()
    ↓ 并行处理
Room.ProcessFrame() × N个房间
```

### 下一步工作
1. 实现 Field 服务器的消息处理逻辑
2. 集成客户端输入处理
3. 添加游戏状态同步机制
4. 实现断线重连功能
5. 优化网络性能和延迟

### 参考资料
- Nakama 官方文档：https://heroiclabs.com/docs/
- Nakama 源码：https://github.com/heroiclabs/nakama
- Godot 帧同步教程：https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html