CREATE TABLE `t_gameevents` (
  `uid` bigint unsigned NOT NULL AUTO_INCREMENT,
  `id` int NOT NULL COMMENT '唯一ID',
  `name` varchar(32) DEFAULT NULL,
  `user_id` varchar(16) NOT NULL COMMENT '玩家唯一ID',
  `type` int NOT NULL DEFAULT '1',
  `count` int NOT NULL DEFAULT '0',
  `items` varchar(64) DEFAULT NULL,
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '上次更新时间，通常是3秒内',
  `completed_time` datetime DEFAULT NULL,
  `status` int DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除;',
  PRIMARY KEY (`uid`),
  KEY `index2` (`user_id`,`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


ALTER TABLE `t_gameevents` 
ADD COLUMN `sub_type` INT NOT NULL DEFAULT 0 AFTER `type`,
ADD COLUMN `value` VARCHAR(32) NULL DEFAULT NULL AFTER `sub_type`;

ALTER TABLE `t_gameevents` 
ADD COLUMN `group` INT NOT NULL DEFAULT 0 AFTER `sub_type`;

-- Add season field
ALTER TABLE `t_gameevents` 
ADD COLUMN `season` INT NOT NULL DEFAULT '0' COMMENT '赛季或赛年周期性的值' AFTER `group`,
DROP INDEX `index2` ,
ADD INDEX `idx` (`user_id` ASC, `id` ASC, `season` ASC) VISIBLE;


ALTER TABLE `t_gameevents` 
ADD COLUMN `virtual_currency` VARCHAR(64) NULL DEFAULT NULL AFTER `count`;