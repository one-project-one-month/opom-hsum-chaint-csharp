-- Local MySQL setup for HsumChaint.
-- Run with:
-- mysql -u root -p -P 3307 -h localhost < HsumChaint.Database/Scripts/000_create_local_database.sql

CREATE DATABASE IF NOT EXISTS hsumchaint_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE hsumchaint_db;

CREATE TABLE IF NOT EXISTS `User` (
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(255) NOT NULL,
    phone VARCHAR(50) NOT NULL,
    password VARCHAR(255) NOT NULL,
    user_type INT NOT NULL DEFAULT 0,
    email VARCHAR(255) NULL,
    contact_phone VARCHAR(50) NULL,
    fcm_token VARCHAR(255) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_User_phone_active (phone, is_deleted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS MonkProfile (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NOT NULL,
    monastery_name VARCHAR(255) NULL,
    monastery_address VARCHAR(500) NULL,
    PRIMARY KEY (id),
    KEY IX_MonkProfile_user_id (user_id),
    CONSTRAINT FK_MonkProfile_User_user_id
        FOREIGN KEY (user_id) REFERENCES `User` (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Monastery_Space (
    id INT NOT NULL AUTO_INCREMENT,
    monastery_name VARCHAR(255) NULL,
    description VARCHAR(1000) NULL,
    address VARCHAR(500) NULL,
    created_by_id INT NULL,
    PRIMARY KEY (id),
    KEY IX_Monastery_Space_created_by_id (created_by_id),
    CONSTRAINT FK_Monastery_Space_User_created_by_id
        FOREIGN KEY (created_by_id) REFERENCES `User` (id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Monastery_Member (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NULL,
    monastery_space_id INT NULL,
    role INT NOT NULL DEFAULT 3,
    isOwner TINYINT(1) NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY UX_Monastery_Member_user_space (user_id, monastery_space_id),
    KEY IX_Monastery_Member_monastery_space_id (monastery_space_id),
    CONSTRAINT FK_Monastery_Member_User_user_id
        FOREIGN KEY (user_id) REFERENCES `User` (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Monastery_Member_Monastery_Space_monastery_space_id
        FOREIGN KEY (monastery_space_id) REFERENCES Monastery_Space (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Invitation (
    id INT NOT NULL AUTO_INCREMENT,
    monastery_space_id INT NULL,
    invited_user_id INT NULL,
    invited_by_id INT NULL,
    role INT NOT NULL DEFAULT 3,
    status INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY IX_Invitation_monastery_space_id (monastery_space_id),
    KEY IX_Invitation_invited_user_id (invited_user_id),
    KEY IX_Invitation_invited_by_id (invited_by_id),
    CONSTRAINT FK_Invitation_Monastery_Space_monastery_space_id
        FOREIGN KEY (monastery_space_id) REFERENCES Monastery_Space (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Invitation_User_invited_user_id
        FOREIGN KEY (invited_user_id) REFERENCES `User` (id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Invitation_User_invited_by_id
        FOREIGN KEY (invited_by_id) REFERENCES `User` (id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
        FOREIGN KEY (donor_id) REFERENCES `User` (id)
        ON DELETE SET NULL,
    CONSTRAINT FK_Donor_List_User_reviewer_id
        FOREIGN KEY (reviewer_id) REFERENCES `User` (id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Notification (
    id INT NOT NULL AUTO_INCREMENT,
    user_id INT NULL,
    type INT NOT NULL DEFAULT 2,
    message VARCHAR(1000) NULL,
    isRead TINYINT(1) NULL DEFAULT 0,
    isDelete TINYINT(1) NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY IX_Notification_user_id_created_at (user_id, created_at),
    CONSTRAINT FK_Notification_User_user_id
        FOREIGN KEY (user_id) REFERENCES `User` (id)
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
        FOREIGN KEY (user_id) REFERENCES `User` (id)
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
        FOREIGN KEY (user_id) REFERENCES `User` (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
