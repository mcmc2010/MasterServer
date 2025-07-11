namespace AMToolkits.Game
{
    /// <summary>
    /// 
    /// </summary>
    public enum Currency
    {
        RMB = 0, //人民币
        CNY = 156,
        USD = 840, //美元
        HKD = 344, //港币
    }

    public enum VirtualCurrency
    {
        GD = 0,
        GM = 1,
        COIN = 0,
        GEMS = 1,
    }

    /// <summary>
    /// 
    /// </summary>
    public class CurrencyUtils
    {
        /// <summary>
        /// 
        /// </summary>
        public const string CURRENCY_NONE = "NONE";
        public const string CURRENCY_GOLD = "GOLD";
        public const string CURRENCY_GEMS = "GEMS";
        public const string CURRENCY_GOLD_SHORT = "GD";
        public const string CURRENCY_GEMS_SHORT = "GM";

        public const uint COLOR_CURRENCY = 0xF08080FF;
        public const uint COLOR_CURRENCY_GOLD = 0xFFF178FF;
        public const uint COLOR_CURRENCY_GEMS = 0xF120F1FF;
    }
}