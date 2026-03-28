namespace Ikon.App.MyProject.Clinical;

/// <summary>LungFirst algorithm — Step 2 scoring + Step 3 banding (spec PDF). No black-box.</summary>
public static class CopdRiskScorer
{
    public static FlaggingEvaluation Evaluate(CopdPatientRecord raw)
    {
        var r = CopdClinicalKeywordScanner.MergeKeywords(raw);
        var age = AgeInYears(r.DateOfBirth);

        var gate = HardGateEvaluator.GetFailureReason(r, age);
        if (gate != null)
        {
            return new FlaggingEvaluation
            {
                PatientId = r.PatientId,
                Age = age,
                Excluded = new GateExcludedResult { PatientId = r.PatientId, Age = age, Reason = gate }
            };
        }

        var p1 = age >= 40
                 && (r.Smoking != SmokingStatus.None || r.SmokingCessationOrNrtLast5Years);
        var p2 = r.SalbutamolPrescriptionCount >= 1;
        var p3 = r.LrtiAntibioticCoursesLast24Months >= 2;
        var p4 = r.VagueRespiratoryVisitCodesLast12Months >= 2;

        var scoreBeforeP5 = 0;
        if (p1)
        {
            scoreBeforeP5 += 2;
        }

        if (p2)
        {
            scoreBeforeP5 += 2;
        }

        if (p3)
        {
            scoreBeforeP5 += 1;
        }

        if (p4)
        {
            scoreBeforeP5 += 1;
        }

        scoreBeforeP5 = Math.Clamp(scoreBeforeP5, 0, 7);

        var p5 = scoreBeforeP5 > 0 && !r.SpirometryEverRecorded;
        var parameterScore = p5 ? Math.Min(7, scoreBeforeP5 + 1) : scoreBeforeP5;

        var comorbidityApplied = r.HasComorbidityModifier && parameterScore >= 2;
        var adjusted = parameterScore + (comorbidityApplied ? 0.5 : 0);
        var finalScore = comorbidityApplied
            ? (int)Math.Min(7, Math.Ceiling(adjusted))
            : parameterScore;

        var (band, action, wave, slot) = MapBand(finalScore);

        var scored = new CopdRiskResult
        {
            PatientId = r.PatientId,
            Age = age,
            ParameterScore = parameterScore,
            FinalScore = finalScore,
            ComorbidityModifierApplied = comorbidityApplied,
            Band = band,
            ActionLabel = action,
            Wave = wave,
            Slot = slot,
            RuleP1AgeSmokingOrCessation = p1,
            RuleP2SalbutamolWithoutRespDx = p2,
            RuleP3LrtiAntibiotics = p3,
            RuleP4VagueRespiratoryVisits = p4,
            RuleP5NoSpirometry = p5
        };

        return new FlaggingEvaluation
        {
            PatientId = r.PatientId,
            Age = age,
            Scored = scored
        };
    }

    private static (LungFirstBand Band, string Action, string Wave, string Slot) MapBand(int finalScore)
    {
        if (finalScore <= 2)
        {
            return (
                LungFirstBand.PassiveWatch,
                "No action — passive watch; re-score in 6 months",
                "—",
                "—");
        }

        if (finalScore == 3)
        {
            return (
                LungFirstBand.StandardInvite,
                "Standard invite — Wave 2 queue, 20 min slot; OmaKanta 3-touch if no response",
                "Wave 2",
                "20 min standard");
        }

        if (finalScore is >= 4 and <= 5)
        {
            return (
                LungFirstBand.PriorityInvite,
                "Priority invite — Wave 1, 30 min, sooner date; OmaKanta + SMS; GP passive notification",
                "Wave 1",
                "30 min priority");
        }

        return (
            LungFirstBand.UrgentPriority,
            "Urgent priority — Wave 1 immediate; GP actively notified; coordinator call if no booking in 7 days",
            "Wave 1",
            "30 min urgent");
    }

    private static int AgeInYears(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age))
        {
            age--;
        }

        return Math.Max(0, age);
    }
}
