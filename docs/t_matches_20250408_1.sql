ALTER TABLE `t_matches`
CHANGE COLUMN `flag` `flag` ENUM('none', 'waiting', 'matched', 'timeout', 'cancelled', 'game', 'completed', 'error') NULL DEFAULT 'waiting' ;

ALTER TABLE `t_matches` 
CHANGE COLUMN `status` `status` INT NULL DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除;2:AI' ;

ALTER TABLE `t_matches` 
ADD COLUMN `name` VARCHAR(32) NULL DEFAULT NULL AFTER `id`,
ADD COLUMN `tid` INT NOT NULL DEFAULT 0 COMMENT '如果是AI该值是大于0，这个是角色模版ID' AFTER `name`,
CHANGE COLUMN `status` `status` INT NULL DEFAULT '1' COMMENT '0:归档;1:启用;-1:删除;' ,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`uid`, `sn`, `id`);

ALTER TABLE `t_matches` 
ADD COLUMN `room_id` INT NOT NULL DEFAULT 0 AFTER `level`,
CHANGE COLUMN `last_time` `last_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '上次更新时间，通常是3秒内' AFTER `create_time`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`uid`, `sn`, `id`, `room_id`);
