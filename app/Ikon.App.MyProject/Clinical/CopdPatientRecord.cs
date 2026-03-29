namespace Ikon.App.MyProject.Clinical;

/// <summary>
/// Kanta-aligned snapshot for Prognos flagging — see prognos-ai-flagging-spec.pdf.
/// Invites only; never diagnoses.
/// </summary>
public sealed class CopdPatientRecord
{
    public string PatientId { get; init; } = "";
    public DateOnly DateOfBirth { get; init; }

    /// <summary>Structured smoking status from coded records.</summary>
    public SmokingStatus Smoking { get; init; }

    /// <summary>J44.x / J45.x anywhere — triggers hard gate G2 (monitoring track, not screening).</summary>
    public bool HasCopdOrAsthmaDiagnosis { get; init; }

    /// <summary>≥1 salbutamol / SABA dispensed (lifetime).</summary>
    public int SalbutamolPrescriptionCount { get; init; }

    /// <summary>Antibiotic courses for LRTI in last 24 months (co-coded with respiratory).</summary>
    public int LrtiAntibioticCoursesLast24Months { get; init; }

    /// <summary>GP visits with vague respiratory codes in last 12 months.</summary>
    public int VagueRespiratoryVisitCodesLast12Months { get; init; }

    /// <summary>Any spirometry / lung function procedure on lifetime record (TUOHF-class).</summary>
    public bool SpirometryEverRecorded { get; init; }

    /// <summary>P1: NRT / varenicline / bupropion in last 5 years (ATC N07BA*, etc.).</summary>
    public bool SmokingCessationOrNrtLast5Years { get; init; }

    /// <summary>G3: Lung health check or spirometry within 24 months.</summary>
    public bool LungHealthCheckOrSpirometryWithin24Months { get; init; }

    /// <summary>G4: population register.</summary>
    public bool Deceased { get; init; }

    /// <summary>G4: Pirkanmaa pilot — default true for demo.</summary>
    public bool ResidenceInPirkanmaa { get; init; } = true;

    /// <summary>G5: palliative / terminal.</summary>
    public bool PalliativeOrTerminalCare { get; init; }

    /// <summary>Comorbidity modifier (I50, I25, E11, …) — +0.5 rounded up if parameter score ≥2.</summary>
    public bool HasComorbidityModifier { get; init; }

    /// <summary>Free-text for keyword augmentation (Finnish + English per spec).</summary>
    public string ClinicalNotes { get; init; } = "";
}

public enum SmokingStatus
{
    None,
    Current,
    ExSmoker,
    History
}

