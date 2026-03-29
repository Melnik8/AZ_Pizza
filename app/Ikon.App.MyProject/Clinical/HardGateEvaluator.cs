namespace Ikon.App.MyProject.Clinical;

/// <summary>Step 1 — hard gates before any scoring (Prognos spec).</summary>
public static class HardGateEvaluator
{
    /// <summary>Returns null if all gates pass; otherwise first failing gate reason for audit.</summary>
    public static string? GetFailureReason(CopdPatientRecord r, int ageYears)
    {
        if (ageYears < 40)
        {
            return "G1: Age under 40 — not in screening cohort";
        }

        if (r.HasCopdOrAsthmaDiagnosis)
        {
            return "G2: COPD (J44) or asthma (J45) on record — monitoring track, not screening";
        }

        if (r.LungHealthCheckOrSpirometryWithin24Months)
        {
            return "G3: Lung health check or spirometry within 24 months";
        }

        if (r.Deceased)
        {
            return "G4: Deceased";
        }

        if (!r.ResidenceInPirkanmaa)
        {
            return "G4: Residence outside Pirkanmaa pilot";
        }

        if (r.PalliativeOrTerminalCare)
        {
            return "G5: Palliative / terminal care — not appropriate to contact";
        }

        return null;
    }
}

