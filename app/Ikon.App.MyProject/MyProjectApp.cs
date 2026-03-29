using System.Linq;
using Ikon.App.MyProject.Clinical;
using Ikon.App.MyProject.Data;

return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParameters(string Name = "Ikon");

[App]
public class MyProjectApp(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new Theme());

    private readonly IPatientRecordRepository _patients = PatientRecordRepositoryFactory.Create(app);
    private readonly ClientReactive<string> _currentTheme = new(Constants.LightTheme);
    private readonly ClientReactive<string> _currentLanguage = new("en");
    private readonly Reactive<bool> _isAnalyzing = new(false);
    private readonly Reactive<string?> _analysisError = new(null);
    private readonly Reactive<List<CopdRiskResult>> _scored = new([]);
    private readonly Reactive<List<GateExcludedResult>> _excluded = new([]);
    private readonly Reactive<HashSet<string>> _invitedPatients = new(new HashSet<string>());
    private readonly Reactive<bool> _hasAnalyzed = new(false);

    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en"] = new()
        {
            ["Title"] = "Prognos AI flagging (prototype)",
            ["AnalyzeDatabase"] = "Analyze database",
            ["Analyzing"] = "Analyzing…",
            ["ShowExampleQueue"] = "Show example queue",
            ["QueuedCount"] = "{0} queued",
            ["RunBatchAnalysis"] = "Run batch analysis. Output: priority-ranked invitation queue (bands), not a diagnosis.",
            ["ColorReference"] = "Color reference",
            ["RedDescription"] = "Red — score 6-7 urgent priority; Wave 1 immediate; GP actively notified; coordinator call if no booking in 7 days.",
            ["AmberDescription"] = "Amber — score 4-5 priority invite; 30 min, sooner date; OmaKanta + SMS; GP passive notification.",
            ["GreenDescription"] = "Green — score 3 standard invite; 20 min slot; OmaKanta + SMS.",
            ["InvitationQueueHeader"] = "Invitation queue (by final score)",
            ["Patient"] = "Patient",
            ["Age"] = "Age",
            ["Final"] = "Final",
            ["Band"] = "Band",
            ["WaveSlot"] = "Wave / slot",
            ["Details"] = "Details",
            ["Action"] = "Action",
            ["InvitationSent"] = "Invitation sent",
            ["Invite"] = "Invite",
            ["AnalysisFailed"] = "Analysis failed: {0}",
            ["RuleP1"] = "P1 age+smoking/NRT",
            ["RuleP2"] = "P2 salbutamol",
            ["RuleP3"] = "P3 LRTI abs",
            ["RuleP4"] = "P4 vague visits",
            ["RuleP5"] = "P5 no spirometry",
            ["ComorbidityModifier"] = "+0.5 comorbidity→ceil",
            ["BandPassiveWatch"] = "0–2 watch",
            ["BandStandardInvite"] = "3 standard",
            ["BandPriorityInvite"] = "4–5 priority",
            ["BandUrgentPriority"] = "6–7 urgent"
        },
        ["fi"] = new()
        {
            ["Title"] = "Prognos AI-luokitus (prototyyppi)",
            ["AnalyzeDatabase"] = "Analysoi tietokanta",
            ["Analyzing"] = "Analysoidaan…",
            ["ShowExampleQueue"] = "Näytä esimerkkijono",
            ["QueuedCount"] = "{0} jonossa",
            ["RunBatchAnalysis"] = "Suorita eräanalyysi. Tulostus: priorisoitu kutsujono (bändit), ei diagnoosi.",
            ["ColorReference"] = "Väriviite",
            ["RedDescription"] = "Punainen — pistemäärä 6-7 kiireellinen prioriteetti; aaltosarja 1 välitön; lääkäri ilmoitettu; koordinaattori soittaa, jos varausta ei tehdä 7 päivän sisällä.",
            ["AmberDescription"] = "Keltainen — pistemäärä 4-5 prioriteettikutsu; 30 min, nopeampi aika; OmaKanta + SMS; GP passiivinen ilmoitus.",
            ["GreenDescription"] = "Vihreä — pistemäärä 3 normaali kutsu; 20 min aika; OmaKanta + SMS.",
            ["InvitationQueueHeader"] = "Kutsujono (lopullisen pistemäärän mukaan)",
            ["Patient"] = "Potilas",
            ["Age"] = "Ikä",
            ["Final"] = "Lopullinen",
            ["Band"] = "Bändi",
            ["WaveSlot"] = "Aalto / aika",
            ["Details"] = "Tiedot",
            ["Action"] = "Toiminto",
            ["InvitationSent"] = "Kutsu lähetetty",
            ["Invite"] = "Kutsu",
            ["AnalysisFailed"] = "Analyysi epäonnistui: {0}",
            ["RuleP1"] = "P1 ikä+tupakointi/NRT",
            ["RuleP2"] = "P2 salbutamoli",
            ["RuleP3"] = "P3 antibiootit",
            ["RuleP4"] = "P4 epämääräiset käynnit",
            ["RuleP5"] = "P5 ei spirometriaa",
            ["ComorbidityModifier"] = "+0,5 liitännäissairaus→pyöristä ylöspäin",
            ["BandPassiveWatch"] = "0–2 seuranta",
            ["BandStandardInvite"] = "3 vakio",
            ["BandPriorityInvite"] = "4–5 prioriteetti",
            ["BandUrgentPriority"] = "6–7 kiireellinen"
        },
        ["sv"] = new()
        {
            ["Title"] = "Prognos AI-flagging (prototyp)",
            ["AnalyzeDatabase"] = "Analysera databas",
            ["Analyzing"] = "Analyserar…",
            ["ShowExampleQueue"] = "Visa exempelkö",
            ["QueuedCount"] = "{0} i kö",
            ["RunBatchAnalysis"] = "Kör batchanalys. Utdata: prioriterad inbjudningskö (band), inte en diagnos.",
            ["ColorReference"] = "Färgreferens",
            ["RedDescription"] = "Röd — poäng 6-7 brådskande prioritet; Våg 1 omedelbart; GP meddelas; koordinator ringer om ingen bokning på 7 dagar.",
            ["AmberDescription"] = "Gul — poäng 4-5 prioriterad inbjudan; 30 min, tidigare tid; OmaKanta + SMS; GP passiv avisering.",
            ["GreenDescription"] = "Grön — poäng 3 standardinbjudan; 20 min tid; OmaKanta + SMS.",
            ["InvitationQueueHeader"] = "Inbjudningskö (efter slutpoäng)",
            ["Patient"] = "Patient",
            ["Age"] = "Ålder",
            ["Final"] = "Slut",
            ["Band"] = "Band",
            ["WaveSlot"] = "Våg / tid",
            ["Details"] = "Detaljer",
            ["Action"] = "Åtgärd",
            ["InvitationSent"] = "Inbjudan skickad",
            ["Invite"] = "Bjud in",
            ["AnalysisFailed"] = "Analys misslyckades: {0}",
            ["RuleP1"] = "P1 ålder+rökning/NRT",
            ["RuleP2"] = "P2 salbutamol",
            ["RuleP3"] = "P3 antibiotika",
            ["RuleP4"] = "P4 vaga besök",
            ["RuleP5"] = "P5 ingen spirometri",
            ["ComorbidityModifier"] = "+0,5 komorbiditet→uppåt",
            ["BandPassiveWatch"] = "0–2 bevaka",
            ["BandStandardInvite"] = "3 standard",
            ["BandPriorityInvite"] = "4–5 prioritet",
            ["BandUrgentPriority"] = "6–7 brådskande"
        }
    };

    private string T(string key) => Translate(_currentLanguage.Value, key);

    private string T(string key, params object[] args) => string.Format(Translate(_currentLanguage.Value, key), args);

    private static string Translate(string language, string key)
    {
        if (_translations.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_translations.TryGetValue("en", out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public async Task Main()
    {
        app.ClientJoinedAsync += async args =>
        {
            if (!string.IsNullOrEmpty(args.ClientContext.Theme))
            {
                _currentTheme.Value = args.ClientContext.Theme == Constants.DarkTheme ? Constants.DarkTheme : Constants.LightTheme;
            }
        };

        UI.Root([Page.Default],
            content: view =>
            {
                view.Column(["h-screen overflow-hidden"], content: view =>
                {
                    view.Column([Container.Xl2, "px-4 py-4 flex-shrink-0 max-w-6xl mx-auto w-full"], content: view =>
                    {
                        view.Row([Layout.Row.SpaceBetween, "items-start gap-4 mb-2"], content: view =>
                        {
                            view.Column([Layout.Column.Xs], content: view =>
                            {
                                view.Text([Text.H2, "font-heading"], T("Title"));
                            });

                            view.Row(["items-center gap-2"], content: view =>
                            {
                                foreach (var lang in new[] { "fi", "sv", "en" })
                                {
                                    var isActive = _currentLanguage.Value == lang;
                                    view.Button(
                                        [Button.SecondaryMd, isActive ? "bg-accent text-accent-foreground" : ""],
                                        lang.ToUpperInvariant(),
                                        disabled: isActive,
                                        onClick: async () => { _currentLanguage.Value = lang; });
                                }

                                view.Button([Button.GhostMd, Button.Size.Icon],
                                    onClick: ToggleThemeAsync,
                                    content: v => v.Icon([Icon.Default], name: _currentTheme.Value == Constants.DarkTheme ? "sun" : "moon"));
                            });
                        });
                    });

                    view.Column(["flex-1 min-h-0 max-w-6xl mx-auto w-full px-4 pb-4"], content: view =>
                    {
                        view.Row(["gap-3 flex-wrap items-center mb-3 flex-shrink-0"], content: view =>
                        {
                            view.Button(
                                [Button.PrimaryMd],
                                _isAnalyzing.Value ? T("Analyzing") : T("AnalyzeDatabase"),
                                disabled: _isAnalyzing.Value,
                                onClick: async () => { await AnalyzeAsync(); });

                            view.Button(
                                [Button.SecondaryMd],
                                T("ShowExampleQueue"),
                                disabled: _isAnalyzing.Value,
                                onClick: async () => { await ShowExampleQueueAsync(); });

                            if (_hasAnalyzed.Value && !_isAnalyzing.Value)
                            {
                                view.Text([Text.Small, "text-muted-foreground"],
                                    T("QueuedCount", _scored.Value.Count));
                            }
                        });

                        if (_analysisError.Value != null)
                        {
                            view.Text([Text.Small, "text-red-600 mb-2"], _analysisError.Value);
                        }

                        view.ScrollArea(rootStyle: ["flex-1 min-h-0"], content: view =>
                        {
                            if (!_hasAnalyzed.Value)
                            {
                                view.Text([Text.Body, "text-muted-foreground"],
                                    T("RunBatchAnalysis"));
                                return;
                            }

                            view.Column([Layout.Column.Lg, "pb-4"], content: view =>
                            {
                                view.Box(["rounded-lg border border-border bg-muted/10 p-4 mb-4"], content: view =>
                                {
                                    view.Text([Text.H4, "font-heading mb-3"], T("ColorReference"));
                                    view.Column(["gap-2"], content: view =>
                                    {
                                        view.Row(["items-center gap-3"], content: view =>
                                        {
                                            view.Box(["h-3 w-3 rounded-full bg-red-500"]);
                                            view.Text(["text-sm"], T("RedDescription"));
                                        });
                                        view.Row(["items-center gap-3"], content: view =>
                                        {
                                            view.Box(["h-3 w-3 rounded-full bg-amber-500"]);
                                            view.Text(["text-sm"], T("AmberDescription"));
                                        });
                                        view.Row(["items-center gap-3"], content: view =>
                                        {
                                            view.Box(["h-3 w-3 rounded-full bg-emerald-500"]);
                                            view.Text(["text-sm"], T("GreenDescription"));
                                        });
                                    });
                                });

                                view.Text([Text.H3, "font-heading text-sm text-foreground"], T("InvitationQueueHeader"));
                                view.Column([Layout.Column.Sm], content: view =>
                                {
                                    view.Row([
                                            "gap-3 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-foreground border-b border-border flex-wrap"
                                        ],
                                        content: view =>
                                        {
                                            view.Text(["min-w-[7rem]"], T("Patient"));
                                            view.Text(["w-10"], T("Age"));
                                            view.Text(["w-14 text-center"], T("Final"));
                                            view.Text(["min-w-[6rem]"], T("Band"));
                                            view.Text(["min-w-[7rem]"], T("WaveSlot"));
                                            view.Text(["flex-1 min-w-[12rem]"], T("Details"));
                                            view.Text(["w-28 text-right"], T("Action"));
                                        });

                                    foreach (var row in _scored.Value)
                                    {
                                        var band = RowStyleForBand(row.Band);
                                        view.Column(["rounded-lg border p-3 text-sm gap-2", band], content: view =>
                                        {
                                            view.Row(["gap-3 flex-wrap items-start"], content: view =>
                                            {
                                                view.Text(["min-w-[7rem] font-mono font-semibold"], row.PatientId);
                                                view.Text(["w-10 font-medium tabular-nums"], $"{row.Age}");
                                                view.Column(["w-14 text-center"], content: view =>
                                                {
                                                    view.Text(["font-bold tabular-nums text-lg"], $"{row.FinalScore}");
                                                    if (row.ComorbidityModifierApplied)
                                                    {
                                                        view.Text(["text-xs opacity-90"], $"p={row.ParameterScore}");
                                                    }
                                                });
                                                view.Text(["min-w-[6rem] text-xs font-semibold"], BandLabel(row.Band));
                                                view.Column(["min-w-[7rem]"], content: view =>
                                                {
                                                    view.Text(["text-xs font-medium"], row.Wave);
                                                    view.Text(["text-xs"], row.Slot);
                                                });
                                                view.Column(["flex-1 min-w-[12rem] gap-1.5"], content: view =>
                                                {
                                                    view.Text(["text-sm font-medium leading-snug"], CleanActionLabel(row.ActionLabel));
                                                    view.Text(["text-xs font-medium leading-relaxed"], FormatRuleHits(row));
                                                });
                                                var invited = _invitedPatients.Value.Contains(row.PatientId);
                                                view.Column(["w-28 flex items-center justify-end"], content: view =>
                                                {
                                                    if (invited)
                                                    {
                                                        view.Button([Button.SecondaryMd, "w-full"], T("InvitationSent"), disabled: true);
                                                    }
                                                    else
                                                    {
                                                        view.Button([Button.PrimaryMd, "w-full"], T("Invite"),
                                                            onClick: async () =>
                                                            {
                                                                var updated = new HashSet<string>(_invitedPatients.Value)
                                                                {
                                                                    row.PatientId
                                                                };
                                                                _invitedPatients.Value = updated;
                                                            });
                                                    }
                                                });
                                            });
                                        });
                                    }
                                });
                            });
                        });
                    });
                });
            });
    }

    private static string CleanActionLabel(string actionLabel)
    {
        var marker = actionLabel.IndexOf('—');
        if (marker >= 0)
        {
            return actionLabel.Substring(0, marker).Trim();
        }

        return actionLabel.Trim();
    }

private string BandLabel(PrognosBand b) =>
            b switch
            {
                PrognosBand.PassiveWatch => T("BandPassiveWatch"),
                PrognosBand.StandardInvite => T("BandStandardInvite"),
                PrognosBand.PriorityInvite => T("BandPriorityInvite"),
                PrognosBand.UrgentPriority => T("BandUrgentPriority"),
                _ => ""
            };

    private string FormatRuleHits(CopdRiskResult r)
    {
        var parts = new List<string>();
        if (r.RuleP1AgeSmokingOrCessation)
        {
            parts.Add(T("RuleP1"));
        }

        if (r.RuleP2SalbutamolWithoutRespDx)
        {
            parts.Add(T("RuleP2"));
        }

        if (r.RuleP3LrtiAntibiotics)
        {
            parts.Add(T("RuleP3"));
        }

        if (r.RuleP4VagueRespiratoryVisits)
        {
            parts.Add(T("RuleP4"));
        }

        if (r.RuleP5NoSpirometry)
        {
            parts.Add(T("RuleP5"));
        }

        if (r.ComorbidityModifierApplied)
        {
            parts.Add(T("ComorbidityModifier"));
        }

        return parts.Count == 0 ? "—" : string.Join(" · ", parts);
    }

    private static string RowStyleForBand(PrognosBand band) =>
        band switch
        {
            PrognosBand.PassiveWatch =>
                "bg-emerald-50 border-emerald-200 text-emerald-950 dark:bg-emerald-950/50 dark:border-emerald-700 dark:text-zinc-100",
            PrognosBand.StandardInvite =>
                "bg-emerald-50 border-emerald-200 text-emerald-950 dark:bg-emerald-950/50 dark:border-emerald-700 dark:text-zinc-100",
            PrognosBand.PriorityInvite =>
                "bg-amber-50 border-amber-200 text-amber-950 dark:bg-amber-950/50 dark:border-amber-700 dark:text-zinc-100",
            PrognosBand.UrgentPriority =>
                "bg-red-50 border-red-200 text-red-950 dark:bg-red-950/50 dark:border-red-800 dark:text-zinc-100",
            _ => "bg-card border-border text-foreground"
        };

    private async Task ToggleThemeAsync()
    {
        var nextTheme = _currentTheme.Value == Constants.DarkTheme ? Constants.LightTheme : Constants.DarkTheme;
        var updated = await ClientFunctions.SetThemeAsync(nextTheme);

        if (updated)
        {
            _currentTheme.Value = nextTheme;
        }
    }

    private async Task ShowExampleQueueAsync()
    {
        if (_isAnalyzing.Value)
        {
            return;
        }

        _analysisError.Value = null;
        _scored.Value = GetExampleInviteQueue()
            .OrderByDescending(r => r.FinalScore)
            .ThenBy(r => r.PatientId)
            .ToList();
        _excluded.Value = new List<GateExcludedResult>();
        _invitedPatients.Value = new HashSet<string>();
        _hasAnalyzed.Value = true;

        await Task.CompletedTask;
    }

    private static List<CopdRiskResult> GetExampleInviteQueue() => new()
    {
        new CopdRiskResult
        {
            PatientId = "EX-2001",
            Age = 72,
            ParameterScore = 5,
            FinalScore = 6,
            ComorbidityModifierApplied = false,
            Band = PrognosBand.UrgentPriority,
            ActionLabel = "Urgent priority — Wave 1 immediate; GP actively notified; coordinator call if no booking in 7 days",
            Wave = "Wave 1",
            Slot = "30 min urgent",
            RuleP1AgeSmokingOrCessation = true,
            RuleP2SalbutamolWithoutRespDx = true,
            RuleP3LrtiAntibiotics = true,
            RuleP4VagueRespiratoryVisits = false,
            RuleP5NoSpirometry = false
        },
        new CopdRiskResult
        {
            PatientId = "EX-2002",
            Age = 68,
            ParameterScore = 6,
            FinalScore = 7,
            ComorbidityModifierApplied = true,
            Band = PrognosBand.UrgentPriority,
            ActionLabel = "Urgent priority — Wave 1 immediate; GP actively notified; coordinator call if no booking in 7 days",
            Wave = "Wave 1",
            Slot = "30 min urgent",
            RuleP1AgeSmokingOrCessation = true,
            RuleP2SalbutamolWithoutRespDx = true,
            RuleP3LrtiAntibiotics = true,
            RuleP4VagueRespiratoryVisits = true,
            RuleP5NoSpirometry = true
        },
        new CopdRiskResult
        {
            PatientId = "EX-2003",
            Age = 64,
            ParameterScore = 4,
            FinalScore = 4,
            ComorbidityModifierApplied = false,
            Band = PrognosBand.PriorityInvite,
            ActionLabel = "Priority invite — 30 min, sooner date; OmaKanta + SMS; GP passive notification",
            Wave = "Wave 1",
            Slot = "30 min priority",
            RuleP1AgeSmokingOrCessation = true,
            RuleP2SalbutamolWithoutRespDx = false,
            RuleP3LrtiAntibiotics = true,
            RuleP4VagueRespiratoryVisits = true,
            RuleP5NoSpirometry = false
        },
        new CopdRiskResult
        {
            PatientId = "EX-2004",
            Age = 57,
            ParameterScore = 4,
            FinalScore = 5,
            ComorbidityModifierApplied = false,
            Band = PrognosBand.PriorityInvite,
            ActionLabel = "Priority invite — 30 min, sooner date; OmaKanta + SMS; GP passive notification",
            Wave = "Wave 1",
            Slot = "30 min priority",
            RuleP1AgeSmokingOrCessation = false,
            RuleP2SalbutamolWithoutRespDx = true,
            RuleP3LrtiAntibiotics = false,
            RuleP4VagueRespiratoryVisits = true,
            RuleP5NoSpirometry = true
        },
        new CopdRiskResult
        {
            PatientId = "EX-2005",
            Age = 62,
            ParameterScore = 3,
            FinalScore = 3,
            ComorbidityModifierApplied = false,
            Band = PrognosBand.StandardInvite,
            ActionLabel = "Standard invite — 20 min slot; OmaKanta 3-touch if no response",
            Wave = "Wave 2",
            Slot = "20 min standard",
            RuleP1AgeSmokingOrCessation = true,
            RuleP2SalbutamolWithoutRespDx = false,
            RuleP3LrtiAntibiotics = false,
            RuleP4VagueRespiratoryVisits = true,
            RuleP5NoSpirometry = false
        }
    };

    private async Task AnalyzeAsync()
    {
        if (_isAnalyzing.Value)
        {
            return;
        }

        _isAnalyzing.Value = true;
        _analysisError.Value = null;

        try
        {
            var records = await _patients.GetAllPatientsAsync();
            var evals = records.Select(CopdRiskScorer.Evaluate).ToList();

            _excluded.Value = evals
                .Where(e => e.Excluded != null)
                .Select(e => e.Excluded!)
                .OrderBy(x => x.PatientId)
                .ToList();

            _scored.Value = evals
                .Where(e => e.Scored != null)
                .Select(e => e.Scored!)
                .Where(r => r.FinalScore >= 3)
                .OrderByDescending(r => r.FinalScore)
                .ThenBy(r => r.PatientId)
                .ToList();

            _invitedPatients.Value = new HashSet<string>();
            _hasAnalyzed.Value = true;
        }
        catch (Exception ex)
        {
            _analysisError.Value = T("AnalysisFailed", ex.Message);
            Log.Instance.Warning($"Prognos analysis error: {ex}");
        }
        finally
        {
            _isAnalyzing.Value = false;
        }
    }
}

