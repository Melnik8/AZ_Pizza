namespace Ikon.App.MyProject.Data;

using System.Text.Json;
using System.Text.Json.Serialization;
using Ikon.App.MyProject.Clinical;

/// <summary>Loads demo cohort from JSON — no network; suitable for offline CDS prototype.</summary>
public sealed class DemoPatientRecordRepository : IPatientRecordRepository
{
    private readonly string _jsonPath;

    public DemoPatientRecordRepository(JsonSerializerOptions? options = null)
    {
        Options = options ?? CreateOptions();
        var baseDir = AppContext.BaseDirectory;
        _jsonPath = Path.Combine(baseDir, "Data", "copd_demo_patients.json");
    }

    private JsonSerializerOptions Options { get; }

    public async Task<IReadOnlyList<CopdPatientRecord>> GetAllPatientsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_jsonPath))
        {
            Log.Instance.Warning($"Demo data not found at {_jsonPath}");
            return [];
        }

        await using var stream = File.OpenRead(_jsonPath);
        var root = await JsonSerializer.DeserializeAsync<CopdDemoPatientsFile>(stream, Options, cancellationToken).ConfigureAwait(false);
        return root?.Patients ?? [];
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }
}
