-- Migration: Add Donation.Delete Permission to System and Space RBAC
-- Date: 2026-10-23
-- Target Database: MySQL 8.0+ / InnoDB
-- Description: Introduces 'Donation.Delete' permission for removing accidental donation records.
--              Maps to System Roles (Admin, Monk) and Space Roles (Owner, Admin, Editor).

USE hsumchaint_db;

START TRANSACTION;

-- 1. Insert new system-level permission
INSERT INTO `Permission` (`name`, `is_deleted`) VALUES
    ('Donation.Delete', 0)
ON DUPLICATE KEY UPDATE `is_deleted` = 0;

-- 2. Map system permission to Role_Permission
-- System Admin (Role 1): Full access
INSERT INTO `Role_Permission` (`role_id`, `permission_id`, `is_deleted`)
SELECT 1, p.`id`, 0 
FROM `Permission` p 
WHERE p.`name` = 'Donation.Delete' AND p.`is_deleted` = 0
ON DUPLICATE KEY UPDATE `is_deleted` = 0;

-- Monk (Role 2): Monastery Creator/Manager
INSERT INTO `Role_Permission` (`role_id`, `permission_id`, `is_deleted`)
SELECT 2, p.`id`, 0 
FROM `Permission` p 
WHERE p.`name` = 'Donation.Delete' AND p.`is_deleted` = 0
ON DUPLICATE KEY UPDATE `is_deleted` = 0;

-- 3. Insert new space-level permission
INSERT INTO `Space_Permission` (`name`, `description`, `is_deleted`) VALUES
    ('Donation.Delete', 'Permanently delete unfulfilled or accidental donation records', 0)
ON DUPLICATE KEY UPDATE `description` = VALUES(`description`), `is_deleted` = 0;

-- 4. Map space permission to Space Roles
-- Space Owner (Role 1)
INSERT INTO `Space_Role_Permission` (`space_role_id`, `space_permission_id`, `is_deleted`)
SELECT 1, sp.`id`, 0 
FROM `Space_Permission` sp 
WHERE sp.`name` = 'Donation.Delete' AND sp.`is_deleted` = 0
ON DUPLICATE KEY UPDATE `is_deleted` = 0;

-- Space Admin (Role 2)
INSERT INTO `Space_Role_Permission` (`space_role_id`, `space_permission_id`, `is_deleted`)
SELECT 2, sp.`id`, 0 
FROM `Space_Permission` sp 
WHERE sp.`name` = 'Donation.Delete' AND sp.`is_deleted` = 0
ON DUPLICATE KEY UPDATE `is_deleted` = 0;

-- Space Editor (Role 3)
INSERT INTO `Space_Role_Permission` (`space_role_id`, `space_permission_id`, `is_deleted`)
SELECT 3, sp.`id`, 0 
FROM `Space_Permission` sp 
WHERE sp.`name` = 'Donation.Delete' AND sp.`is_deleted` = 0
ON DUPLICATE KEY UPDATE `is_deleted` = 0;

COMMIT;
