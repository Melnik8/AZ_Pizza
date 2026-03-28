namespace Ikon.App.MyProject.Data;

using System.Data.Common;
using Ikon.App.MyProject.Clinical;
using Npgsql;

/// <summary>Loads patients from PostgreSQL — Ikon-allocated DB or local Docker (see Data/Sql).</summary>
public sealed class PostgresPatientRecordRepository : IPatientRecordRepository
{
    private readonly string _connectionString;

    public PostgresPatientRecordRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<CopdPatientRecord>> GetAllPatientsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT patient_id, date_of_birth, smoking, has_copd_or_asthma_diagnosis,
                   salbutamol_prescription_count, lrti_antibiotic_courses_last_24_months,
                   vague_respiratory_visit_codes_last_12_months, spirometry_ever_recorded,
                   smoking_cessation_or_nrt_last_5_years, lung_health_check_or_spirometry_within_24_months,
                   deceased, residence_in_pirkanmaa, palliative_or_terminal_care, has_comorbidity_modifier,
                   clinical_notes
            FROM copd_patients
            ORDER BY patient_id
            """;

        var list = new List<CopdPatientRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(Map(reader));
        }

        return list;
    }

    private static CopdPatientRecord Map(DbDataReader r)
    {
        var smoking = Enum.Parse<SmokingStatus>(r.GetString(r.GetOrdinal("smoking")), ignoreCase: false);
        var dob = DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("date_of_birth")));

        return new CopdPatientRecord
        {
            PatientId = r.GetString(r.GetOrdinal("patient_id")),
            DateOfBirth = dob,
            Smoking = smoking,
            HasCopdOrAsthmaDiagnosis = r.GetBoolean(r.GetOrdinal("has_copd_or_asthma_diagnosis")),
            SalbutamolPrescriptionCount = r.GetInt32(r.GetOrdinal("salbutamol_prescription_count")),
            LrtiAntibioticCoursesLast24Months = r.GetInt32(r.GetOrdinal("lrti_antibiotic_courses_last_24_months")),
            VagueRespiratoryVisitCodesLast12Months = r.GetInt32(r.GetOrdinal("vague_respiratory_visit_codes_last_12_months")),
            SpirometryEverRecorded = r.GetBoolean(r.GetOrdinal("spirometry_ever_recorded")),
            SmokingCessationOrNrtLast5Years = GetBool(r, "smoking_cessation_or_nrt_last_5_years"),
            LungHealthCheckOrSpirometryWithin24Months = GetBool(r, "lung_health_check_or_spirometry_within_24_months"),
            Deceased = GetBool(r, "deceased"),
            ResidenceInPirkanmaa = GetBool(r, "residence_in_pirkanmaa", defaultValue: true),
            PalliativeOrTerminalCare = GetBool(r, "palliative_or_terminal_care"),
            HasComorbidityModifier = GetBool(r, "has_comorbidity_modifier"),
            ClinicalNotes = r.IsDBNull(r.GetOrdinal("clinical_notes")) ? "" : r.GetString(r.GetOrdinal("clinical_notes"))
        };
    }

    private static bool GetBool(DbDataReader r, string name, bool defaultValue = false)
    {
        var i = r.GetOrdinal(name);
        return r.IsDBNull(i) ? defaultValue : r.GetBoolean(i);
    }
}
