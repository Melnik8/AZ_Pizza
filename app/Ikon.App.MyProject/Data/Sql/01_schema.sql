-- LungFirst / Kanta-aligned test schema (PostgreSQL)
-- See lungfirst-ai-flagging-spec.pdf — hard gates + P1–P5 + comorbidity modifier

CREATE TABLE IF NOT EXISTS copd_patients (
    patient_id VARCHAR(64) PRIMARY KEY,
    date_of_birth DATE NOT NULL,
    smoking VARCHAR(32) NOT NULL CHECK (smoking IN ('None', 'Current', 'ExSmoker', 'History')),
    has_copd_or_asthma_diagnosis BOOLEAN NOT NULL DEFAULT FALSE,
    salbutamol_prescription_count INT NOT NULL DEFAULT 0,
    lrti_antibiotic_courses_last_24_months INT NOT NULL DEFAULT 0,
    vague_respiratory_visit_codes_last_12_months INT NOT NULL DEFAULT 0,
    spirometry_ever_recorded BOOLEAN NOT NULL DEFAULT FALSE,
    smoking_cessation_or_nrt_last_5_years BOOLEAN NOT NULL DEFAULT FALSE,
    lung_health_check_or_spirometry_within_24_months BOOLEAN NOT NULL DEFAULT FALSE,
    deceased BOOLEAN NOT NULL DEFAULT FALSE,
    residence_in_pirkanmaa BOOLEAN NOT NULL DEFAULT TRUE,
    palliative_or_terminal_care BOOLEAN NOT NULL DEFAULT FALSE,
    has_comorbidity_modifier BOOLEAN NOT NULL DEFAULT FALSE,
    clinical_notes TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS idx_copd_patients_dob ON copd_patients (date_of_birth);
