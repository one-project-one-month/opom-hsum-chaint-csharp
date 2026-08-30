-- One round of local testing data for HsumChaint.
-- Prerequisite: run 000_create_local_database.sql first.
-- All seeded users use password: Passw0rd!
--
-- Run with:
-- mysql -u root -p -P 3307 -h localhost < HsumChaint.Database/Scripts/001_seed_test_data.sql

USE hsumchaint_db;

INSERT INTO `User`
    (id, name, phone, password, user_type, email, contact_phone, fcm_token, created_at, updated_at, is_deleted)
VALUES
    (1, 'Ashin Nanda', '09100000001', 'AQAAAAIAAYagAAAAENgQAA7XK8LSoUH9X2bHih50M4bRumRT+/zgkXuj2DRzg+ON/cVOYukm/pMQXoVT+w==', 1, 'ashin.nanda@hsumchaint.local', '09100000001', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (2, 'Ko Admin', '09100000002', 'AQAAAAIAAYagAAAAEDIpV3urEmFsT/sadx0glQu6ZQVPR2rBoaOrdj3OBmc0hdVzfIYOYHh972IzaIUUlg==', 0, 'admin@hsumchaint.local', '09100000002', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (3, 'Ma Donor One', '09100000003', 'AQAAAAIAAYagAAAAEDwKdJOjimPY1/Li3yxUzuxwkJ3NN1oqvSiniR+Z8jZ4sCkUfPuTKE2FSx5F1Pa+ZA==', 0, 'donor.one@hsumchaint.local', '09100000003', 'local-test-fcm-token-donor-one', UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (4, 'Ko Donor Two', '09100000004', 'AQAAAAIAAYagAAAAEJwZjGPdmtxmmoKCnEh1dS+i5Nq6sYn2y1aw9gswyUYQtYeYT/c5JNyDfAO2UVn+Nw==', 0, 'donor.two@hsumchaint.local', '09100000004', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0),
    (5, 'Ma Viewer', '09100000005', 'AQAAAAIAAYagAAAAENgQAA7XK8LSoUH9X2bHih50M4bRumRT+/zgkXuj2DRzg+ON/cVOYukm/pMQXoVT+w==', 0, 'viewer@hsumchaint.local', '09100000005', NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 0)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    password = VALUES(password),
    user_type = VALUES(user_type),
    email = VALUES(email),
    contact_phone = VALUES(contact_phone),
    fcm_token = VALUES(fcm_token),
    is_deleted = VALUES(is_deleted);

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

INSERT INTO Monastery_Member
    (id, user_id, monastery_space_id, role, isOwner)
VALUES
    (1, 1, 1, 0, 1),
    (2, 2, 1, 1, 0),
    (3, 5, 1, 3, 0)
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    monastery_space_id = VALUES(monastery_space_id),
    role = VALUES(role),
    isOwner = VALUES(isOwner);

INSERT INTO Invitation
    (id, monastery_space_id, invited_user_id, invited_by_id, role, status, created_at)
VALUES
    (1, 1, 4, 1, 3, 0, UTC_TIMESTAMP()),
    (2, 1, 5, 1, 3, 1, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    monastery_space_id = VALUES(monastery_space_id),
    invited_user_id = VALUES(invited_user_id),
    invited_by_id = VALUES(invited_by_id),
    role = VALUES(role),
    status = VALUES(status);

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

INSERT INTO Notification
    (id, user_id, type, message, isRead, isDelete, created_at)
VALUES
    (1, 1, 1, 'A new donation request is waiting for review.', 0, 0, UTC_TIMESTAMP()),
    (2, 3, 1, 'Your donation was accepted.', 0, 0, UTC_TIMESTAMP()),
    (3, 4, 1, 'Your donation pickup/dropoff schedule has been updated.', 0, 0, UTC_TIMESTAMP()),
    (4, 4, 0, 'You have a new monastery invitation.', 0, 0, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    type = VALUES(type),
    message = VALUES(message),
    isRead = VALUES(isRead),
    isDelete = VALUES(isDelete);

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
