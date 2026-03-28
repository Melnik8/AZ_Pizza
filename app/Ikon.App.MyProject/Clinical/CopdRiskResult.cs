namespace Ikon.App.MyProject.Clinical;

/// <summary>Scoring output after gates — LungFirst bands &amp; queue hints.</summary>
public sealed class CopdRiskResult
{
    public required string PatientId { get; init; }
    public int Age { get; init; }

    /// <summary>Sum of P1–P5 before comorbidity modifier.</summary>
    public int ParameterScore { get; init; }

    /// <summary>After +0.5 comorbidity modifier and ceiling (max 7).</summary>
    public int FinalScore { get; init; }

    public bool ComorbidityModifierApplied { get; init; }
    public LungFirstBand Band { get; init; }
    public required string ActionLabel { get; init; }
    public required string Wave { get; init; }
    public required string Slot { get; init; }

    public bool RuleP1AgeSmokingOrCessation { get; init; }
    public bool RuleP2SalbutamolWithoutRespDx { get; init; }
    public bool RuleP3LrtiAntibiotics { get; init; }
    public bool RuleP4VagueRespiratoryVisits { get; init; }
    public bool RuleP5NoSpirometry { get; init; }
}

/// <summary>Step 3 score banding (LungFirst spec).</summary>
public enum LungFirstBand
{
    PassiveWatch,
    StandardInvite,
    PriorityInvite,
    UrgentPriority
}

/// <summary>Gate exclusion — no score.</summary>
public sealed class GateExcludedResult
{
    public required string PatientId { get; init; }
    public int Age { get; init; }
    public required string Reason { get; init; }
}
