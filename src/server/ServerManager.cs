
namespace Server
{
    public interface IServerManager
    {
        ServerConfig? Config { get; set; }
        Logger.LoggerEntry? Logger { get; set; }
    }
}