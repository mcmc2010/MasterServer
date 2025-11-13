using System.Security.Cryptography.X509Certificates;

using AMToolkits.Extensions;
using Logger;

namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shop_template_item"></param>
        /// <returns></returns>
        public bool IsPaymentProduct(Game.TShop? shop_template_item)
        {
            if (shop_template_item == null) { return false; }

            // 商城物品隐藏物品，并且支付金额大于0，这个特殊性
            if (shop_template_item.ShopType == (int)AMToolkits.Game.ShopType.CashShop_1 && shop_template_item.Pay > 0.0f)
            {
                return true;
            }

            //
            return shop_template_item.ShopType == (int)AMToolkits.Game.ShopType.Shop_1;
        }

        /// <summary>
        /// 修改订单 - 待审核
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _GetTransactions(string user_uid, List<TransactionItem> transactions,
                                                        string? code = "pending")
        {
            //
            var r_result = await DBGetTransactions(user_uid, transactions, null, null, code);
            if (r_result < 0)
            {
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// 开始支付 - 创建订单
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> StartTransaction_V1(string user_uid, TransactionItem transaction)
        {
            // 商城物品必须有ProductId
            var shop_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
            if (shop_template_data == null)
            {
                return -1;
            }
            // 物品必须是商城物品
            var shop_template_item = shop_template_data.First(v => v.ProductId == transaction.product_id);
            if (shop_template_item == null || !IsPaymentProduct(shop_template_item))
            {
                return -1;
            }

            transaction.name = shop_template_item.Name;

            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null || r_user.ID != transaction.user_id)
            {
                return -2;
            }

            // 0: 如果支付包含特效关联，已经存在就不能再支付
            var effect_list = new List<string>();
            var effects = AMToolkits.Game.ValuesUtils.ParseValues(shop_template_item.EffectValues);
            if (!effects.IsNullOrEmpty())
            {
                effect_list.AddRange(effects);

                // 检测特效是否存在
                if (await GameEffectsManager.Instance._CheckUserEffects(r_user, effect_list) < 0)
                {
                    return -100;
                }
            }

            transaction.custom_id = r_user.CustomID;

            /// 支付方法：需要设置
            transaction.result_code = null;
            if (transaction.payment_method == "none" ||
               (!_settings.Alipay.Enabled && transaction.payment_method?.Contains("alipay") == true))
            {
                transaction.result_code = "none";
            }

            var r_result = await DBCreateTransaction(r_user.ID, transaction);
            if (r_result <= 0)
            {
                return -1;
            }


            _logger?.Log($"{TAGName} (StartTransaction) : {transaction.id} - {transaction.name} " +
                    $"(User:{transaction.user_id}) {transaction.order_id} Amount: {transaction.amount} {transaction.currency} ");

            if (transaction.result_code == "none")
            {
                return -5;
            }
            return 1;
        }

        /// <summary>
        /// 开始支付 - 检测订单
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<string?> CheckTransaction_V1(string user_uid, TransactionItem transaction,
                            string? data)
        {
            if (data.IsNullOrWhiteSpace())
            {
                return null;
            }

            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null || r_user.ID != transaction.user_id)
            {
                return null;
            }

            transaction.custom_id = r_user.CustomID;

            // 对数据签名
            var buffer = data?.Base64UrlDecode();
            if (_certificate == null || buffer == null)
            {
                return "";
            }

            byte[]? sign_data = null;
            if (!AMToolkits.RSA.RSA2SignData(buffer, _certificate.GetRSAPrivateKey(), out sign_data) || sign_data == null)
            {
                return null;
            }

            var sign_b64 = sign_data.Base64UrlEncode();
            return sign_b64;
        }


        /// <summary>
        /// 开始支付 - 完成订单
        /// </summary>
        /// <param name="user_uid"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> FinalTransaction_V1(string user_uid, TransactionItem transaction,
                            string reason = "completed")
        {
            var r_user = UserManager.Instance.GetUserT<UserBase>(user_uid);
            if (r_user == null || r_user.ID != transaction.user_id)
            {
                return -2;
            }

            // 原因必须在指定的选项中
            reason = reason.Trim().ToLower();
            if (!REASONS.Any(v => v == reason))
            {
                return -1;
            }

            transaction.custom_id = r_user.CustomID;

            /// 支付方法：需要设置
            if (transaction.payment_method == "none" ||
               (!_settings.Alipay.Enabled && transaction.payment_method?.Contains("alipay") == true))
            {
                return -5;
            }

            //
            List<TransactionItem> transactions = new List<TransactionItem>();
            var r_result = await DBGetTransactions(r_user.ID, transactions, transaction.id, transaction.order_id, null);
            if (r_result < 0)
            {
                return -1;
            }
            // 订单或流水号不存在
            var ti = transactions.FirstOrDefault();
            if (transactions.Count == 0 || ti == null)
            {
                return 0;
            }
            transaction.Clone(ti);

            // 商城物品必须有ProductId
            var shop_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
            if (shop_template_data == null)
            {
                return -1;
            }
            // 物品必须是商城物品
            var shop_template_item = shop_template_data.First(v => v.ProductId == transaction.product_id);
            if (shop_template_item == null || !IsPaymentProduct(shop_template_item))
            {
                return -1;
            }

            // 获取物品列表
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_template_item.Items);
            if (items.IsNullOrEmpty())
            {
                return -1;
            }
            var item_list = UserManager.Instance.InitGeneralItemData(items);
            if (item_list == null)
            {
                return -1;
            }

            // 获取道具
            if (shop_template_item.ShopType == (int)AMToolkits.Game.ShopType.Shop_1)
            {
                // 只处理第一个配置物品
                var item = item_list.FirstOrDefault();
                if (item?.ID == AMToolkits.Game.ItemConstants.ID_GM)
                {
                    transaction.virtual_currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GEMS_SHORT;
                    transaction.virtual_amount = item.Count;
                }
                // 不支持 R 兑换游戏币（金币）
                else if (item?.ID == AMToolkits.Game.ItemConstants.ID_GD)
                {
                    //transaction.virtual_currency = AMToolkits.Game.CurrencyUtils.CURRENCY_GOLD_SHORT;
                    //transaction.virtual_amount = item.Count;
                }
                else
                {
                    // 其它物品不处理
                }
            }

            //
            var r_result_payment = await PlayFabService.Instance.PFPaymentFinal(transaction.user_id, transaction.custom_id, transaction.id,
                            transaction, "payment");
            if (r_result_payment != null && r_result_payment.Data != null)
            {
                transaction.virtual_amount = r_result_payment.Data.CurrentAmount ?? transaction.virtual_amount;
                transaction.virtual_currency = r_result_payment.Data.CurrentVirtualCurrency;
            }

            r_result = await DBFinalTransaction(r_user.ID, transaction, reason);
            if (r_result <= 0)
            {
                return -1;
            }

            //
            _logger?.Log($"{TAGName} (FinalTransaction) : {transaction.id} - {transaction.name} " +
                    $"(User:{transaction.user_id}) {transaction.order_id} Amount: {transaction.amount} {transaction.currency} ");
            return 1;
        }


        /// <summary>
        /// 修改订单 - 待审核
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> _PendingTransaction(string user_uid, TransactionItem transaction)
        {
            //
            List<TransactionItem> transactions = new List<TransactionItem>();
            var r_result = await DBGetTransactions(user_uid, transactions, transaction.id, transaction.order_id, "completed");
            if (r_result < 0)
            {
                return -1;
            }

            // 订单或流水号不存在
            var ti = transactions.FirstOrDefault();
            if (transactions.Count == 0 || ti == null)
            {
                return 0;
            }
            transaction.Clone(ti);

            r_result = await DBReviewTransaction(user_uid, transaction, "pending");
            if (r_result <= 0)
            {
                return -1;
            }

            transaction.ReviewTime = DateTime.UtcNow;
            transaction.ReviewCount = 0;

            lock (_transactions_queue_locked)
            {
                _transactions_queue.Add(transaction);
            }

            //
            _logger?.Log($"{TAGName} (PendingTransaction) : ({user_uid}) {transaction.id} - {transaction.order_id} ");
            return 1;
        }
    }
}