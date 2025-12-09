
using System.Reflection.Metadata;
using AMToolkits.Extensions;
using AMToolkits.Game;
using Logger;

namespace Server
{
    public partial class UserManager
    {

        #region Server Internal

        public async Task<AMToolkits.Game.WalletData?> _GetWalletData(string user_uid)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            var user = this.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return null;
            }

            //
            var wallet = AMToolkits.MemoryCached.Get<WalletData>("wallet", user_uid);
            if(wallet != null)
            {
                return wallet;
            }

            return await this._GetWalletData(user_uid, user.CustomID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        protected async Task<AMToolkits.Game.WalletData?> _GetWalletData(string user_uid, string custom_uid)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }
            if (custom_uid == null || custom_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFGetWalletData(user_uid, custom_uid);
            if (result == null)
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }

            AMToolkits.Game.WalletData wallet = new AMToolkits.Game.WalletData();
            // 
            if (!result.TryGetValue(AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS, out var gems))
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }
            wallet.gems = System.Convert.ToSingle(gems);
            if (!result.TryGetValue(AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD, out var gold))
            {
                _logger?.LogError($"{TAGName} (GetWalletData) (User:{user_uid}) Failed");
                return null;
            }
            wallet.gold = System.Convert.ToSingle(gold);

            this._UpdateWalletData(user_uid, wallet);
            return wallet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="pairs"></param>
        internal void _UpdateWalletData(string user_uid, WalletData wallet)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return;
            }
            
            AMToolkits.MemoryCached.Update("wallet", user_uid, wallet);
        }
        
        internal void _UpdateWalletData(string user_uid, Dictionary<string, object?> pairs)
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return;
            }

            var (balance, currency) = this._GetVirtualCurrencyBalance(pairs);

            //
            var wallet = AMToolkits.MemoryCached.Get<WalletData>("wallet", user_uid, false);
            if (wallet == null)
            {
                wallet = new WalletData()
                {

                };
            }

            if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT || currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD)
            {
                wallet.gold = balance;
            }
            else if (currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT || currency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS)
            {
                wallet.gems = balance;
            }

            this._UpdateWalletData(user_uid, wallet);
        }
        
        internal void _UpdateWalletData(string user_uid, PFNCashShopItemData? data)
        {
            if (data == null)
            {
                return;
            }
            
                        
            //
            var wallet = AMToolkits.MemoryCached.Get<WalletData>("wallet", user_uid, false);
            if (wallet == null)
            {
                wallet = new WalletData()
                {

                };
            }

            if (data.CurrentVirtualCurrency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT || data.CurrentVirtualCurrency == AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD)
            {
                wallet.gold = data.CurrentBalance ?? wallet.gold;
            }
            else if (data.CurrentVirtualCurrency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT || data.CurrentVirtualCurrency == AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS)
            {
                wallet.gems = data.CurrentBalance ?? wallet.gems;
            }

            this._UpdateWalletData(user_uid, wallet);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public (float, string) _GetVirtualCurrencyBalance(Dictionary<string, object?> pairs)
        {
            // 默认为int32，此处用浮点表示
            float balance = System.Convert.ToSingle(pairs.Get("balance") ?? 0.0f);
            string currency = System.Convert.ToString(pairs.Get("currency")) ?? AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;
            return (balance, currency);
        }

        /// 物品更新
        /// </summary>
        public async Task<Dictionary<string, object?>?> _UpdateVirtualCurrency(string user_uid,
                            float amount = 0.0f,
                            AMToolkits.Game.VirtualCurrency currency = AMToolkits.Game.VirtualCurrency.GD,
                            string reason = "")
        {
            if (user_uid == null || user_uid.IsNullOrWhiteSpace())
            {
                return null;
            }

            var user = this.GetUserT<UserBase>(user_uid);
            if (user == null)
            {
                return null;
            }

            // 首先要更新PlayFab 服务
            var result = await PlayFabService.Instance.PFUpdateVirtualCurrency(user_uid, user.CustomID, amount, currency, reason);
            if (result == null)
            {
                _logger?.LogError($"{TAGName} (UpdateVirtualCurrency) (User:{user_uid}) Amount: {amount} {currency} Failed");
                return null;
            }

            // 更新成功，更新本地数据库
            // 暂时不处理
            _UpdateWalletData(user_uid, result);
            return result;
        }

        public async Task<int> _UpdateVirtualCurrency(string user_uid,
                            List<AMToolkits.Game.GeneralItemData> list,
                            Dictionary<string, object?> result,
                            string reason = "")
        {
            result.Set(AMToolkits.ServiceConstants.KEY_RESULT, AMToolkits.ServiceConstants.VALUE_ERROR);

            
            Dictionary<string, object?>? result_vc = null;

            bool is_all_vc = true;
            foreach (var vc in list)
            {
                if (vc.ID == AMToolkits.Game.ItemConstants.ID_GD && vc.Count != 0 &&
                    (result_vc = await UserManager.Instance._UpdateVirtualCurrency(user_uid, vc.Count, AMToolkits.Game.VirtualCurrency.GD)) != null)
                {
                    var (balance, currency) = this._GetVirtualCurrencyBalance(result_vc);
                    result.Set(AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD, balance);
                }
                else if (vc.ID == AMToolkits.Game.ItemConstants.ID_GM && vc.Count != 0 &&
                    (result_vc = await UserManager.Instance._UpdateVirtualCurrency(user_uid, vc.Count, AMToolkits.Game.VirtualCurrency.GM)) != null)
                {
                    var (balance, currency) = this._GetVirtualCurrencyBalance(result_vc);
                    result.Set(AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS, balance);
                }
                else
                {
                    is_all_vc = false;
                }

            }

            if (!is_all_vc || result_vc == null)
            {
                return -1;
            }

            result.Set(AMToolkits.ServiceConstants.KEY_RESULT, AMToolkits.ServiceConstants.VALUE_SUCCESS);            
            return 1;
        }
        

        public async Task<Dictionary<string, object?>?> _CheckVirtualCurrency(string user_uid,
                            float amount = 0.0f,
                            AMToolkits.Game.VirtualCurrency currency = AMToolkits.Game.VirtualCurrency.GD)
        {
            var wallet = await _GetWalletData(user_uid);
            if (wallet == null)
            {
                return null;
            }

            Dictionary<string, object?> result = new Dictionary<string, object?>(){
                { AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS, wallet.gems },
                { AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD, wallet.gold }
            };
            if (currency == AMToolkits.Game.VirtualCurrency.GD && wallet.gold - amount < 0.01f)
            {
                result.Set(AMToolkits.ServiceConstants.KEY_RESULT, AMToolkits.ServiceConstants.VALUE_ERROR);
                result.Set(AMToolkits.ServiceConstants.KEY_ERROR, AMToolkits.ServiceConstants.VALUE_INSUFFICIENT);
                return result;
            }
            else if (currency == AMToolkits.Game.VirtualCurrency.GM && wallet.gems - amount < 0.01f)
            {
                result.Set(AMToolkits.ServiceConstants.KEY_RESULT, AMToolkits.ServiceConstants.VALUE_ERROR);
                result.Set(AMToolkits.ServiceConstants.KEY_ERROR, AMToolkits.ServiceConstants.VALUE_INSUFFICIENT);
                return result;
            }
            
            //
            result.Set(AMToolkits.ServiceConstants.KEY_RESULT, AMToolkits.ServiceConstants.VALUE_SUCCESS);
            return result;
        }
        #endregion
    }
}