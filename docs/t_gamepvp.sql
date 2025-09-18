CREATE TABLE `t_gamepvp` (
  `uid` bigint unsigned NOT NULL AUTO_INCREMENT,
  `sn` varchar(32) NOT NULL COMMENT '唯一ID',
  `id` varchar(16) NOT NULL COMMENT '玩家唯一ID',
  `name` varchar(32) DEFAULT NULL,
  `tid` int NOT NULL DEFAULT '0' COMMENT '如果是AI该值是大于0，这个是角色模版ID',
  `type` enum('normal','ranking') NOT NULL DEFAULT 'normal',
  `level` int NOT NULL DEFAULT '0',
  `room_id` int NOT NULL DEFAULT '0',
  `game_status` enum('none','failure','victory','error') NOT NULL DEFAULT 'none',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '上次更新时间，通常是3秒内',
  `end_time` datetime DEFAULT NULL,
  `match_status` enum('none','waiting','matched','timeout','cancelled','completed','error') NOT NULL DEFAULT 'waiting',
  `status` int DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除;',
  PRIMARY KEY (`uid`,`sn`),
  UNIQUE KEY `sn_UNIQUE` (`sn`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


ALTER TABLE `t_gamepvp` 
DROP INDEX `sn_UNIQUE` ,
ADD INDEX `sn_UNIQUE` (`sn` ASC, `id` ASC) VISIBLE;
