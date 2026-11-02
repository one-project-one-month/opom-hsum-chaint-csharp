-- One round of local testing data for HsumChaint.
-- Prerequisite: run 000_create_local_database.sql first.
-- All seeded users use password: Passw0rd!
--
-- Run with:
-- mysql -u root -p -P 3307 -h localhost < HsumChaint.Database/Scripts/001_seed_test_data.sql

USE hsumchaint_db;

-- 1. System Roles
INSERT INTO `Role` (id, name, is_deleted) VALUES
    (1, 'Admin', 0),
    (2, 'Monk', 0),
    (3, 'User', 0)
ON DUPLICATE KEY UPDATE name = VALUES(name), is_deleted = VALUES(is_deleted);

-- 2. System Permissions
INSERT INTO `Permission` (name, is_deleted) VALUES
    ('Roles.View', 0),
    ('Roles.Manage', 0),
    ('Roles.Assign', 0),
    ('Users.View', 0),
    ('Users.Manage', 0),
    ('Monastery.Create', 0),
    ('Monastery.View', 0),
    ('Monastery.Update', 0),
    ('Monastery.Delete', 0),
    ('Monastery.ManageMembers', 0),
    ('Donation.View', 0),
    ('Donation.Create', 0),
    ('Donation.Review', 0),
    ('Donation.Schedule', 0),
    ('Donation.Cancel', 0),
    ('Donation.Delete', 0),
    ('Users.Update', 0),
    ('Invitation.View', 0),
    ('Invitation.Respond', 0),
    ('Notification.View', 0),
    ('Notification.Manage', 0)
ON DUPLICATE KEY UPDATE is_deleted = 0;

-- Admin access is entirely data-driven, including any additional active permissions.
INSERT INTO Role_Permission (role_id, permission_id, is_deleted)
SELECT 1, id, 0 FROM `Permission` WHERE is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

INSERT INTO Role_Permission (role_id, permission_id, is_deleted)
SELECT 2, id, 0 FROM `Permission` WHERE name IN
('Monastery.Create', 'Monastery.View', 'Monastery.Update', 'Monastery.Delete', 'Monastery.ManageMembers',
 'Donation.View', 'Donation.Create', 'Donation.Review', 'Donation.Schedule', 'Donation.Cancel',
 'Users.Update', 'Invitation.View', 'Invitation.Respond', 'Notification.View', 'Notification.Manage') AND is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

INSERT INTO Role_Permission (role_id, permission_id, is_deleted)
SELECT 3, id, 0 FROM `Permission` WHERE name IN
('Monastery.View', 'Donation.View', 'Donation.Create', 'Donation.Cancel',
 'Users.Update', 'Invitation.View', 'Invitation.Respond', 'Notification.View', 'Notification.Manage') AND is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

-- 3. Space Roles
INSERT INTO Space_Role (id, name, description, is_deleted) VALUES
    (1, 'Owner', 'Full control of monastery space (Monk creator only)', 0),
    (2, 'Admin', 'Monastery manager/trustee (Edit info, manage members & donations)', 0),
    (3, 'Editor', 'Accountant/helper (Record donations, schedule events)', 0),
    (4, 'Viewer', 'General member/donor (View space info and schedules, request donation)', 0)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    description = VALUES(description),
    is_deleted = 0;

-- 4. Space Permissions
INSERT INTO Space_Permission (name, description, is_deleted) VALUES
    ('Space.View', 'View monastery space details and announcements', 0),
    ('Space.Update', 'Update monastery name, description, and contact info', 0),
    ('Space.Delete', 'Delete the monastery space (Owner only)', 0),
    ('Space.InviteMember', 'Invite new members to the monastery space', 0),
    ('Space.UpdateMemberRole', 'Update an existing member\'s space role', 0),
    ('Space.RemoveMember', 'Remove a member from the monastery space', 0),
    ('Space.TransferOwnership', 'Transfer ownership to another monk (Owner only)', 0),
    ('Donation.View', 'View donations inside this monastery space', 0),
    ('Donation.Create', 'Submit donation requests to this monastery', 0),
    ('Donation.Review', 'Accept, reject, or review donation requests', 0),
    ('Donation.Schedule', 'Schedule pickup/dropoff times for donations', 0),
    ('Donation.Cancel', 'Cancel scheduled or accepted donations', 0),
    ('Space.ViewMembers', 'View monastery space members list', 0)
ON DUPLICATE KEY UPDATE
    description = VALUES(description),
    is_deleted = 0;

-- 5. Space Role Permissions
-- Owner (1): Full access
INSERT INTO Space_Role_Permission (space_role_id, space_permission_id, is_deleted)
SELECT 1, id, 0 FROM Space_Permission WHERE is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

