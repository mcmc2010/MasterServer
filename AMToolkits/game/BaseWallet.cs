namespace AMToolkits.Game
{


    [System.Serializable]
    public class WalletData : IMemoryCachedData
    {
        public float gold;
        public float gems;

        public int integer_gold { get { return (int)System.Math.Round(this.gold); } }
        public int integer_gems { get { return (int)System.Math.Round(this.gems); } }

        public bool HasBalance(string currency, float amount)
        {
            float balance = 0.0f;
            currency = currency.Trim();
            if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS || currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT)
            {
                balance = this.gems;
            }
            else if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD || currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT)
            {
                balance = this.gold;
            }

            if (balance < 0.0f || balance + amount < 0.0f)
            {
                return false;
            }
            return true;
        }

        public float GetBalance(string currency)
        {
            float balance = 0.0f;
            currency = currency.Trim();
            if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS || currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT)
            {
                balance = this.gems;
            }
            else if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD || currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT)
            {
                balance = this.gold;
            }
            return balance;
        }
    }

}