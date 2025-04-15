/// !!! 服务端与客户端文件列不同，需要分开管理，不要混淆
// 地区常用值
//亚洲
//中国 ​CHN
//日本 ​JPN
//韩国 ​KOR
//印度 ​IND
//俄罗斯 ​RUS
//新加坡 ​SGP
//泰国 ​THA
//越南 ​VNM
//​欧洲
//德国 ​DEU
//法国 ​FRA
//英国 ​GBR
//意大利 ​ITA
//西班牙 ​ESP
//瑞士 ​CHE
//荷兰 ​NLD
//瑞典 ​SWE
//​美洲
//美国 ​USA
//加拿大 ​CAN
//巴西 ​BRA
//墨西哥 ​MEX
//阿根廷 ​ARG
//智利 ​CHL
//​非洲 & 大洋洲
//南非 ​ZAF
//埃及 ​EGY
//澳大利亚 ​AUS
//新西兰 ​NZL
//​特殊地区示例
//香港（中国） ​HKG
//澳门（中国） ​MAC
//台湾（中国） ​TWN


using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Game
{

    /// <summary>
    /// AI数据。
    /// </summary>
    [System.Serializable]
    public class TAIPlayers : AMToolkits.Utility.ITableData
    {
        /// <summary>
        /// 获取编号
        /// </summary>
        public int Id
        {
            get;
            protected set;
        }

        /// <summary>
        /// 获取名称
        /// </summary>
        public string Name
        {
            get;
            protected set;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        public string Value
        {
            get;
            protected set;
        }

        /// <summary>
        /// 性别 male/female
        /// </summary>
        public string Gender
        {
            get;
            protected set;
        }

        /// <summary>
        /// 获取级别
        /// </summary>
        public int Level
        {
            get;
            protected set;
        }

        /// <summary>
        /// HOL 匹配系数
        /// 默认100
        /// </summary>
        public int HOLValue
        {
            get;
            protected set;
        }
        /// <summary>
        /// 地区
        /// </summary>
        public string Region
        {
            get;
            protected set;
        }

        /// <summary>
        /// Icon
        /// </summary>
        public string Icon
        {
            get;
            protected set;
        }

        /// <summary>
        /// 物品
        /// </summary>
        public int[] Items
        {
            get;
            protected set;
        }

        /// <summary>
        /// 获取类型
        /// </summary>
        public int Type
        {
            get;
            protected set;
        }

        /// <summary>
        /// 无
        /// </summary>
        public int Status
        {
            get;
            protected set;
        }

        /// <summary>
        /// 此处没有做太多错误修正，目的是为了把表规范化。
        /// 请按规定配表
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool ParseData(string data, string separators = ",")
        {
            string[] columns = data.Split(separators);
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = columns[i].Trim();
            }

            int index = 0;
            index++;

            int id = -1;
            int.TryParse(columns[index++], out id);
            if (id < 0)
            {
                return false;
            }
            Id = id;
            index++;

            if (index < columns.Length) Name = columns[index++].TrimStart('\"').TrimEnd('\"').Trim();
            if (index < columns.Length) Value = columns[index++].TrimStart('\"').TrimEnd('\"').Trim();
            if (index < columns.Length) Gender = columns[index++].TrimStart('\"').TrimEnd('\"').Trim();
            if (index < columns.Length) Level = int.Parse(columns[index++]);
            if (index < columns.Length) HOLValue = int.Parse(columns[index++]);
            if (index < columns.Length) Region = columns[index++].TrimStart('\"').TrimEnd('\"').Trim();
            if (index < columns.Length) Icon = columns[index++].TrimStart('\"').TrimEnd('\"').Trim();
            if (index < columns.Length)
            {
                this.Items = new int[] { };
                string text = columns[index++].TrimStart('\"').TrimEnd('\"').Trim();
                string[] vs = text.Split("|");
                this.Items = vs.Where(s => int.TryParse(s, out _)).Select(int.Parse).ToArray();
            }


            // 仅仅只做可以为null的处理，配表尽可能在前期纠正错误
            if (index < columns.Length)
            {
                string text = columns[index++];
                if(string.IsNullOrEmpty(text))
                {
                    text = "0";
                }
                Type = int.Parse(text);
            }
            if (index < columns.Length)
            {
                string text = columns[index++];
                if (string.IsNullOrEmpty(text))
                {
                    text = "0";
                }
                Status = int.Parse(text);
            }

            //male,female
            if(Gender == "")
            {
                Gender = "male";
            }

            //
            return true;
        }
    }

}

