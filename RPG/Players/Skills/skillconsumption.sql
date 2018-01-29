CREATE VIEW skillconsumption AS
SELECT playerskill.playerid, playerskill.skill, playerskill.level, 
CASE playerskill.level
    WHEN 1 THEN 1
    WHEN 2 THEN 3
    WHEN 3 THEN 6
    ELSE 0
END consumption
FROM playerskill