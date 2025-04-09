CREATE TABLE `t_matches` (
  `uid` bigint unsigned NOT NULL AUTO_INCREMENT,
  `sn` varchar(32) NOT NULL COMMENT '唯一ID',
  `id` varchar(16) NOT NULL COMMENT '玩家唯一ID',
  `hol` int DEFAULT '100' COMMENT '1-99999',
  `type` enum('normal','ranking') NOT NULL DEFAULT 'normal',
  `level` int NOT NULL DEFAULT '0',
  `flag` enum('waiting','game','timeout','cancelled') DEFAULT 'waiting',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '上次更新时间，通常是3秒内',
  `status` int DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除',
  PRIMARY KEY (`uid`),
  UNIQUE KEY `id_UNIQUE` (`id`),
  UNIQUE KEY `sn_UNIQUE` (`sn`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `t_matches_archived` (
  `uid` bigint unsigned NOT NULL AUTO_INCREMENT,
  `sn` varchar(32) NOT NULL COMMENT '唯一ID',
  `id` varchar(16) NOT NULL COMMENT '玩家唯一ID',
  `hol` int DEFAULT '100' COMMENT '1-99999',
  `type` enum('normal','ranking') NOT NULL DEFAULT 'normal',
  `level` int NOT NULL DEFAULT '0',
  `flag` enum('waiting','game','timeout','cancelled') DEFAULT 'waiting',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '上次更新时间，通常是3秒内',
  `status` int DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除',
  PRIMARY KEY (`uid`),
  UNIQUE KEY `id_UNIQUE` (`id`),
  UNIQUE KEY `sn_UNIQUE` (`sn`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