-- Admin (2): Space management and donations
INSERT INTO Space_Role_Permission (space_role_id, space_permission_id, is_deleted)
SELECT 2, id, 0 FROM Space_Permission WHERE name IN (
    'Space.View',
    'Space.Update',
    'Space.InviteMember',
    'Space.UpdateMemberRole',
    'Space.RemoveMember',
    'Space.ViewMembers',
    'Donation.View',
    'Donation.Create',
    'Donation.Review',
    'Donation.Schedule',
    'Donation.Cancel'
) AND is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

-- Editor (3): Donation operations
INSERT INTO Space_Role_Permission (space_role_id, space_permission_id, is_deleted)
SELECT 3, id, 0 FROM Space_Permission WHERE name IN (
    'Space.View',
    'Donation.View',
    'Donation.Create',
    'Donation.Review',
    'Donation.Schedule'
) AND is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

-- Viewer (4): View & request donation
INSERT INTO Space_Role_Permission (space_role_id, space_permission_id, is_deleted)
SELECT 4, id, 0 FROM Space_Permission WHERE name IN (
    'Space.View',
    'Donation.View',
    'Donation.Create'
) AND is_deleted = 0
ON DUPLICATE KEY UPDATE is_deleted = 0;

-- 6. Users
INSERT INTO `User`
    (id, name, phone, password, role_id, email, contact_phone, fcm_token, created_at, updated_at, is_deleted)
