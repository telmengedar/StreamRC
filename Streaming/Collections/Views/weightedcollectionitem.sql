CREATE VIEW weightedcollectionitem AS
SELECT collectionitem.collection, collectionitem.item, collectionitem.user, coalesce(user.status, 1) as status
FROM collectionitem
LEFT JOIN user ON user.name=collectionitem.user