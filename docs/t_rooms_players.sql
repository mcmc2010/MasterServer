CREATE TABLE `t_rooms_players` (
  `uid` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `rid` INT NOT NULL DEFAULT '-1' COMMENT '12位数字ID',
  `id` VARCHAR(16) NOT NULL COMMENT '房主玩家ID',
  `create_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `joined_time` DATETIME NULL DEFAULT NULL,
  `leave_time` DATETIME NULL DEFAULT NULL,
  `role` ENUM('none', 'master', 'member', 'lookon') NOT NULL DEFAULT 'none',
  `status` INT NOT NULL DEFAULT '1' COMMENT '0:禁用;1:启用;-1:删除',
  PRIMARY KEY (`uid`, `rid`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;