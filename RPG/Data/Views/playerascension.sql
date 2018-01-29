CREATE VIEW playerascension AS
SELECT userid, player.level, player.experience as experience, levelentry.experience as nextlevel
FROM player
INNER JOIN levelentry ON player.level+1=levelentry.level