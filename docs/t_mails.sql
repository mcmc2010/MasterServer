CREATE TABLE `t_mails` (
  `uid` int unsigned NOT NULL AUTO_INCREMENT,
  `sn` varchar(32) NOT NULL COMMENT '唯一ID',
  `id` varchar(16) NOT NULL COMMENT '所属用户ID，系统小于1000',
  `sender_id` varchar(16) NOT NULL COMMENT '发送用户ID，系统小于1000',
  `type` enum('normal','system','master') NOT NULL DEFAULT 'normal',
  `title` varchar(32) DEFAULT NULL COMMENT '邮件标题',
  `content` varchar(2048) DEFAULT NULL COMMENT '邮件内容',
  `attachment` varchar(64) DEFAULT NULL COMMENT '附件格式:ID,NUM|ID,NUM',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `read_time` datetime DEFAULT NULL COMMENT '已读时间',
  `received_time` datetime DEFAULT NULL COMMENT '附件已领取时间',
  `status` int DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`,`sn`),
  UNIQUE KEY `sn_UNIQUE` (`sn`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
