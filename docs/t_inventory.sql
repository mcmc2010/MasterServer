CREATE TABLE `t_inventory` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'Server ItemInstanceID',
  `id` varchar(16) NOT NULL COMMENT 'PlayFab ItemInstanceID',
  `tid` int NOT NULL DEFAULT '0' COMMENT '物品索引index',
  `name` varchar(32) DEFAULT NULL COMMENT '物品名称',
  `user_id` varchar(16) NOT NULL COMMENT '12位数字ID',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime DEFAULT NULL,
  `expired_time` datetime DEFAULT NULL COMMENT '过期时间，物品一旦获得就开始计算过期时间',
  `using_time` datetime DEFAULT NULL COMMENT '使用时间',
  `remaining_time` datetime DEFAULT NULL COMMENT '剩余时间，从第一次使用开始计算剩余时间',
  `custom_data` varchar(256) DEFAULT NULL COMMENT '必要的物品属性',
  `status` int NOT NULL DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`,`id`,`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1000 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE `t_inventory` 
ADD COLUMN `count` INT NOT NULL DEFAULT 1 COMMENT '物品数量' AFTER `status`;

ALTER TABLE `t_inventory` 
CHANGE COLUMN `count` `count` INT NOT NULL DEFAULT '1' COMMENT '物品数量' AFTER `user_id`;

ALTER TABLE `t_inventory` 
ADD COLUMN `type` INT NOT NULL DEFAULT '0' COMMENT '物品类型' AFTER `status`;

ALTER TABLE `t_inventory` 
CHANGE COLUMN `type` `type` INT NOT NULL DEFAULT '0' COMMENT '物品类型' AFTER `name`;

ALTER TABLE `t_inventory` 
ADD UNIQUE INDEX `id_UNIQUE` (`id` ASC) VISIBLE;

ALTER TABLE `t_inventory` 
ADD COLUMN `reason` VARCHAR(16) NULL DEFAULT NULL AFTER `custom_data`;

ALTER TABLE `t_inventory` 
ADD COLUMN `group` INT NOT NULL DEFAULT '0' COMMENT '物品分组号' AFTER `user_id`;
