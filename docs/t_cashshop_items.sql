CREATE TABLE `t_cashshop_items` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'Server ItemInstanceID',
  `id` varchar(16) NOT NULL COMMENT 'PlayFab ItemInstanceID',
  `tid` int NOT NULL DEFAULT '0' COMMENT '物品索引index',
  `name` varchar(32) DEFAULT NULL COMMENT '物品名称',
  `type` int NOT NULL DEFAULT '0' COMMENT '物品类型',
  `user_id` varchar(16) NOT NULL COMMENT '12位数字ID',
  `count` int NOT NULL DEFAULT '1' COMMENT '物品数量',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `amount` decimal(10,0) DEFAULT NULL COMMENT '花费',
  `balance` decimal(10,0) DEFAULT NULL COMMENT '余额',
  `custom_data` varchar(256) DEFAULT NULL COMMENT '必要的物品属性',
  `status` int NOT NULL DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`,`id`,`user_id`),
  UNIQUE KEY `id_UNIQUE` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
SELECT * FROM game.t_cashshop_items;


ALTER TABLE `game`.`t_cashshop_items` 
CHANGE COLUMN `id` `id` VARCHAR(16) NOT NULL COMMENT '流水单号' ,
CHANGE COLUMN `tid` `product_id` INT NOT NULL DEFAULT '0' COMMENT '产品ID' ;

ALTER TABLE `game`.`t_cashshop_items` 
CHANGE COLUMN `id` `id` VARCHAR(32) NOT NULL COMMENT '流水单号' ;

ALTER TABLE `game`.`t_cashshop_items` 
CHANGE COLUMN `product_id` `product_id` VARCHAR(32) NOT NULL DEFAULT '0' COMMENT '产品ID' ;
CHANGE COLUMN `name` `name` VARCHAR(64) DEFAULT NULL COMMENT '物品名称' ;


ALTER TABLE `game`.`t_cashshop_items` 
ADD COLUMN `custom_id` VARCHAR(16) NOT NULL COMMENT '第三方ID，此处为playfab' AFTER `user_id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`uid`, `id`, `user_id`, `custom_id`);

ALTER TABLE `game`.`t_cashshop_items` 
ADD COLUMN `item_0` VARCHAR(64) NULL DEFAULT NULL AFTER `balance`,
ADD COLUMN `item_1` VARCHAR(64) NULL DEFAULT NULL AFTER `item_0`,
ADD COLUMN `item_2` VARCHAR(64) NULL DEFAULT NULL AFTER `item_1`;

ALTER TABLE `game`.`t_cashshop_items` 
ADD COLUMN `virtual_balance` DECIMAL(10,0) NULL DEFAULT NULL COMMENT '可能是获得虚拟货币' AFTER `balance`,
ADD COLUMN `virtual_currency` VARCHAR(5) NULL DEFAULT 'GM' AFTER `virtual_balance`;