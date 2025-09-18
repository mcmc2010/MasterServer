CREATE TABLE `t_hol` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `id` varchar(16) NOT NULL COMMENT '12位数字ID',
  `value` int DEFAULT '100',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `status` int DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`),
  UNIQUE KEY `id_UNIQUE` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE `t_hol` 
ADD COLUMN `rank_level` INT NOT NULL DEFAULT 1000 COMMENT '默认为青铜三星' AFTER `status`;

ALTER TABLE `t_hol` 
ADD COLUMN `rank_value` INT NOT NULL DEFAULT 0 COMMENT '目前只有在大师之后，用来计数' AFTER `rank_level`,
CHANGE COLUMN `rank_level` `rank_level` INT NOT NULL DEFAULT 1000 COMMENT '默认为青铜三星' AFTER `last_time`;

ALTER TABLE `t_hol` 
ADD COLUMN `last_rank_level` INT NOT NULL DEFAULT '1000' COMMENT '默认为青铜三星' AFTER `last_time`,
ADD COLUMN `last_rank_value` INT NOT NULL DEFAULT '0' COMMENT '目前只有在大师之后，用来计数' AFTER `last_rank_level`,
CHANGE COLUMN `rank_value` `rank_value` INT NOT NULL DEFAULT '0' COMMENT '目前只有在大师之后，用来计数' AFTER `rank_level`;

ALTER TABLE `t_hol` 
ADD COLUMN `season` INT NOT NULL DEFAULT 1 COMMENT '玩家当前赛季' AFTER `rank_value`;

ALTER TABLE `t_hol` 
ADD COLUMN `season_time` INT NULL DEFAULT NULL COMMENT '玩家赛季开始时间，为NULL就是赛季没有游戏' AFTER `season`,

ALTER TABLE `t_hol` 
ADD COLUMN `challenger_reals` INT NOT NULL DEFAULT 0 COMMENT '大师印记' AFTER `rank_value`;

ALTER TABLE `t_hol` 
ADD COLUMN `played_count` INT NOT NULL DEFAULT '0' COMMENT '游戏次数或局数' AFTER `season_time`,
ADD COLUMN `played_win_count` INT NOT NULL DEFAULT '0' COMMENT '游戏次数或局数' AFTER `played_count`;

ALTER TABLE `t_hol` 
ADD COLUMN `cp_value` INT NOT NULL DEFAULT 100 COMMENT 'Combat Power' AFTER `value`;

ALTER TABLE `t_hol` 
ADD COLUMN `winning_streak_count` INT NOT NULL DEFAULT '0' COMMENT '连胜次数' AFTER `played_win_count`,
ADD COLUMN `winning_streak_highest` INT NOT NULL DEFAULT '0' COMMENT '最高连胜次数' AFTER `winning_streak_count`;


--
ALTER TABLE `t_hol` 
ADD UNIQUE KEY `id_season` (`id`,`season`) USING BTREE;

