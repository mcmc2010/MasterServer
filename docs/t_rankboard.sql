CREATE TABLE `t_rankboard` (
  `type` int NOT NULL COMMENT '类型',
  `data` blob COMMENT '数据',
  PRIMARY KEY (`type`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
