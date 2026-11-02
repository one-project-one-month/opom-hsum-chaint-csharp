-- Incremental, idempotent repair for existing local databases.
-- Does not reseed users, credentials, donations, or monastery records.
USE hsumchaint_db;
START TRANSACTION;

INSERT INTO `Permission` (name, is_deleted)
VALUES ('Donation.Delete', 0)
ON DUPLICATE KEY UPDATE is_deleted = 0;

INSERT INTO Role_Permission (role_id, permission_id, is_deleted)
SELECT role.id, permission.id, 0
FROM `Role` role CROSS JOIN `Permission` permission
WHERE role.name = 'Admin' AND role.is_deleted = 0
  AND permission.name = 'Donation.Delete' AND permission.is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

COMMIT;
