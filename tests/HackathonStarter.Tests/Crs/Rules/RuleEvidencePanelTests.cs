using System.Globalization;
using System.Net;
using System.Text.Json;
using HackathonStarter.Components.Pages.Rules;
using HackathonStarter.Components.Rules;
using HackathonStarter.Features.Changes;
using HackathonStarter.Features.Rules;
using HackathonStarter.Features.Tin;
using HackathonStarter.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace HackathonStarter.Tests.Crs.Rules;

/// <summary>
/// The "Indicii și capcane" panel: grouped from the engine's real outcome (the golden cases through ClientSnapshot) and
/// rendered with Blazor's HtmlRenderer, Romanian by default and English on request.
/// </summary>
public class RuleEvidencePanelTests
{
    private static readonly ICrsRuleEngine Engine = new CrsRuleEngine(new FakeTinFormat());

    private static RuleOutcome Outcome(string id)
    {
        var c = EngineCases.Get(id);
        return Engine.Evaluate(Snapshots.From(c.Before), Snapshots.From(c.After), Enum.Parse<ChangeKind>(c.Change.Kind));
    }

    [Fact]
    public void Groups_uncovered_covered_and_cleared_as_the_engine_returned_them()
    {
        var maria = RuleEvidence.From(Outcome("4"), en: false);
        Assert.Equal([RuleIds.CrsAddress, RuleIds.CrsPhone], maria.Uncovered.Select(i => i.Hit.RuleId).Order());
        Assert.Empty(maria.Covered);
        Assert.Equal(["DE"], maria.AffectedJurisdictions);
        Assert.Equal((Decision.Clarification, Regime.Crs), (maria.Decision, maria.Regime));

        var teodora = RuleEvidence.From(Outcome("18-explained"), en: false);
        Assert.Empty(teodora.Uncovered);
        Assert.Equal(Coverage.AcceptedExplanation, Assert.Single(teodora.Covered).Hit.CoveredBy);
        Assert.Contains(teodora.Cleared, i => i.Hit.RuleId == RuleIds.Explanation);
        Assert.Empty(teodora.AffectedJurisdictions);

        var trap = RuleEvidence.From(Outcome("5a"), en: false);
        Assert.Equal(0, trap.IndiciaCount);
        Assert.Equal(RuleIds.CrsStandingOrder, Assert.Single(trap.Cleared).Hit.RuleId);
    }

    [Fact]
    public void Texts_are_romanian_by_default_and_english_on_request_from_the_outcome_and_the_catalog()
    {
        var outcome = Outcome("4");
        var hit = outcome.Indicia.First(h => h.RuleId == RuleIds.CrsAddress);
        var rule = RuleCatalog.Get(RuleIds.CrsAddress);

        var ro = RuleEvidence.From(outcome, en: false);
        var en = RuleEvidence.From(outcome, en: true);

        Assert.Equal(outcome.ExplanationRo, ro.Explanation);
        Assert.Equal(outcome.Explanation, en.Explanation);
        var roItem = ro.Uncovered.Single(i => i.Hit.RuleId == RuleIds.CrsAddress);
        var enItem = en.Uncovered.Single(i => i.Hit.RuleId == RuleIds.CrsAddress);
        Assert.Equal((rule.TitleRo, rule.DescriptionRo, hit.ExplanationRo), (roItem.Title, roItem.Description, roItem.Explanation));
        Assert.Equal((rule.Title, rule.Description, hit.Explanation), (enItem.Title, enItem.Description, enItem.Explanation));
    }

    [Fact]
    public void Fatca_comes_first_and_a_rule_missing_from_the_catalog_is_shown_as_it_came()
    {
        RuleHit Hit(string id, Regime regime, string j) => new(id, regime, j, "en", "ro", "29", "Phones");
        var outcome = new RuleOutcome(Decision.FatcaAlert,
            [Hit(RuleIds.CrsPhone, Regime.Crs, "DE"), Hit(RuleIds.FatcaPhone, Regime.Fatca, "US"), Hit("OLD-RULE", Regime.Crs, "AT")],
            [], [], "en", "ro", "crs-rules/0.9");

        var view = RuleEvidence.From(outcome, en: false);

        Assert.Equal(RuleIds.FatcaPhone, view.Uncovered[0].Hit.RuleId);
        Assert.Equal(["US", "DE", "AT"], view.AffectedJurisdictions);
        var old = view.Uncovered.Single(i => i.Hit.RuleId == "OLD-RULE");
        Assert.Equal(("OLD-RULE", "29"), (old.Title, old.Rule.GuidePage));
    }

    [Fact]
    public async Task Maria_renders_in_romanian_with_the_page_the_label_and_the_details()
    {
        var outcome = Outcome("4");
        var html = await Render(outcome, "ro-RO");

        Assert.Contains("Indicii și capcane", html);
        Assert.Contains("Necesită clarificare", html);
        Assert.Contains(outcome.ExplanationRo, html);
        Assert.Contains(RuleCatalog.Get(RuleIds.CrsAddress).TitleRo, html);
        Assert.Contains("Neacoperit", html);
        Assert.Contains("Ghid p. 29", html);
        // A new account: the list of indicia is applied as "reason to know", our interpretation (GEN-NEW-ACCOUNT).
        Assert.All(outcome.Indicia, h => Assert.Equal(RuleBasis.Interpretation, h.Basis));
        Assert.All(Rows(html), r => Assert.Contains("Interpretare", r));
        Assert.DoesNotContain("Sursă verificată", html);
        Assert.Contains("interpretarea noastră sau pe o politică a băncii", html);
        Assert.Contains("<details", html);
        Assert.Contains(RuleCatalog.Get(RuleIds.CrsAddress).LegalBasis, html);
        Assert.Contains("Addresses/Residence", html);
        Assert.Equal(outcome.Indicia.Count + outcome.Cleared.Count, Rows(html).Length);
        Assert.DoesNotContain("Clarification needed", html);
    }

