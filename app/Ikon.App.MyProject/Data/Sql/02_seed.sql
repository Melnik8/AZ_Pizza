-- Synthetic test cohort — not real patients
TRUNCATE copd_patients;

INSERT INTO copd_patients (
    patient_id, date_of_birth, smoking, has_copd_or_asthma_diagnosis,
    salbutamol_prescription_count, lrti_antibiotic_courses_last_24_months,
    vague_respiratory_visit_codes_last_12_months, spirometry_ever_recorded,
    smoking_cessation_or_nrt_last_5_years, lung_health_check_or_spirometry_within_24_months,
    deceased, residence_in_pirkanmaa, palliative_or_terminal_care, has_comorbidity_modifier,
    clinical_notes
) VALUES
(
    'DB-2001', '1975-03-12', 'Current', FALSE,
    3, 2, 3, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE,
    'Salbutamol repeats; cough visits; no spirometry.'
),
(
    'DB-2002', '1988-07-01', 'None', FALSE,
    1, 0, 2, FALSE, TRUE, FALSE, FALSE, TRUE, FALSE, TRUE,
    'Nicorette course; Ventolin; URTI codes. Diabetes on record.'
),
(
    'DB-2003', '1962-11-20', 'ExSmoker', TRUE,
    0, 1, 1, TRUE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE,
    'G2: asthma on register — gate exit demo.'
),
(
    'DB-2004', '1955-01-05', 'History', FALSE,
    2, 2, 2, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE,
    'Smoking history; chest infections; antibiotics.'
),
(
    'DB-2005', '2005-04-18', 'None', FALSE,
    0, 0, 0, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE,
    'G1: under 40.'
),
(
    'DB-2006', '1960-06-01', 'Current', FALSE,
    1, 0, 0, FALSE, FALSE, TRUE, FALSE, TRUE, FALSE, FALSE,
    'G3: lung health check within 24 months — gate exit.'
),
(
    'DB-2007', '1958-09-15', 'ExSmoker', FALSE,
    0, 0, 2, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE,
    'Borderline: vague visits only; no salbutamol.'
);
