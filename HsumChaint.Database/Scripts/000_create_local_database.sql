-- Local MySQL setup for HsumChaint.
-- Run with:
-- mysql -u root -p -P 3307 -h localhost < HsumChaint.Database/Scripts/000_create_local_database.sql

CREATE DATABASE IF NOT EXISTS hsumchaint_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE hsumchaint_db;

-- 1. Access Control: System Role & Permission Tables
CREATE TABLE IF NOT EXISTS Role (
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(50) NOT NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Role_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Permission (
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(50) NOT NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Permission_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Role_Permission (
    id INT NOT NULL AUTO_INCREMENT,
    role_id INT NOT NULL,
    permission_id INT NOT NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Role_Permission_role_permission (role_id, permission_id),
    KEY IX_Role_Permission_role_id (role_id),
    KEY IX_Role_Permission_permission_id (permission_id),
    CONSTRAINT FK_Role_Permission_Role_role_id
        FOREIGN KEY (role_id) REFERENCES Role (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Role_Permission_Permission_permission_id
        FOREIGN KEY (permission_id) REFERENCES Permission (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 1.1 Space Access Control: Space Role & Space Permission Tables
CREATE TABLE IF NOT EXISTS Space_Role (
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Space_Role_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Space_Permission (
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Space_Permission_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Space_Role_Permission (
    id INT NOT NULL AUTO_INCREMENT,
    space_role_id INT NOT NULL,
    space_permission_id INT NOT NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Space_Role_Permission (space_role_id, space_permission_id),
    KEY IX_Space_Role_Permission_role_id (space_role_id),
    KEY IX_Space_Role_Permission_permission_id (space_permission_id),
    CONSTRAINT FK_Space_Role_Permission_Space_Role_role_id
        FOREIGN KEY (space_role_id) REFERENCES Space_Role (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Space_Role_Permission_Space_Permission_permission_id
        FOREIGN KEY (space_permission_id) REFERENCES Space_Permission (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Core User Table
CREATE TABLE IF NOT EXISTS User (
    id INT NOT NULL AUTO_INCREMENT,
    role_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    phone VARCHAR(50) NULL,
    password VARCHAR(255) NOT NULL,
    email VARCHAR(255) NULL,
    phone_verified_at DATETIME NULL,
    email_verified_at DATETIME NULL,
    contact_phone VARCHAR(50) NULL,
    fcm_token VARCHAR(255) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_User_phone_active (phone, is_deleted),
    UNIQUE KEY UX_User_email_active (email, is_deleted),
    KEY IX_User_role_id (role_id),
    CONSTRAINT FK_User_Role_role_id
        FOREIGN KEY (role_id) REFERENCES Role (id)
        ON DELETE RESTRICT,
    CONSTRAINT CK_User_identifier CHECK (phone IS NOT NULL OR email IS NOT NULL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Login_Mode (
    id INT NOT NULL AUTO_INCREMENT,
    code VARCHAR(30) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 0,
    require_phone_verification TINYINT(1) NOT NULL DEFAULT 0,
    require_email_verification TINYINT(1) NOT NULL DEFAULT 0,
    updated_by_id INT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Login_Mode_code (code),
    KEY IX_Login_Mode_is_active (is_active),
    KEY IX_Login_Mode_updated_by_id (updated_by_id),
    CONSTRAINT FK_Login_Mode_User_updated_by_id
        FOREIGN KEY (updated_by_id) REFERENCES User (id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO Login_Mode (id, code, display_name, is_active) VALUES
    (1, 'PHONE_ONLY', 'Phone only', 1),
    (2, 'EMAIL_ONLY', 'Email only', 0),
    (3, 'PHONE_OR_EMAIL', 'Phone or email', 0);

CREATE TABLE IF NOT EXISTS Otp_Code (
    id BIGINT NOT NULL,
    user_id INT NULL,
    channel VARCHAR(20) NOT NULL,
    purpose VARCHAR(30) NOT NULL,
    destination VARCHAR(255) NOT NULL,
    code_hash VARCHAR(255) NOT NULL,
    expires_at DATETIME NOT NULL,
    is_used TINYINT(1) NOT NULL DEFAULT 0,
    used_at DATETIME NULL,
    revoked_at DATETIME NULL,
    failed_attempt_count INT NOT NULL DEFAULT 0,
    max_attempt_count INT NOT NULL DEFAULT 5,
    created_at DATETIME NOT NULL,
    PRIMARY KEY (id),
    KEY IX_Otp_Code_user_id (user_id),
    KEY IX_Otp_Code_lookup (destination, channel, purpose, created_at),
    KEY IX_Otp_Code_expires_at (expires_at),
    KEY IX_Otp_Code_cleanup (used_at, expires_at, revoked_at),
    CONSTRAINT FK_Otp_Code_User_user_id
        FOREIGN KEY (user_id) REFERENCES User (id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Profiles & Spaces
CREATE TABLE IF NOT EXISTS MonkProfile (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NOT NULL,
    monastery_name VARCHAR(255) NULL,
    monastery_address VARCHAR(500) NULL,
    PRIMARY KEY (id),
    KEY IX_MonkProfile_user_id (user_id),
    CONSTRAINT FK_MonkProfile_User_user_id
        FOREIGN KEY (user_id) REFERENCES User (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Monastery_Space (
    id INT NOT NULL AUTO_INCREMENT,
    monastery_name VARCHAR(255) NULL,
    description VARCHAR(1000) NULL,
    address VARCHAR(500) NULL,
    created_by_id INT NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    KEY IX_Monastery_Space_created_by_id (created_by_id),
    CONSTRAINT FK_Monastery_Space_User_created_by_id
        FOREIGN KEY (created_by_id) REFERENCES User (id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Monastery_Member (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NULL,
    monastery_space_id INT NULL,
    space_role_id INT NOT NULL,
    is_owner TINYINT(1) NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Monastery_Member_user_space (user_id, monastery_space_id),
    KEY IX_Monastery_Member_monastery_space_id (monastery_space_id),
    KEY IX_Monastery_Member_space_role_id (space_role_id),
    CONSTRAINT FK_Monastery_Member_User_user_id
        FOREIGN KEY (user_id) REFERENCES User (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Monastery_Member_Monastery_Space_monastery_space_id
        FOREIGN KEY (monastery_space_id) REFERENCES Monastery_Space (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Monastery_Member_Space_Role_space_role_id
        FOREIGN KEY (space_role_id) REFERENCES Space_Role (id)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Invitation (
    id INT NOT NULL AUTO_INCREMENT,
    monastery_space_id INT NULL,
    invited_user_id INT NULL,
    invited_by_id INT NULL,
    space_role_id INT NOT NULL DEFAULT 4,
    status INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY IX_Invitation_monastery_space_id (monastery_space_id),
    KEY IX_Invitation_invited_user_id (invited_user_id),
    KEY IX_Invitation_invited_by_id (invited_by_id),
    KEY IX_Invitation_space_role_id (space_role_id),
    CONSTRAINT FK_Invitation_Monastery_Space_monastery_space_id
        FOREIGN KEY (monastery_space_id) REFERENCES Monastery_Space (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Invitation_User_invited_user_id
        FOREIGN KEY (invited_user_id) REFERENCES User (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Invitation_User_invited_by_id
        FOREIGN KEY (invited_by_id) REFERENCES User (id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Invitation_Space_Role_space_role_id
        FOREIGN KEY (space_role_id) REFERENCES Space_Role (id)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Operations & Donor Management
CREATE TABLE IF NOT EXISTS Donor_List (
    id INT NOT NULL AUTO_INCREMENT,
    monastery_space_id INT NULL,
    donor_id INT NULL,
    donor_name VARCHAR(255) NULL,
    donation_type VARCHAR(100) NULL,
    donation_type_value INT NOT NULL DEFAULT 0,
    custom_donation_type VARCHAR(255) NULL,
    note VARCHAR(1000) NULL,
    amount DECIMAL(18, 2) NULL,
    quantity DECIMAL(18, 2) NULL,
    status VARCHAR(100) NULL,
    status_value INT NOT NULL DEFAULT 2,
    reviewer_id INT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reviewed_at DATETIME NULL,
    pickup_time DATETIME NULL,
    dropoff_time DATETIME NULL,
    completed_at DATETIME NULL,
    PRIMARY KEY (id),
    KEY IX_Donor_List_monastery_space_id (monastery_space_id),
    KEY IX_Donor_List_donor_id (donor_id),
    KEY IX_Donor_List_reviewer_id (reviewer_id),
    KEY IX_Donor_List_status_created_at (status_value, created_at),
    CONSTRAINT FK_Donor_List_Monastery_Space_monastery_space_id
        FOREIGN KEY (monastery_space_id) REFERENCES Monastery_Space (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Donor_List_User_donor_id
        FOREIGN KEY (donor_id) REFERENCES User (id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Donor_List_User_reviewer_id
        FOREIGN KEY (reviewer_id) REFERENCES User (id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Notifications & User Preferences
CREATE TABLE IF NOT EXISTS Notification (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NULL,
    type INT NOT NULL DEFAULT 2,
    message VARCHAR(1000) NULL,
    is_read TINYINT(1) NULL DEFAULT 0,
    is_deleted TINYINT(1) NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY IX_Notification_user_id_created_at (user_id, created_at),
    CONSTRAINT FK_Notification_User_user_id
        FOREIGN KEY (user_id) REFERENCES User (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Refresh_Token (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NULL,
    refresh_token VARCHAR(512) NULL,
    expires_at DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at DATETIME NULL,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Refresh_Token_user_id (user_id),
    CONSTRAINT FK_Refresh_Token_User_user_id
        FOREIGN KEY (user_id) REFERENCES User (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS User_Setting (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NULL,
    pickup_time DATETIME NULL,
    dropoff_time DATETIME NULL,
    pickup_notification_time DATETIME NULL,
    dropoff_notification_time DATETIME NULL,
    PRIMARY KEY (id),
    UNIQUE KEY UX_User_Setting_user_id (user_id),
    CONSTRAINT FK_User_Setting_User_user_id
        FOREIGN KEY (user_id) REFERENCES User (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
