namespace Ikon.App.MyProject.Data;

using Ikon.App.MyProject.Clinical;

/// <summary>Abstraction over EHR / warehouse — swap for PostgreSQL via AppDatabaseConnection in production.</summary>
public interface IPatientRecordRepository
{
    Task<IReadOnlyList<CopdPatientRecord>> GetAllPatientsAsync(CancellationToken cancellationToken = default);
}
