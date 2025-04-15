ALTER TABLE `t_matches`
CHANGE COLUMN `flag` `flag` ENUM('none', 'waiting', 'matched', 'timeout', 'cancelled', 'game', 'completed', 'error') NULL DEFAULT 'waiting' ;
ALTER TABLE `t_matches` 
CHANGE COLUMN `status` `status` INT NULL DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除;2:AI' ;
