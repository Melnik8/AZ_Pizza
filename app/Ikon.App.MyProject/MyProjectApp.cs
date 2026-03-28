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
                                view.Text([Text.H2, "font-heading"], "LungFirst AI flagging (prototype)");
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

                            if (_hasAnalyzed.Value && !_isAnalyzing.Value)
                            {
                                view.Text([Text.Small, "text-muted-foreground"],
                                    $"{_scored.Value.Count} queued · {_excluded.Value.Count} non-risk");
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
                                if (_excluded.Value.Count > 0)
                                {
                                    view.Text([Text.H3, "font-heading text-sm"], "Non-risk group (no score)");
                                    view.Column([Layout.Column.Sm, "mb-6"], content: view =>
                                    {
                                        foreach (var ex in _excluded.Value.OrderBy(e => e.PatientId))
                                        {
                                            view.Column([
                                                    "rounded-lg border border-border bg-muted/30 p-3 text-sm gap-1"
                                                ],
                                                content: view =>
                                                {
                                                    view.Row(["gap-2 flex-wrap"], content: view =>
                                                    {
                                                        view.Text(["font-mono font-medium"], ex.PatientId);
                                                        view.Text(["text-muted-foreground"], $"age {ex.Age}");
                                                    });
                                                    view.Text(["text-xs opacity-90"], ex.Reason);
                                                });
                                        }
                                    });
                                }

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
                                                    view.Text(["text-sm font-medium leading-snug"], row.ActionLabel);
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

    private static string BandLabel(LungFirstBand b) =>
        b switch
        {
            LungFirstBand.PassiveWatch => "0–2 watch",
            LungFirstBand.StandardInvite => "3 standard",
            LungFirstBand.PriorityInvite => "4–5 priority",
            LungFirstBand.UrgentPriority => "6–7 urgent",
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

    private static string RowStyleForBand(LungFirstBand band) =>
        band switch
        {
            LungFirstBand.PassiveWatch =>
                "bg-emerald-50 border-emerald-200 text-emerald-950 dark:bg-emerald-950/50 dark:border-emerald-700 dark:text-zinc-100",
            LungFirstBand.StandardInvite =>
                "bg-sky-50 border-sky-200 text-sky-950 dark:bg-sky-950/50 dark:border-sky-700 dark:text-zinc-100",
            LungFirstBand.PriorityInvite =>
                "bg-amber-50 border-amber-200 text-amber-950 dark:bg-amber-950/50 dark:border-amber-700 dark:text-zinc-100",
            LungFirstBand.UrgentPriority =>
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
                .OrderByDescending(r => r.FinalScore)
                .ThenBy(r => r.PatientId)
                .ToList();

            _invitedPatients.Value = new HashSet<string>();
            _hasAnalyzed.Value = true;
        }
        catch (Exception ex)
        {
            _analysisError.Value = $"Analysis failed: {ex.Message}";
            Log.Instance.Warning($"LungFirst analysis error: {ex}");
        }
        finally
        {
            _isAnalyzing.Value = false;
        }
    }
}
