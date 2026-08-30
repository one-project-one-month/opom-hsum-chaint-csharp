ALTER TABLE Donor_List
    ADD COLUMN donation_type_value INT NOT NULL DEFAULT 0,
    ADD COLUMN custom_donation_type VARCHAR(255) NULL,
    ADD COLUMN note VARCHAR(1000) NULL,
    ADD COLUMN amount DECIMAL(18, 2) NULL,
    ADD COLUMN quantity DECIMAL(18, 2) NULL,
    ADD COLUMN status_value INT NOT NULL DEFAULT 2,
    ADD COLUMN created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN reviewed_at DATETIME NULL,
    ADD COLUMN pickup_time DATETIME NULL,
    ADD COLUMN dropoff_time DATETIME NULL,
    ADD COLUMN completed_at DATETIME NULL;

ALTER TABLE Monastery_Member
    ADD COLUMN role_value INT NOT NULL DEFAULT 3;

UPDATE Monastery_Member
SET role_value = CASE LOWER(role)
    WHEN 'owner' THEN 0
    WHEN '0' THEN 0
    WHEN 'admin' THEN 1
    WHEN '1' THEN 1
    WHEN 'editor' THEN 2
    WHEN '2' THEN 2
    WHEN 'viewer' THEN 3
    WHEN '3' THEN 3
    ELSE 3
END;

ALTER TABLE Monastery_Member
    DROP COLUMN role;

ALTER TABLE Monastery_Member
    CHANGE COLUMN role_value role INT NOT NULL DEFAULT 3;
