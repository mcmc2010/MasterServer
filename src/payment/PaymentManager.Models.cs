
using Logger;
using AMToolkits.Extensions;


namespace Server
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PaymentManager
    {
        /// <summary>
        /// 统计消费
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="name"></param>
        /// <param name="currency"></param>
        /// <param name="balance"></param>
        /// <returns></returns>
        public async Task<int> DBCreateTransaction(string user_uid, TransactionItem transaction)
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                // 
                string sql =
                    $"INSERT INTO `t_transactions`( " +
                    $"  `id`,`order_id`, " +
                    $"  `name`, `type`, `sub_type`,`product_id`,`count`, " +
                    $"  `price`,`amount`,`fee`,`currency`, " +
                    $"  `virtual_amount`,`virtual_currency`, " +
                    $"  `user_id`,`custom_id`, " +
                    $"  `channel`,`payment_method`, " +
                    $"  `create_time`,`update_time`,`complete_time`, " +
                    $"  `custom_data`,`code`, " +
                    $"  `status` " +
                    $") VALUES ( " +
                    $"  ?, ?, " +
                    $"  ?, ?, ?, ?, ?, " +
                    $"  ?, ?, ?, ?, " +
                    $"  ?, ?, " +
                    $"  ?, ?,  " +
                    $"  ?, ?,  " +
                    $"  CURRENT_TIMESTAMP,CURRENT_TIMESTAMP, NULL, " +
                    $"  NULL, NULL, " +
                    $"  1); ";
                var result_code = db?.Query(sql,
                        transaction.id, transaction.order_id,
                        transaction.name, transaction.type, transaction.sub_type, transaction.product_id, transaction.count,
                        transaction.price, transaction.amount, transaction.fee, transaction.currency,
                        transaction.virtual_amount, transaction.virtual_currency,
                        transaction.user_id, transaction.custom_id,
                        transaction.channel, transaction.payment_method
                        );
                if (result_code < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (CreateTransaction) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }
    }
}