CREATE VIEW fullshopitem AS
SELECT shopitem.itemid, shopitem.quantity, shopitem.discount, item.name, item.type, item.target, item.levelrequirement, item.armor, item.damage, item.countable
FROM shopitem
INNER JOIN item ON item.id=shopitem.itemid