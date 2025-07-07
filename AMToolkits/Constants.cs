
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
#define USE_UNITY_BUILD
#endif

#if !USE_UNITY_BUILD
using System.Net.NetworkInformation;
#endif

namespace AMToolkits
{
    #region OSDevice
    public static class OSPlatform
    {
        public const string MacOS = "MacOS";
        public const string Window = "Window";
        public const string Linux = "Linux";
        public const string iOS = "iOS";
        public const string Android = "Android";
        public const string Web = "Web";
    }

    public static class OSArchitecture
    {
        public const string X86 = "x86";
        public const string X64 = "x64";
        public const string ARMv7 = "arm32"; // AArch32
        public const string ARMv8 = "arm64"; // AArch64
    }

    #endregion

    #region Networks
    /// <summary>
    /// 
    /// </summary>
    public static class NetworkStatus
    {

        public const float NETWORK_DELAY_MAX = 460 * 0.001f; // 最高延迟460ms
        public const float NETWORK_DELAY_MIN = 10 * 0.001f;  // 最快延迟10ms

#if !USE_UNITY_BUILD
        public static string GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 只选择已启用且非虚拟的网卡（如以太网、Wi-Fi）
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    !nic.Description.Contains("Virtual"))
                {
                    return BitConverter.ToString(nic.GetPhysicalAddress().GetAddressBytes());
                }
            }
            return "";
        }
#endif

    }
    #endregion
}