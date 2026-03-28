namespace Ikon.App.MyProject.Data;

using Ikon.App.MyProject.Clinical;

/// <summary>JSON envelope for <see cref="DemoPatientRecordRepository"/>.</summary>
public sealed class CopdDemoPatientsFile
{
    public List<CopdPatientRecord> Patients { get; init; } = [];
}
