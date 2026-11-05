-- Migration: Re-index DonationStatus to remove unused Draft(0) and Submitted(1)
-- Mapping:
--   Old 0 (Draft), 1 (Submitted), 2 (PendingReview) -> 0 (PendingReview)
--   Old 3 (Accepted) -> 1 (Accepted)
--   Old 4 (Scheduled) -> 2 (Scheduled)
--   Old 5 (Completed) -> 3 (Completed)
--   Old 6 (Rejected) -> 4 (Rejected)
--   Old 7 (Cancelled) -> 5 (Cancelled)

UPDATE Donor_List
SET status_value = CASE status_value
    WHEN 0 THEN 0
    WHEN 1 THEN 0
    WHEN 2 THEN 0
    WHEN 3 THEN 1
    WHEN 4 THEN 2
    WHEN 5 THEN 3
    WHEN 6 THEN 4
    WHEN 7 THEN 5
    ELSE 0
END,
status = CASE
    WHEN status IN ('Draft', 'Submitted') THEN 'PendingReview'
    ELSE status
END;

ALTER TABLE Donor_List
    ALTER COLUMN status_value SET DEFAULT 0;
