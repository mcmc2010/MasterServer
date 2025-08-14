CREATE TABLE `t_friends` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `id` varchar(16) NOT NULL COMMENT '所属用户ID，系统小于1000',
  `friend_id` varchar(16) NOT NULL COMMENT '发送用户ID，系统小于1000',
  `type` enum('pending','accepted','blocked') NOT NULL DEFAULT 'pending' COMMENT '默认请求',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `intimacy` int DEFAULT '0' COMMENT '亲密度',
  `status` int DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`),
  UNIQUE KEY `idx_friends` (`id`,`friend_id`),
  KEY `idx_types` (`id`,`type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

