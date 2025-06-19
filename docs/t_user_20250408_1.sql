-- ALTER TABLE `t_user` (
--   `uid` int unsigned NOT NULL AUTO_INCREMENT,
--   `id` varchar(16) NOT NULL COMMENT '12位数字ID',
--   `client_id` varchar(32) NOT NULL COMMENT '字母或数字，通常是16位',
--   `token` varchar(64) DEFAULT NULL,
--   `passphrase` varchar(32) DEFAULT NULL,
--   `playfab_id` varchar(32) DEFAULT NULL COMMENT 'Playfab ID',
--   `create_time` datetime DEFAULT CURRENT_TIMESTAMP,
--   `last_time` datetime DEFAULT CURRENT_TIMESTAMP,
--   `status` int DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
--   PRIMARY KEY (`uid`),
--   UNIQUE KEY `id_UNIQUE` (`id`),
--   UNIQUE KEY `client_id_UNIQUE` (`client_id`)
-- ) ENGINE=InnoDB AUTO_INCREMENT=1001 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
ALTER TABLE `t_user` 
ADD COLUMN `name` VARCHAR(32) NULL DEFAULT NULL COMMENT '' AFTER `id`;
ALTER TABLE `t_user` 
ADD COLUMN `device` VARCHAR(32) NULL DEFAULT NULL COMMENT 'OSPlatform' AFTER `playfab_id`;

--
ALTER TABLE `t_user` 
DROP PRIMARY KEY,
ADD PRIMARY KEY (`uid`, `id`);

--
ALTER TABLE `t_user` 
ADD COLUMN `privilege_level` int DEFAULT '0' COMMENT '7是gm' AFTER `last_time`;
ALTER TABLE `game`.`t_user` 
CHANGE COLUMN `name` `name` VARCHAR(32) NULL DEFAULT NULL ;
