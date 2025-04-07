CREATE TABLE `t_hol` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `id` varchar(16) NOT NULL COMMENT '12位数字ID',
  `value` int DEFAULT '100',
  `create_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `status` int DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
