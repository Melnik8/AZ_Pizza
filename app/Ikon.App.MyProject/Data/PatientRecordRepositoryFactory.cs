namespace Ikon.App.MyProject.Data;

using System.Linq;

/// <summary>Prefers PostgreSQL when configured; otherwise bundled JSON demo.</summary>
public static class PatientRecordRepositoryFactory
{
    public const string DatabaseLogicalName = "copd";

    /// <summary>
    /// Order: <c>COPD_PG_CONNECTION</c> env (local Docker/tests), then Ikon <see cref="IAppBase.Databases"/> entry
    /// named <see cref="DatabaseLogicalName"/>, else <see cref="DemoPatientRecordRepository"/>.
    /// </summary>
    public static IPatientRecordRepository Create(IAppBase host)
    {
        var env = Environment.GetEnvironmentVariable("COPD_PG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(env))
        {
            Log.Instance.Info("COPD CDS: using PostgreSQL from COPD_PG_CONNECTION.");
            return new PostgresPatientRecordRepository(env);
        }

        var allocated = host.Databases?.FirstOrDefault(d =>
            string.Equals(d.Name, DatabaseLogicalName, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(d.ConnectionString));

        if (allocated != null)
        {
            Log.Instance.Info($"COPD CDS: using PostgreSQL '{DatabaseLogicalName}' from app host.");
            return new PostgresPatientRecordRepository(allocated.ConnectionString);
        }

        Log.Instance.Info("COPD CDS: no PostgreSQL configured — using bundled JSON demo data.");
        return new DemoPatientRecordRepository();
    }
}
