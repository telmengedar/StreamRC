CREATE VIEW weightedvote AS
SELECT pollvote.poll, coalesce(polloption.description, pollvote.vote) as vote, pollvote.user, coalesce(user.status, 1) as status
FROM pollvote
LEFT JOIN polloption ON polloption.key=pollvote.vote
LEFT JOIN user ON user.name=pollvote.user
WHERE polloption.locked = 'False' OR polloption.locked = 0