CREATE VIEW equippediteminformation AS
SELECT equipmentitem.playerid, equipmentitem.itemid, equipmentitem.slot, item.armor, item.damage, item.usageoptimum, item.type, item.name
FROM equipmentitem
INNER JOIN item ON equipmentitem.itemid=item.id