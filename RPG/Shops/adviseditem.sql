CREATE VIEW adviseditem AS
SELECT shopitem.itemid, shopitem.quantity, shopitem.discount, item1.name, item1.value, item1.type, item1.target, item1.levelrequirement, item1.armor, item1.damage, item1.countable, equipmentitem.playerid
FROM shopitem
INNER JOIN item AS item1 ON item1.id=shopitem.itemid
INNER JOIN equipmentitem ON (equipmentitem.slot & 255) = item1.target
INNER JOIN player ON player.userid=equipmentitem.playerid
INNER JOIN item as item2 ON item2.id=equipmentitem.itemid
WHERE item1.levelrequirement<=player.level AND (item1.damage > item2.damage OR item1.armor > item2.armor)