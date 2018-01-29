CREATE VIEW activepoll AS
SELECT pollvote.poll as name, count(*) as votes
FROM pollvote
GROUP BY pollvote.poll