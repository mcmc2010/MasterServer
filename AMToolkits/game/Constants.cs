namespace AMToolkits.Game
{
    /// <summary>
    /// 物品类型
    /// </summary>
    public enum ItemType
    {
        None = -1,
        Default = 0,
        Economy = 1,    // 经济
        Item = 5,       // 物品道具
        Equipment = 10, // 装备
        Item_1 = 1000,  // 物品道具
    }

    public enum ItemSubType
    {
        Default = 0,    // 每次消耗物品
        RemainingTime = 10, // 时效物品，需要增加有效期
        RemainingUses = 20, // 使用次数

    }


    public enum GameGroupType
    {
        None = 0,
        Daily = 1, //每日
        OnlyOne = 100, //该组只能唯一性一项
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ItemConstants
    {
        //
        public const int ID_NONE = 0;

        // 可转换为虚拟货币的特殊物品
        public const int ID_GD = 1001;
        public const int ID_GM = 1002;
        // 自定义
        public const int ID_GN = 1003;
        // 自定义不为道具，类似可转换为虚拟货币的特殊物品
        public const int ID_N0 = 1000;
        public const int ID_N1 = 1005;
        public const int ID_N2 = 1006;
        public const int ID_N3 = 1007;
        public const int ID_N4 = 1008;
        public const int ID_NN = 1009;
    }
}