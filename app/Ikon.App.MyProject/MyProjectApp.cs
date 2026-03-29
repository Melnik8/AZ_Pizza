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
    private readonly Reactive<bool> _isAnalyzing = new(false);
    private readonly Reactive<string?> _analysisError = new(null);
    private readonly Reactive<List<CopdRiskResult>> _scored = new([]);
    private readonly Reactive<List<GateExcludedResult>> _excluded = new([]);
    private readonly Reactive<HashSet<string>> _invitedPatients = new(new HashSet<string>());
    private readonly Reactive<bool> _hasAnalyzed = new(false);

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
                                view.Text([Text.H2, "font-heading"], "Prognos AI flagging (prototype)");
                            });

                            view.Button([Button.GhostMd, Button.Size.Icon],
                                onClick: ToggleThemeAsync,
                                content: v => v.Icon([Icon.Default], name: _currentTheme.Value == Constants.DarkTheme ? "sun" : "moon"));
                        });
                    });

                    view.Column(["flex-1 min-h-0 max-w-6xl mx-auto w-full px-4 pb-4"], content: view =>
                    {
                        view.Row(["gap-3 flex-wrap items-center mb-3 flex-shrink-0"], content: view =>
                        {
                            view.Button(
                                [Button.PrimaryMd],
                                _isAnalyzing.Value ? "Analyzing…" : "Analyze database",
                                disabled: _isAnalyzing.Value,
                                onClick: async () => { await AnalyzeAsync(); });

                            view.Button(
                                [Button.SecondaryMd],
                                "Show example queue",
                                disabled: _isAnalyzing.Value,
                                onClick: async () => { await ShowExampleQueueAsync(); });

                            if (_hasAnalyzed.Value && !_isAnalyzing.Value)
                            {
                                view.Text([Text.Small, "text-muted-foreground"],
                                    $"{_scored.Value.Count} queued");
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
                                    "Run batch analysis. Output: priority-ranked invitation queue (bands), not a diagnosis.");
                                return;
                            }

                            view.Column([Layout.Column.Lg, "pb-4"], content: view =>
                            {
                                view.Box(["rounded-lg border border-border bg-muted/10 p-4 mb-4"], content: view =>
                                {
                                    view.Text([Text.H4, "font-heading mb-3"], "Color reference");
                                    view.Column(["gap-2"], content: view =>
                                    {
                                        view.Row(["items-center gap-3"], content: view =>
                                        {
                                            view.Box(["h-3 w-3 rounded-full bg-red-500"]);
                                            view.Text(["text-sm"], "Red — score 6-7 urgent priority; Wave 1 immediate; GP actively notified; coordinator call if no booking in 7 days.");
                                        });
                                        view.Row(["items-center gap-3"], content: view =>
                                        {
                                            view.Box(["h-3 w-3 rounded-full bg-amber-500"]);
                                            view.Text(["text-sm"], "Amber — score 4-5 priority invite; 30 min, sooner date; OmaKanta + SMS; GP passive notification.");
                                        });
                                        view.Row(["items-center gap-3"], content: view =>
                                        {
                                            view.Box(["h-3 w-3 rounded-full bg-emerald-500"]);
                                            view.Text(["text-sm"], "Green — score 3 standard invite; 20 min slot; OmaKanta + SMS.");
                                        });
                                    });
                                });

                                view.Text([Text.H3, "font-heading text-sm text-foreground"], "Invitation queue (by final score)");
                                view.Column([Layout.Column.Sm], content: view =>
                                {
                                    view.Row([
                                            "gap-3 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-foreground border-b border-border flex-wrap"
                                        ],
                                        content: view =>
                                        {
                                            view.Text(["min-w-[7rem]"], "Patient");
                                            view.Text(["w-10"], "Age");
                                            view.Text(["w-14 text-center"], "Final");
                                            view.Text(["min-w-[6rem]"], "Band");
                                            view.Text(["min-w-[7rem]"], "Wave / slot");
                                            view.Text(["flex-1 min-w-[12rem]"], "Details");
                                            view.Text(["w-28 text-right"], "Action");
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
                                                        view.Button([Button.SecondaryMd, "w-full"], "Invitation sent", disabled: true);
                                                    }
                                                    else
                                                    {
                                                        view.Button([Button.PrimaryMd, "w-full"], "Invite",
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

    private static string BandLabel(PrognosBand b) =>
        b switch
        {
            PrognosBand.PassiveWatch => "0–2 watch",
            PrognosBand.StandardInvite => "3 standard",
            PrognosBand.PriorityInvite => "4–5 priority",
            PrognosBand.UrgentPriority => "6–7 urgent",
            _ => ""
        };

    private static string FormatRuleHits(CopdRiskResult r)
    {
        var parts = new List<string>();
        if (r.RuleP1AgeSmokingOrCessation)
        {
            parts.Add("P1 age+smoking/NRT");
        }

        if (r.RuleP2SalbutamolWithoutRespDx)
        {
            parts.Add("P2 salbutamol");
        }

        if (r.RuleP3LrtiAntibiotics)
        {
            parts.Add("P3 LRTI abs");
        }

        if (r.RuleP4VagueRespiratoryVisits)
        {
            parts.Add("P4 vague visits");
        }

        if (r.RuleP5NoSpirometry)
        {
            parts.Add("P5 no spirometry");
        }

        if (r.ComorbidityModifierApplied)
        {
            parts.Add("+0.5 comorbidity→ceil");
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
            _analysisError.Value = $"Analysis failed: {ex.Message}";
            Log.Instance.Warning($"Prognos analysis error: {ex}");
        }
        finally
        {
            _isAnalyzing.Value = false;
        }
    }
}

