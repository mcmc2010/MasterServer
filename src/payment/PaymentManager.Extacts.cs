
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
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> ExtractTransaction_V0(string user_uid, TransactionItem transaction,
                        string reason = "review")
        {
            // 商城物品必须有ProductId
            var shop_template_data = AMToolkits.Utility.TableDataManager.GetTableData<Game.TShop>();
            if (shop_template_data == null)
            {
                return -1;
            }
            // 物品必须是商城物品
            var shop_template_item = shop_template_data.First(v => v.ProductId == transaction.product_id);
            if (shop_template_item == null)
            {
                return -1;
            }

            if (shop_template_item.ShopType == (int)AMToolkits.Game.ShopType.Shop_1)
            {
                return await ExtractTransaction_V1(user_uid, shop_template_item, transaction, reason);
            }
            else if (shop_template_item.ShopType == (int)AMToolkits.Game.ShopType.CashShop_1)
            {
                return await ExtractTransaction_V2(user_uid, shop_template_item, transaction, reason);
            }
            else
            {
                //return 0;
            }
            return 0;
        }
        
        /// <summary>
        /// 普通 ：仅仅包括钻石
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> ExtractTransaction_V1(string user_uid,
                                Game.TShop shop_template_item,
                                TransactionItem transaction,
                                string reason = "review")
        {
            // 物品必须是可以解绑货币
            if (transaction.virtual_amount == 0.0f)
            {
                return 0;
            }

            transaction.result_code = reason;

            var r_result = await PlayFabService.Instance.PFPaymentFinal(transaction.user_id, transaction.custom_id, transaction.id,
                            transaction, "payment");
            if (r_result == null)
            {
                _logger?.LogError($"{TAGName} (ExtractTransaction_V1) : ({user_uid}) {transaction.order_id} " +
                                $"Amount : {transaction.amount} {transaction.currency}, " +
                                $"Update : {transaction.virtual_amount} {transaction.virtual_currency} Failed");
                return -1;
            }

            transaction.virtual_amount = r_result.Data?.CurrentAmount ?? transaction.virtual_amount;
            transaction.virtual_currency = r_result.Data?.CurrentVirtualCurrency ?? transaction.virtual_currency;

            await UserManager.Instance._UpdateUserVIPData(user_uid, transaction.amount, transaction.currency);

            _logger?.Log($"{TAGName} (ExtractTransaction_V1) : ({user_uid}) {transaction.order_id} " +
                                $"Amount : {transaction.amount} {transaction.currency}, " +
                                $"Update : {transaction.virtual_amount} {transaction.virtual_currency} Success");
            return 1;
        }

        /// <summary>
        /// 物品 ：
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> ExtractTransaction_V2(string user_uid,
                                Game.TShop shop_template_item,
                                TransactionItem transaction,
                                string reason = "review")
        {
            // 物品必须是可以解绑货币
            if (shop_template_item.Items.IsNullOrWhiteSpace())
            {
                return 0;
            }

            transaction.result_code = reason;

            var r_result = await PlayFabService.Instance.PFPaymentFinal(transaction.user_id, transaction.custom_id, transaction.id,
                            transaction, "payment");
            if (r_result == null)
            {
                _logger?.LogError($"{TAGName} (ExtractTransaction_V2) : ({user_uid}) {transaction.order_id} " +
                                $"Amount : {transaction.amount} {transaction.currency}, " +
                                $"Update : {transaction.virtual_amount} {transaction.virtual_currency} Failed");
                return -1;
            }

            transaction.virtual_amount = r_result.Data?.CurrentAmount ?? transaction.virtual_amount;
            transaction.virtual_currency = r_result.Data?.CurrentVirtualCurrency ?? transaction.virtual_currency;

            await UserManager.Instance._UpdateUserVIPData(user_uid, transaction.amount, transaction.currency);

            // 0:
            // 查看是否有特效
            var effect_list = new List<string>();
            var effects = AMToolkits.Game.ValuesUtils.ParseValues(shop_template_item.EffectValues);
            if (!effects.IsNullOrEmpty())
            {
                effect_list.AddRange(effects);

            }

            // 1 :检测物品
            var item_list = new List<AMToolkits.Game.GeneralItemData>();
            var items = AMToolkits.Game.ItemUtils.ParseGeneralItem(shop_template_item.Items);
            if (!items.IsNullOrEmpty())
            {
                var list = UserManager.Instance.InitGeneralItemData(items);
                if (list == null)
                {
                    return -1;
                }
                item_list.AddRange(list);

                // 需要对齐
                int index = 1000;
                foreach (var v in item_list)
                {
                    v.NID = ++index;
                }
            }

            var result_product = await PlayFabService.Instance.PFPaymentFinalProducts(transaction.user_id, transaction.custom_id, transaction.id,
                                        transaction,
                                        effect_list, item_list,
                                        "payment");
            if (result_product == null)
            {
                _logger?.Log($"{TAGName} (ExtractTransaction_V2) : ({user_uid}) {transaction.order_id} " +
                                $"Amount : {transaction.amount} {transaction.currency}, " +
                                $"Update : Effects ({effect_list.Count}), Items ({item_list.Count}) Failed");
                return -1;
            }

            string print_effects = "";
            string print_items = "";
            if (result_product?.Data?.EffectList != null)
            {
                print_effects = string.Join(";", result_product.Data.EffectList);
                if (await GameEffectsManager.Instance._AddUserEffects(user_uid, result_product.Data.EffectList) < 0)
                {
                    _logger?.LogWarning($"{TAGName} (ExtractTransaction_V2) (User:{user_uid}) {transaction.order_id} - {shop_template_item.Name} " +
                                        $" Effects:{print_effects} Failed");
                }
            }

            print_items = string.Join(";", result_product?.Data?.ItemList?.Select(v => $"{v.IID} - {v.ID}({v.Count})") ?? new List<string>() { });
            // 添加数据库记录
            if (await UserManager.Instance._CashshopBuyProduct(user_uid, transaction.custom_id, result_product?.Data) <= 0)
            {
                _logger?.LogWarning($"{TAGName} (ExtractTransaction_V2) (User:{user_uid}) {transaction.order_id} - {shop_template_item.Name} " +
                                    $" {print_items} Failed");
                //return result;
            }

            //
            _logger?.Log($"{TAGName} (ExtractTransaction_V2) : ({user_uid}) {transaction.order_id} " +
                                $"Amount : {transaction.amount} {transaction.currency}, " +
                                $"Update : {transaction.virtual_amount} {transaction.virtual_currency}, " +
                                $"Effects: {print_effects}, Items: {print_items} Success");
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> ExtractTransaction_VX100(Dictionary<string, string?>? order_data,
                        Dictionary<string, string?> custom_data,
                        string reason = "review")
        {
            // 物品必须是可以解绑货币
            if (order_data == null)
            {
                return -1;
            }

            string? nid = "";
            string? order_id = "";
            string? product_id = "";
            order_data.TryGetValue("id", out nid);
            order_data.TryGetValue("order_id", out order_id);
            order_data.TryGetValue("product_id", out product_id);

            nid = nid?.Trim();
            order_id = order_id?.Trim();
            product_id = product_id?.Trim();
            if (string.IsNullOrWhiteSpace(nid) || string.IsNullOrWhiteSpace(order_id) || string.IsNullOrWhiteSpace(product_id))
            {
                return -1;
            }
#if USING_REDIS
            var transaction = AMToolkits.Redis.RedisManager.Instance.GetKeyValueT<TransactionItem>(KEY_TRANSACTIONS, nid);
            if(transaction == null)
            {
                return -2;
            }
#endif
            //
            List<TransactionItem> transactions = new List<TransactionItem>();
            var r_result = await DBGetTransactions(transaction.user_id, transactions, transaction.id, transaction.order_id, "completed");
            if (r_result < 0)
            {
                return -1;
            }

            var item = transactions.FirstOrDefault();
            if (item?.order_id != order_id || item?.id != nid || item?.product_id != product_id)
            {
                _logger?.Log($"{TAGName} (ExtractTransaction_VX100) : ({transaction.user_id}) {item?.order_id} - {order_id} Error order data [Error]");
                return -5;
            }
            transaction = item;

            // 
            if ((r_result = await ExtractTransaction_V0(transaction.user_id, transaction, "review")) < 0)
            {
                await DBReviewTransaction(transaction.user_id, transaction, "review", "pending");
            }
            else
            {
                await DBReviewTransaction(transaction.user_id, transaction, "review");
            }

            //从队列中释放
            lock (_transactions_queue_locked)
            {
                _transactions_queue.RemoveAll(v => v.id == transaction.id);
            }

#if USING_REDIS
            AMToolkits.Redis.RedisManager.Instance.DeleteKeyValue(KEY_TRANSACTIONS, nid);

#endif
            _logger?.Log($"{TAGName} (ExtractTransaction_VX100) : ({transaction.user_id}) {transaction.order_id} ({r_result}) Final [Review]");
            return 0;
        }
    }
}