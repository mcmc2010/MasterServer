using System;

namespace Game
{
    [Serializable]
    public class TAIPlayers : AMToolkits.Utility.ITableData
    {
        /// <summary> 编号 </summary>
        public int Id { get; protected set; }

        /// <summary> 组 </summary>
        public string Name { get; protected set; }

        /// <summary> (可选) </summary>
        public string Value { get; protected set; }

        /// <summary> 性别 </summary>
        public string Gender { get; protected set; }

        /// <summary> 等级 </summary>
        public int Level { get; protected set; }

        public int Star { get; protected set; }

        public int Score { get; protected set; }

        /// <summary> 1-99999 默认100 </summary>
        public int HOLValue { get; protected set; }

        /// <summary> 国家地区 </summary>
        public string Region { get; protected set; }

        /// <summary> 头像 </summary>
        public string Icon { get; protected set; }

        /// <summary> 物品ID;可以用|分隔多个物品 </summary>
        public string Items { get; protected set; }

        /// <summary> 速度 </summary>
        public float Speed { get; protected set; }

        /// <summary> 瞄准准确度 (0-1, 1为完全准确) </summary>
        public float AimAccuracy { get; protected set; }

        /// <summary> 力度控制精度 (0-1, 1为完全准确) </summary>
        public float PowerControl { get; protected set; }

        /// <summary> 失误概率 (0-1, 0无失误，1失误频繁) </summary>
        public float ErrorChance { get; protected set; }

        /// <summary> 思考时间系数 (秒) </summary>
        public float ThinkingTime { get; protected set; }

        /// <summary> 最大误差度数>0 </summary>
        public float MaxErrorAngle { get; protected set; }

        /// <summary> 直接球概率 (0-1) </summary>
        public float DirectShots { get; protected set; }

        /// <summary> 反弹球概率 (0-1) </summary>
        public float BankShots { get; protected set; }

        /// <summary> 组合球概率 (0-1) </summary>
        public float CombinationShots { get; protected set; }

        /// <summary> 防守概率 (0-1, 0不考虑防守，1积极防守) </summary>
        public float Defensive { get; protected set; }

        public int Type { get; protected set; }

        public int Status { get; protected set; }

        public virtual bool ParseData(string data, string separators = "\t")
        {
            if (string.IsNullOrWhiteSpace(data) || data.TrimStart().StartsWith("#"))
                return false;

            string[] columns = data.Split(separators);
            for (int i = 0; i < columns.Length; i++)
                columns[i] = columns[i].Trim();
            int index = 0;

            // 跳过第1列（#）
            index++;
            // 读取Id（第2列）
            int id = 0;
            int.TryParse(index < columns.Length ? columns[index++] : "0", out id);
            Id = id;
            // 跳过第3列（策划备注）
            index++;
            if (index < columns.Length) Name = columns[index++].Trim('"');
            if (index < columns.Length) Value = columns[index++].Trim('"');
            if (index < columns.Length) Gender = columns[index++].Trim('"');
            int level = 0;
            int.TryParse(index < columns.Length ? columns[index++] : "0", out level);
            Level = level;
            int star = 0;
            int.TryParse(index < columns.Length ? columns[index++] : "0", out star);
            Star = star;
            int score = 0;
            int.TryParse(index < columns.Length ? columns[index++] : "0", out score);
            Score = score;
            int holvalue = 0;
            int.TryParse(index < columns.Length ? columns[index++] : "0", out holvalue);
            HOLValue = holvalue;
            if (index < columns.Length) Region = columns[index++].Trim('"');
            if (index < columns.Length) Icon = columns[index++].Trim('"');
            if (index < columns.Length) Items = columns[index++].Trim('"');
            float speed = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out speed);
            Speed = speed;
            float aimaccuracy = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out aimaccuracy);
            AimAccuracy = aimaccuracy;
            float powercontrol = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out powercontrol);
            PowerControl = powercontrol;
            float errorchance = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out errorchance);
            ErrorChance = errorchance;
            float thinkingtime = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out thinkingtime);
            ThinkingTime = thinkingtime;
            float maxerrorangle = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out maxerrorangle);
            MaxErrorAngle = maxerrorangle;
            float directshots = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out directshots);
            DirectShots = directshots;
            float bankshots = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out bankshots);
            BankShots = bankshots;
            float combinationshots = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out combinationshots);
            CombinationShots = combinationshots;
            float defensive = 0;
            float.TryParse(index < columns.Length ? columns[index++] : "0", out defensive);
            Defensive = defensive;
            int type = 0;
            int.TryParse(index < columns.Length ? columns[index++] : "0", out type);
            Type = type;
            int status = 0;
            int.TryParse(index < columns.Length ? columns[index++] : "0", out status);
            Status = status;
            return true;
        }
        // 可补充ParseData等方法
    }
}
