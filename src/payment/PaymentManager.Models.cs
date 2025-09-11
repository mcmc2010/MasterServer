
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
        /// 
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected List<TransactionItem> ToTransactionItems(List<DatabaseResultItemSet>? rows)
        {
            List<TransactionItem> items = new List<TransactionItem>();
            if (rows != null)
            {
                foreach (var v in rows)
                {
                    TransactionItem? item = v.To<TransactionItem>();
                    if (item == null) { continue; }

                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <param name="id"></param>
        /// <param name="order_id"></param>
        /// <returns></returns>
        protected async System.Threading.Tasks.Task<int> DBGetTransactions(DatabaseQuery? query, string user_uid,
                            List<TransactionItem> items,
                            string? id = null, string? order_id = null)
        {
            if (query == null)
            {
                return -1;
            }

            id = id?.Trim();
            if (id.IsNullOrWhiteSpace())
            {
                id = null;
            }

            order_id = order_id?.Trim();
            if (order_id.IsNullOrWhiteSpace())
            {
                order_id = null;
            }

            //
            List<DatabaseResultItemSet>? list = null;

            // 
            string sql =
            $"SELECT " +
            $"	  t.`uid` , t.`id`, t.`order_id`, " +
            $"    t.`name`, t.`type`, t.`sub_type`, t.`product_id`, t.`count`, " +
            $"    t.`price`, t.`amount`, t.`fee`, t.`currency`, " +
            $"    t.`virtual_amount`, t.`virtual_currency`,  " +
            $"    t.`user_id`, t.`custom_id`, " +
            $"    t.`channel`, t.`payment_method`, " +
            $"    t.`create_time`, t.`update_time`, t.`complete_time`, " +
            $"    t.`code` AS result_code, t.`custom_data`, t.`status`  " +
            $"FROM `t_transactions` t  " +
            $"WHERE  " +
            $"    t.`status` > 0 AND t.`user_id` = ? AND" +
            $"    (? IS NOT NULL AND t.`id` = ?) OR (? IS NOT NULL AND t.`order_id` = ? )";
            var result_code = query.QueryWithList(sql, out list,
                user_uid,
                id, id, order_id, order_id);
            if (result_code < 0)
            {
                return -1;
            }

            items.AddRange(this.ToTransactionItems(list));

            return items.Count;
        }


        protected async System.Threading.Tasks.Task<int> DBUpdateTransaction(DatabaseQuery? query, string user_uid,
                            TransactionItem transaction,
                            string reason)
        {
            if (query == null)
            {
                return -1;
            }

            DateTime? completed_time = null;
            if (reason == "error" || reason == "completed")
            {
                completed_time = DateTime.Now;
            }

            //
            List<DatabaseResultItemSet>? list = null;

            // 
            string sql =
            $"UPDATE `t_transactions` SET " +
            $"	  `code` = ?, `virtual_amount` = ?, `virtual_currency` = ?, " +
            $"    `payment_method` = ?, " +
            $"    `update_time` = CURRENT_TIMESTAMP, `complete_time` = ? " +
            $"WHERE  " +
            $"    `status` > 0 AND `user_id` = ? AND" +
            $"    `id` = ? AND `order_id` = ?";
            var result_code = query.QueryWithList(sql, out list,
                reason,
                transaction.virtual_amount, transaction.virtual_currency,
                transaction.payment_method,
                completed_time,
                user_uid,
                transaction.id, transaction.order_id);
            if (result_code < 0)
            {
                return -1;
            }

            transaction.update_time = DateTime.UtcNow;
            transaction.complete_time = completed_time;
            transaction.result_code = reason;

            return 1;
        }

        protected async System.Threading.Tasks.Task<int> DBReviewTransaction(DatabaseQuery? query, string user_uid,
                            TransactionItem transaction, string reason)
        {
            if (query == null)
            {
                return -1;
            }

            reason = reason.Trim().ToLower();

            string placeholders = "'error', 'review', 'approved', 'rejected'";

            //
            List<DatabaseResultItemSet>? list = null;

            // 
            string sql =
            $"UPDATE `t_transactions` SET " +
            $"	  `code` = ?,  " +
            $"    `update_time` = CURRENT_TIMESTAMP, `pending_time` = CURRENT_TIMESTAMP " +
            $"WHERE  " +
            $"    `status` > 0 AND `user_id` = ? AND" +
            $"    `id` = ? AND `order_id` = ? AND `code` NOT IN ({placeholders})";
            var result_code = query.QueryWithList(sql, out list,
                reason,
                user_uid,
                transaction.id, transaction.order_id);
            if (result_code < 0)
            {
                return -1;
            }

            transaction.update_time = DateTime.UtcNow;
            transaction.result_code = reason;

            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<int> DBGetTransactions(string user_uid,
                            List<TransactionItem> items,
                            string? id = null, string? order_id = null)
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                items.Clear();

                var result_code = await DBGetTransactions(db, user_uid, items, id, order_id);
                if (result_code < 0)
                {
                    return -1;
                }

                return items.Count;
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} Error :" + e.Message);
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return -1;
        }


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
                    $"  NULL, ?, " +
                    $"  1); ";
                var result_code = db?.Query(sql,
                        transaction.id, transaction.order_id,
                        transaction.name, transaction.type, transaction.sub_type, transaction.product_id, transaction.count,
                        transaction.price, transaction.amount, transaction.fee, transaction.currency,
                        transaction.virtual_amount, transaction.virtual_currency,
                        transaction.user_id, transaction.custom_id,
                        transaction.channel, transaction.payment_method,
                        transaction.result_code);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<int> DBFinalTransaction(string user_uid, TransactionItem transaction,
                            string reason)
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                if (await DBUpdateTransaction(db, user_uid, transaction, reason) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();

            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (FinalTransaction) Error :" + e.Message);
                return -1;
            }
            finally
            {
                DatabaseManager.Instance.Free(db);
            }
            return 1;
        }
        

        /// <summary>
        /// 订单待审核
        /// </summary>
        /// <param name="user_uid"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<int> DBReviewTransaction(string user_uid, TransactionItem transaction,
                                    string reason = "pending")
        {

            var db = DatabaseManager.Instance.New();
            try
            {
                db?.Transaction();

                if (await DBReviewTransaction(db, user_uid, transaction, reason) < 0)
                {
                    db?.Rollback();
                    return -1;
                }

                db?.Commit();
                
            }
            catch (Exception e)
            {
                _logger?.LogError($"{TAGName} (ReviewTransaction) Error :" + e.Message);
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