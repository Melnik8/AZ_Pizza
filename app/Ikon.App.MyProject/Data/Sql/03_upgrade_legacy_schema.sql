-- Run only if you created an older DB without Prognos columns. Safe to re-run.
ALTER TABLE copd_patients ADD COLUMN IF NOT EXISTS smoking_cessation_or_nrt_last_5_years BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE copd_patients ADD COLUMN IF NOT EXISTS lung_health_check_or_spirometry_within_24_months BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE copd_patients ADD COLUMN IF NOT EXISTS deceased BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE copd_patients ADD COLUMN IF NOT EXISTS residence_in_pirkanmaa BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE copd_patients ADD COLUMN IF NOT EXISTS palliative_or_terminal_care BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE copd_patients ADD COLUMN IF NOT EXISTS has_comorbidity_modifier BOOLEAN NOT NULL DEFAULT FALSE;