    [Fact]
    public async Task The_same_outcome_renders_in_english()
    {
        var outcome = Outcome("4");
        var html = await Render(outcome, "en-GB");

        Assert.Contains("Indicia and traps", html);
        Assert.Contains("Clarification needed", html);
        Assert.Contains(outcome.Explanation, html);
        Assert.Contains(RuleCatalog.Get(RuleIds.CrsAddress).Title, html);
        Assert.DoesNotContain(outcome.ExplanationRo, html);
    }

    [Fact]
    public async Task A_trap_says_why_no_case_is_needed()
    {
        var outcome = Outcome("5a");
        var html = await Render(outcome, "ro-RO");

        Assert.Contains("Nu e nevoie de caz", html);
        Assert.Contains("Nicio regulă de indiciu nu se aplică.", html);
        var row = Assert.Single(Rows(html));
        Assert.Contains(RuleIds.CrsStandingOrder, row);
        Assert.Contains("Verificat, fără caz", row);
        Assert.Contains(outcome.Cleared[0].ExplanationRo, row);
    }

    [Fact]
    public async Task Interpretation_and_policy_are_never_shown_as_verified()
    {
        var bogdan = Subjects.RoResident("CL-0003", "1870522083340", new DateOnly(2026, 10, 26), fullName: "Bogdan Lazăr").MovedTo("AT");
        var form = new Features.Clients.SelfCertificationSnapshot(new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc),
            [new("AT", null, Features.Clients.NoTinReason.B_UnableToObtain, "Nu am primit Steuernummer.")], false);
        var outcome = Engine.EvaluateSelfCertification(Snapshots.From(bogdan), form);

        var html = await Render(outcome, "ro-RO");
        var rows = Rows(html);

        foreach (var hit in outcome.Indicia.Concat(outcome.Cleared))
        {
            var row = rows.Single(r => r.Contains($">{hit.RuleId}<"));
            var (label, other) = hit.Basis switch
            {
                RuleBasis.Interpretation => ("Interpretare", "Sursă verificată"),
                RuleBasis.Policy => ("Politica băncii", "Sursă verificată"),
                _ => ("Sursă verificată", "Interpretare"),
            };
            Assert.Contains(label, row);
            Assert.DoesNotContain(other, row);
        }
        Assert.Contains(outcome.Indicia, h => h.Basis == RuleBasis.Interpretation);
        Assert.Contains(outcome.Cleared, h => h.Basis == RuleBasis.Policy);
        Assert.Contains("Acoperit de autocertificare", html);
        Assert.Contains("interpretarea noastră sau pe o politică a băncii", html);
    }

    [Theory]
    [MemberData(nameof(EngineCases.Ids), MemberType = typeof(EngineCases))]
    public async Task Every_golden_outcome_renders_every_rule_it_holds(string id)
    {
        var outcome = Outcome(id);
        var html = await Render(outcome, "ro-RO");

        var rows = Rows(html);
        Assert.Equal(outcome.Indicia.Count + outcome.Cleared.Count, rows.Length);
        foreach (var hit in outcome.Indicia.Concat(outcome.Cleared))
            Assert.Contains(rows, r => r.Contains($">{hit.RuleId}<") && r.Contains(hit.ExplanationRo));
        Assert.Equal(outcome.Uncovered.Count(), rows.Count(r => r.Contains(">Neacoperit<")));
    }

    [Fact]
    public async Task An_outcome_read_back_from_its_json_renders_the_same()
    {
        var outcome = Outcome("18-explained");
        var stored = JsonSerializer.Deserialize<RuleOutcome>(JsonSerializer.Serialize(outcome))!;

        Assert.Equal(await Render(outcome, "ro-RO"), await Render(stored, "ro-RO"));
    }

    [Fact]
    public async Task Loading_empty_and_error_states()
    {
        Assert.Contains("aria-busy=\"true\"", await Render(null, "ro-RO", loading: true));
        Assert.Contains("Încă nicio evaluare a regulilor", await Render(null, "ro-RO"));
        Assert.Contains("motorul nu răspunde", await Render(null, "ro-RO", error: "motorul nu răspunde"));
    }

    [Fact]
    public void Every_preview_scenario_runs_on_the_engine_wired_like_program_cs_and_has_a_romanian_title()
    {
        var engine = new ServiceCollection().AddRules().AddTin().BuildServiceProvider().GetRequiredService<ICrsRuleEngine>();

        Assert.All(RulePreviewScenarios.All, s =>
        {
            Assert.True(Lang.Translations.ContainsKey(s.Title), s.Title);
            Assert.Equal(CrsRuleEngine.Version, s.Evaluate(engine).EngineVersion);
        });
        Assert.Equal(RulePreviewScenarios.All.Count, RulePreviewScenarios.All.Select(s => s.Id).Distinct().Count());
    }

    private static string[] Rows(string html) => [.. html.Split("<article").Skip(1)];

    private static async Task<string> Render(RuleOutcome? outcome, string culture, bool loading = false, string? error = null)
    {
        var services = new ServiceCollection().AddLogging().AddMudServices().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            return await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(RuleEvidencePanel.Outcome)] = outcome,
                    [nameof(RuleEvidencePanel.Loading)] = loading,
                    [nameof(RuleEvidencePanel.Error)] = error,
                });
                // Decoded: HtmlRenderer writes diacritics as entities, the browser shows the text.
                return WebUtility.HtmlDecode((await renderer.RenderComponentAsync<RuleEvidencePanel>(parameters)).ToHtmlString());
            });
        }
        finally { CultureInfo.CurrentUICulture = before; }
    }
}
