CREATE TABLE `t_transaction` (
  `uid` bigint unsigned NOT NULL COMMENT 'IID',
  `id` varchar(32) NOT NULL COMMENT '流水单号',
  `order_id` varchar(32) NOT NULL DEFAULT '0' COMMENT '业务订单号',
  `name` varchar(64) DEFAULT NULL COMMENT '物品名称',
  `type` int NOT NULL DEFAULT '1' COMMENT '交易类型：1-支付 2-退款 3-转账',
  `sub_type` int NOT NULL DEFAULT '0' COMMENT '交易类型：1-支付 2-退款 3-转账',
  `product_id` varchar(32) NOT NULL COMMENT '商品ID',
  `count` int NOT NULL DEFAULT '1' COMMENT '数量',
  `amount` decimal(12,2) NOT NULL COMMENT '花费',
  `fee` decimal(12,2) NOT NULL DEFAULT '0.00',
  `currency` varchar(5) DEFAULT 'CNY',
  `virtual_amount` decimal(12,2) DEFAULT '0.00' COMMENT '花费',
  `virtual_currency` varchar(5) DEFAULT 'GEM',
  `user_id` varchar(16) NOT NULL COMMENT '12位数字ID',
  `custom_id` varchar(16) NOT NULL COMMENT '第三方ID，此处为playfab',
  `channel` varchar(32) NOT NULL COMMENT '交易渠道：APP/WEB/API等',
  `payment_method` varchar(32) DEFAULT NULL COMMENT '支付方式：ALIPAY/WECHAT/APPLE_PAY/BANK_CARD',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `update_time` datetime DEFAULT NULL,
  `complete_time` datetime DEFAULT NULL,
  `custom_data` varchar(256) DEFAULT NULL,
  `code` varchar(16) DEFAULT NULL,
  `status` int NOT NULL DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`),
  UNIQUE KEY `idx_id` (`id`),
  UNIQUE KEY `idex_order_id` (`order_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


ALTER TABLE `t_transaction` 
ADD COLUMN `price` DECIMAL(12,2) NOT NULL DEFAULT 0.00 AFTER `count`,
CHANGE COLUMN `amount` `amount` DECIMAL(12,2) NOT NULL DEFAULT 0.00 COMMENT '花费' ;

ALTER TABLE `t_transaction` 
CHANGE COLUMN `virtual_currency` `virtual_currency` VARCHAR(5) NULL DEFAULT 'GM' ;

ALTER TABLE `t_transaction` 
RENAME TO  `t_transactions` ;

ALTER TABLE `t_transactions` 
AUTO_INCREMENT = 10000 ,
CHANGE COLUMN `uid` `uid` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'IID' ;

ALTER TABLE `t_transactions` 
ADD COLUMN `pending_time` DATETIME NULL DEFAULT NULL AFTER `complete_time`;

ALTER TABLE `t_transactions` 
ADD COLUMN `manual` VARCHAR(16) NULL DEFAULT NULL COMMENT '人工复核' AFTER `code`;


