CREATE VIEW fullinventoryitem AS
SELECT inventoryitem.playerid, item.id, item.name, item.hp, item.type, inventoryitem.quantity, item.value
FROM inventoryitem
INNER JOIN item ON item.id=inventoryitem.itemid