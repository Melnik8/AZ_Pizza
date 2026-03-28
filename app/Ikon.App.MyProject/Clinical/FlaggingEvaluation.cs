namespace Ikon.App.MyProject.Clinical;

/// <summary>Either excluded at hard gate or fully scored.</summary>
public sealed class FlaggingEvaluation
{
    public required string PatientId { get; init; }
    public int Age { get; init; }
    public GateExcludedResult? Excluded { get; init; }
    public CopdRiskResult? Scored { get; init; }
}