VALUES
    (1, 'Ashin Nanda', '09100000001', 'AQAAAAIAAYagAAAAENgQAA7XK8LSoUH9X2bHih50M4bRumRT+/zgkXuj2DRzg+ON/cVOYukm/pMQXoVT+w==', 2, 'ashin.nanda@hsumchaint.local', '09100000001', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (2, 'Ko Admin', '09100000002', 'AQAAAAIAAYagAAAAEDIpV3urEmFsT/sadx0glQu6ZQVPR2rBoaOrdj3OBmc0hdVzfIYOYHh972IzaIUUlg==', 1, 'admin@hsumchaint.local', '09100000002', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (3, 'Ma Donor One', '09100000003', 'AQAAAAIAAYagAAAAEDwKdJOjimPY1/Li3yxUzuxwkJ3NN1oqvSiniR+Z8jZ4sCkUfPuTKE2FSx5F1Pa+ZA==', 3, 'donor.one@hsumchaint.local', '09100000003', 'local-test-fcm-token-donor-one', UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (4, 'Ko Donor Two', '09100000004', 'AQAAAAIAAYagAAAAEJwZjGPdmtxmmoKCnEh1dS+i5Nq6sYn2y1aw9gswyUYQtYeYT/c5JNyDfAO2UVn+Nw==', 3, 'donor.two@hsumchaint.local', '09100000004', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (5, 'Ma Viewer', '09100000005', 'AQAAAAIAAYagAAAAENgQAA7XK8LSoUH9X2bHih50M4bRumRT+/zgkXuj2DRzg+ON/cVOYukm/pMQXoVT+w==', 3, 'viewer@hsumchaint.local', '09100000005', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    password = VALUES(password),
    role_id = VALUES(role_id),
    email = VALUES(email),
    contact_phone = VALUES(contact_phone),
    fcm_token = VALUES(fcm_token),
    is_deleted = VALUES(is_deleted);

-- 7. Monk Profile & Monastery Space
INSERT INTO MonkProfile
    (id, user_id, monastery_name, monastery_address)
VALUES
    (1, 1, 'Shwe Kyaung Monastery', 'No. 12, Mingalar Street, Yangon')
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    monastery_name = VALUES(monastery_name),
    monastery_address = VALUES(monastery_address);

INSERT INTO Monastery_Space
    (id, monastery_name, description, address, created_by_id)
VALUES
    (1, 'Shwe Kyaung Monastery', 'Local testing monastery for donation workflow.', 'No. 12, Mingalar Street, Yangon', 1)
ON DUPLICATE KEY UPDATE
    monastery_name = VALUES(monastery_name),
    description = VALUES(description),
    address = VALUES(address),
    created_by_id = VALUES(created_by_id);

-- 8. Monastery Members (using space_role_id)
INSERT INTO Monastery_Member
    (id, user_id, monastery_space_id, space_role_id, is_owner)
VALUES
    (1, 1, 1, 1, 1), -- Ashin Nanda: Owner
    (2, 2, 1, 2, 0), -- Ko Admin: Admin
    (3, 5, 1, 4, 0)  -- Ma Viewer: Viewer
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    monastery_space_id = VALUES(monastery_space_id),
    space_role_id = VALUES(space_role_id),
    is_owner = VALUES(is_owner);

-- 9. Invitations (using space_role_id)
INSERT INTO Invitation
    (id, monastery_space_id, invited_user_id, invited_by_id, space_role_id, status, created_at)
VALUES
    (1, 1, 4, 1, 4, 0, UTC_TIMESTAMP()), -- Ko Donor Two invited as Viewer (Pending)
    (2, 1, 5, 1, 4, 1, UTC_TIMESTAMP())  -- Ma Viewer invited as Viewer (Accepted)
ON DUPLICATE KEY UPDATE
    monastery_space_id = VALUES(monastery_space_id),
    invited_user_id = VALUES(invited_user_id),
    invited_by_id = VALUES(invited_by_id),
    space_role_id = VALUES(space_role_id),
    status = VALUES(status);

-- 10. Operations & Donor Management
INSERT INTO Donor_List
    (id, monastery_space_id, donor_id, donor_name, donation_type, donation_type_value, custom_donation_type, note, amount, quantity, status, status_value, reviewer_id, created_at, reviewed_at, pickup_time, dropoff_time, completed_at)
VALUES
    (1, 1, 3, 'Ma Donor One', 'Food', 1, NULL, 'Rice bags for weekly offering.', NULL, 10.00, 'PendingReview', 2, NULL, UTC_TIMESTAMP(), NULL, NULL, NULL, NULL),
    (2, 1, NULL, 'Anonymous Cash Donor', 'Money', 0, NULL, 'Manual cash donation recorded by admin.', 50000.00, NULL, 'Accepted', 3, 2, UTC_TIMESTAMP(), UTC_TIMESTAMP(), NULL, NULL, NULL),
    (3, 1, 4, 'Ko Donor Two', 'Medicine', 2, NULL, 'Basic medicine supplies.', NULL, 5.00, 'Scheduled', 4, 2, UTC_TIMESTAMP(), UTC_TIMESTAMP(), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 1 DAY), NULL, NULL),
    (4, 1, 3, 'Ma Donor One', 'Supplies', 4, NULL, 'Cleaning supplies completed donation.', NULL, 3.00, 'Completed', 5, 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 7 DAY), DATE_SUB(UTC_TIMESTAMP(), INTERVAL 6 DAY), DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 DAY), NULL, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 DAY))
ON DUPLICATE KEY UPDATE
    monastery_space_id = VALUES(monastery_space_id),
    donor_id = VALUES(donor_id),
    donor_name = VALUES(donor_name),
    donation_type = VALUES(donation_type),
    donation_type_value = VALUES(donation_type_value),
    custom_donation_type = VALUES(custom_donation_type),
    note = VALUES(note),
    amount = VALUES(amount),
    quantity = VALUES(quantity),
    status = VALUES(status),
    status_value = VALUES(status_value),
    reviewer_id = VALUES(reviewer_id),
    reviewed_at = VALUES(reviewed_at),
    pickup_time = VALUES(pickup_time),
    dropoff_time = VALUES(dropoff_time),
    completed_at = VALUES(completed_at);

-- 11. Notifications
INSERT INTO Notification
    (id, user_id, type, message, is_read, is_deleted, created_at)
VALUES
    (1, 1, 1, 'A new donation request is waiting for review.', 0, 0, UTC_TIMESTAMP()),
    (2, 3, 1, 'Your donation was accepted.', 0, 0, UTC_TIMESTAMP()),
    (3, 4, 1, 'Your donation pickup/dropoff schedule has been updated.', 0, 0, UTC_TIMESTAMP()),
    (4, 4, 0, 'You have a new monastery invitation.', 0, 0, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    type = VALUES(type),
    message = VALUES(message),
    is_read = VALUES(is_read),
    is_deleted = VALUES(is_deleted);

-- 12. Refresh Tokens
INSERT INTO Refresh_Token
    (id, user_id, refresh_token, expires_at, created_at, revoked_at)
VALUES
    (1, 1, 'local-owner-refresh-token', DATE_ADD(UTC_TIMESTAMP(), INTERVAL 7 DAY), UTC_TIMESTAMP(), NULL),
    (2, 2, 'local-admin-refresh-token', DATE_ADD(UTC_TIMESTAMP(), INTERVAL 7 DAY), UTC_TIMESTAMP(), NULL),
    (3, 3, 'local-donor-refresh-token', DATE_ADD(UTC_TIMESTAMP(), INTERVAL 7 DAY), UTC_TIMESTAMP(), NULL)
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    refresh_token = VALUES(refresh_token),
    expires_at = VALUES(expires_at),
    revoked_at = VALUES(revoked_at);

-- 13. User Settings
INSERT INTO User_Setting
    (id, user_id, pickup_time, dropoff_time, pickup_notification_time, dropoff_notification_time)
VALUES
    (1, 3, DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 9 HOUR), DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 17 HOUR), DATE_ADD(DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 8 HOUR), INTERVAL 30 MINUTE), DATE_ADD(DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 16 HOUR), INTERVAL 30 MINUTE)),
    (2, 4, DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 10 HOUR), DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 16 HOUR), DATE_ADD(DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 9 HOUR), INTERVAL 30 MINUTE), DATE_ADD(DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 15 HOUR), INTERVAL 30 MINUTE))
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    pickup_time = VALUES(pickup_time),
    dropoff_time = VALUES(dropoff_time),
    pickup_notification_time = VALUES(pickup_notification_time),
    dropoff_notification_time = VALUES(dropoff_notification_time);
