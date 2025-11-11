CREATE TABLE `t_gameeffects` (
  `uid` bigint unsigned NOT NULL AUTO_INCREMENT,
  `id` int NOT NULL COMMENT '唯一ID',
  `name` varchar(32) DEFAULT NULL,
  `user_id` varchar(16) NOT NULL COMMENT '玩家唯一ID',
  `type` int NOT NULL DEFAULT '1',
  `sub_type` int NOT NULL DEFAULT '0',
  `group` int NOT NULL DEFAULT '0',
  `value` varchar(32) DEFAULT NULL,
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '上次更新时间，通常是3秒内',
  `end_time` datetime DEFAULT NULL,
  `status` int DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除;',
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE `t_gameeffects` 
ADD COLUMN `items` VARCHAR(32) NULL DEFAULT NULL AFTER `value`;

ALTER TABLE `t_gameeffects` 
CHANGE COLUMN `items` `items` VARCHAR(32) NULL DEFAULT NULL COMMENT '如果取消物品，必须取消关联物品表中物品' ;

ALTER TABLE `t_gameeffects` 
ADD COLUMN `season` INT NOT NULL DEFAULT '0' COMMENT '赛季或赛年周期性的值' AFTER `end_time`;

ALTER TABLE `t_gameeffects` 
ADD INDEX `idx` (`id` ASC, `season` ASC, `user_id` ASC, `type` ASC, `group` ASC) VISIBLE;
