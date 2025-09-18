CREATE TABLE `t_user` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `id` varchar(16) NOT NULL COMMENT '12位数字ID',
  `client_id` varchar(32) DEFAULT NULL COMMENT '字母或数字，通常是16位',
  `token` varchar(64) DEFAULT NULL,
  `passphrase` varchar(32) DEFAULT NULL,
  `playfab_id` varchar(32) DEFAULT NULL COMMENT 'Playfab ID',
  `create_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `status` int DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB AUTO_INCREMENT=1000 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE `t_user` 
ADD INDEX `name_INDEX` (`name` ASC) VISIBLE;


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
ALTER TABLE `t_user` 
CHANGE COLUMN `name` `name` VARCHAR(32) NULL DEFAULT NULL ;


--
ALTER TABLE `t_user` 
ADD COLUMN `gender` INT NOT NULL DEFAULT '0' COMMENT '默认0' AFTER `playfab_id`,
ADD COLUMN `region` VARCHAR(64) NULL DEFAULT NULL COMMENT '地区' AFTER `gender`,
ADD COLUMN `avatar` INT NOT NULL DEFAULT '0' COMMENT '头像' AFTER `region`;

--
ALTER TABLE `t_user` 
ADD COLUMN `changed_time` DATETIME NULL DEFAULT NULL AFTER `last_time`;

ALTER TABLE `t_user` 
ADD COLUMN `updated_time` DATETIME NULL DEFAULT NULL AFTER `changed_time`;

-- 
ALTER TABLE `t_user` 
CHANGE COLUMN `avatar` `avatar` VARCHAR(128) NULL DEFAULT NULL COMMENT '头像' ;