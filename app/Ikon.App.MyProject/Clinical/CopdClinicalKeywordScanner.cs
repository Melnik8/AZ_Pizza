using System.Linq;

namespace Ikon.App.MyProject.Clinical;

/// <summary>Free-text hints (Finnish + English) — confirmatory / catch sparse records per Prognos spec.</summary>
public static class CopdClinicalKeywordScanner
{
    private static readonly string[] SmokingKeywords =
    [
        "smoker", "smoking", "cigarette", "tobacco", "ex-smoker", "ex smoker", "pack-year", "pack years",
        "tupakointi", "tupakoitsija", "tupakoi", "entinen tupakoitsija", "askivuodet", "nuuska"
    ];

    private static readonly string[] SalbutamolKeywords =
    [
        "salbutamol", "ventolin", "ventoline", "albuterol", "saba", "r03ac02", "buventol", "airomir"
    ];

    private static readonly string[] CessationKeywords =
    [
        "nicorette", "nicotinell", "champix", "varenicline", "nrt", "zyban", "bupropion",
        "nikotiini", "lopettanut tupakoinnin"
    ];

    private static readonly string[] ComorbidityKeywords =
    [
        "heart failure", "sydämen vajaatoiminta", "ischaemic heart", "sepelvaltimotauti", "diabetes",
        "tyypin 2 diabetes", "atrial fibrillation", "eteisvärinä", "sleep apnoea", "uniapnea",
        "osteoporosis", "osteoporoosi"
    ];

    /// <summary>Merges keyword hints into structured fields when the record is sparse.</summary>
    public static CopdPatientRecord MergeKeywords(CopdPatientRecord r)
    {
        var notes = r.ClinicalNotes ?? "";
        var lower = notes.ToLowerInvariant();

        var smoking = r.Smoking;
        if (smoking == SmokingStatus.None && SmokingKeywords.Any(k => lower.Contains(k, StringComparison.Ordinal)))
        {
            smoking = SmokingStatus.History;
        }

        var salbutamolCount = r.SalbutamolPrescriptionCount;
        if (salbutamolCount < 1 && SalbutamolKeywords.Any(k => lower.Contains(k, StringComparison.Ordinal)))
        {
            salbutamolCount = 1;
        }

        var cessation = r.SmokingCessationOrNrtLast5Years;
        if (!cessation && CessationKeywords.Any(k => lower.Contains(k, StringComparison.Ordinal)))
        {
            cessation = true;
        }

        var comorb = r.HasComorbidityModifier;
        if (!comorb && ComorbidityKeywords.Any(k => lower.Contains(k, StringComparison.Ordinal)))
        {
            comorb = true;
        }

        return new CopdPatientRecord
        {
            PatientId = r.PatientId ?? "",
            DateOfBirth = r.DateOfBirth,
            Smoking = smoking,
            HasCopdOrAsthmaDiagnosis = r.HasCopdOrAsthmaDiagnosis,
            SalbutamolPrescriptionCount = salbutamolCount,
            LrtiAntibioticCoursesLast24Months = r.LrtiAntibioticCoursesLast24Months,
            VagueRespiratoryVisitCodesLast12Months = r.VagueRespiratoryVisitCodesLast12Months,
            SpirometryEverRecorded = r.SpirometryEverRecorded,
            SmokingCessationOrNrtLast5Years = cessation,
            LungHealthCheckOrSpirometryWithin24Months = r.LungHealthCheckOrSpirometryWithin24Months,
            Deceased = r.Deceased,
            ResidenceInPirkanmaa = r.ResidenceInPirkanmaa,
            PalliativeOrTerminalCare = r.PalliativeOrTerminalCare,
            HasComorbidityModifier = comorb,
            ClinicalNotes = r.ClinicalNotes ?? ""
        };
    }
}

