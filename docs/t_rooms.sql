CREATE TABLE `t_rooms` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `id` int NOT NULL DEFAULT '-1' COMMENT '12位数字ID',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `status` int NOT NULL DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`,`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10000 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE `t_rooms` 
ADD COLUMN `ids` VARCHAR(64) NOT NULL COMMENT '关联玩家ID列表使用分号隔开' AFTER `id`;

ALTER TABLE `t_rooms` 
ADD COLUMN `ids_1` VARCHAR(64) NULL DEFAULT NULL COMMENT '关联玩家ID列表使用分号隔开' AFTER `ids_0`,
CHANGE COLUMN `ids` `ids_0` VARCHAR(64) NULL DEFAULT NULL COMMENT '关联玩家ID列表使用分号隔开' ;

ALTER TABLE `t_rooms` 
ADD COLUMN `service_uid` INT NOT NULL DEFAULT 0 COMMENT '服务编号，关联服务端' AFTER `last_time`;

