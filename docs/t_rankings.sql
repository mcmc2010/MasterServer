CREATE TABLE `t_rankings` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `id` varchar(16) NOT NULL COMMENT '玩家唯一ID',
  `name` varchar(32) DEFAULT NULL,
  `type` int NOT NULL DEFAULT '0',
  `balance` int NOT NULL DEFAULT '0',
  `currency` varchar(10) DEFAULT NULL,
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '上次更新时间，通常是3秒内',
  `status` int DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除;',
  PRIMARY KEY (`uid`),
  KEY `index2` (`id`,`type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
