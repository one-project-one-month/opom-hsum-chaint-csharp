-- Migration: Add Dynamic Login Mode and OTP Verification
-- Date: 2026-11-06
-- Target Database: MySQL 8.0+ / InnoDB
-- Description: Introduces Login_Mode and Otp_Code tables, and adds email/phone verification timestamps to User.

USE hsumchaint_db;

START TRANSACTION;

-- 1. Alter User table to allow nullable phone and add verification timestamps
ALTER TABLE `User`
    MODIFY `phone` VARCHAR(50) NULL,
    ADD COLUMN `phone_verified_at` DATETIME NULL AFTER `email`,
    ADD COLUMN `email_verified_at` DATETIME NULL AFTER `phone_verified_at`,
    ADD UNIQUE KEY `UX_User_email_active` (`email`, `is_deleted`);

-- 2. Create Login_Mode table
CREATE TABLE IF NOT EXISTS `Login_Mode` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `code` VARCHAR(30) NOT NULL,
    `display_name` VARCHAR(100) NOT NULL,
    `is_active` TINYINT(1) NOT NULL DEFAULT 0,
    `require_phone_verification` TINYINT(1) NOT NULL DEFAULT 0,
    `require_email_verification` TINYINT(1) NOT NULL DEFAULT 0,
    `updated_by_id` INT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `UX_Login_Mode_code` (`code`),
    KEY `IX_Login_Mode_is_active` (`is_active`),
    KEY `IX_Login_Mode_updated_by_id` (`updated_by_id`),
    CONSTRAINT `FK_Login_Mode_User_updated_by_id`
        FOREIGN KEY (`updated_by_id`) REFERENCES `User` (`id`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Seed default login modes
INSERT IGNORE INTO `Login_Mode` (`id`, `code`, `display_name`, `is_active`) VALUES
    (1, 'PHONE_ONLY', 'Phone only', 1),
    (2, 'EMAIL_ONLY', 'Email only', 0),
    (3, 'PHONE_OR_EMAIL', 'Phone or email', 0);

-- 4. Create Otp_Code table
CREATE TABLE IF NOT EXISTS `Otp_Code` (
    `id` BIGINT NOT NULL,
    `user_id` INT NULL,
    `channel` VARCHAR(20) NOT NULL,
    `purpose` VARCHAR(30) NOT NULL,
    `destination` VARCHAR(255) NOT NULL,
    `code_hash` VARCHAR(255) NOT NULL,
    `expires_at` DATETIME NOT NULL,
    `is_used` TINYINT(1) NOT NULL DEFAULT 0,
    `used_at` DATETIME NULL,
    `revoked_at` DATETIME NULL,
    `failed_attempt_count` INT NOT NULL DEFAULT 0,
    `max_attempt_count` INT NOT NULL DEFAULT 5,
    `created_at` DATETIME NOT NULL,
    PRIMARY KEY (`id`),
    KEY `IX_Otp_Code_user_id` (`user_id`),
    KEY `IX_Otp_Code_lookup` (`destination`, `channel`, `purpose`, `created_at`),
    KEY `IX_Otp_Code_expires_at` (`expires_at`),
    KEY `IX_Otp_Code_cleanup` (`used_at`, `expires_at`, `revoked_at`),
    CONSTRAINT `FK_Otp_Code_User_user_id`
        FOREIGN KEY (`user_id`) REFERENCES `User` (`id`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

COMMIT;
