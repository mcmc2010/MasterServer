CREATE TABLE `t_aiplayers` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `id` varchar(16) NOT NULL COMMENT '12位数字ID',
  `tid` int NOT NULL DEFAULT '0',
  `name` varchar(32) DEFAULT NULL,
  `level` int NOT NULL DEFAULT '0',
  `hol_value` int NOT NULL DEFAULT '100',
  `items` varchar(64) DEFAULT NULL,
  `gender` varchar(10) DEFAULT NULL,
  `region` varchar(32) DEFAULT NULL,
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `status` int NOT NULL DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`,`tid`,`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE `t_aiplayers` 
CHANGE COLUMN `gender` `gender` ENUM('female', 'male') NULL DEFAULT 'female' ;

ALTER TABLE `t_aiplayers` 
ADD COLUMN `match_status` INT NOT NULL DEFAULT 0 COMMENT '0:None;1:Match' AFTER `last_time`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`uid`, `id`, `tid`) ;


ALTER TABLE `t_aiplayers` 
CHANGE COLUMN `hol_value` `hol` INT NOT NULL DEFAULT '100' ;

ALTER TABLE `t_aiplayers` 
ADD COLUMN `total_matched` INT NOT NULL DEFAULT 0 AFTER `last_time`,
ADD COLUMN `total_played` INT NOT NULL DEFAULT 0 AFTER `total_matched`;

