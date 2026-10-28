using HackathonStarter.Features.Rules;

namespace HackathonStarter.Components.Rules;

/// <summary>One checked rule as the panel shows it: the engine's hit, its catalog entry and the texts in one language.</summary>
public sealed record RuleEvidenceItem(RuleHit Hit, RuleDefinition Rule, string Title, string Description, string Explanation)
{
    public bool IsCovered => Hit.CoveredBy != Coverage.None;
    public bool PageVerified => Hit.GuidePage.Length > 0 && Hit.GuidePage != RuleCatalog.NotVerified;
}

/// <summary>
/// A <see cref="RuleOutcome"/> grouped for the "Indicii și capcane" panel: uncovered indicia (FATCA first), covered ones,
/// then the rules checked without a case. Nothing here decides: the texts are the engine's templates and the catalog's.
/// </summary>
public sealed record RuleEvidence(
    Decision Decision,
    Regime? Regime,
    string Explanation,
    IReadOnlyList<RuleEvidenceItem> Uncovered,
    IReadOnlyList<RuleEvidenceItem> Covered,
    IReadOnlyList<RuleEvidenceItem> Cleared,
    IReadOnlyList<string> AffectedJurisdictions,
    IReadOnlyList<string> ReportableFor,
    string EngineVersion)
{
    public int IndiciaCount => Uncovered.Count + Covered.Count;

    public static RuleEvidence From(RuleOutcome outcome, bool en)
    {
        RuleEvidenceItem Item(RuleHit hit)
        {
            // An outcome read back from RuleEvaluation.ResultJson may name a rule a later catalog no longer has.
            var rule = RuleCatalog.Contains(hit.RuleId)
                ? RuleCatalog.Get(hit.RuleId)
                : new RuleDefinition(hit.RuleId, hit.Regime, hit.RuleId, hit.RuleId, hit.GuidePage, Basis: hit.Basis);
            return new RuleEvidenceItem(hit, rule, en ? rule.Title : rule.TitleRo, en ? rule.Description : rule.DescriptionRo,
                en ? hit.Explanation : hit.ExplanationRo);
        }

        var uncovered = outcome.Uncovered.OrderBy(h => h.Regime == Features.Rules.Regime.Fatca ? 0 : 1).ToList();
        return new RuleEvidence(
            outcome.Decision,
            outcome.Regime,
            en ? outcome.Explanation : outcome.ExplanationRo,
            [.. uncovered.Select(Item)],
            [.. outcome.Indicia.Where(h => h.CoveredBy != Coverage.None).Select(Item)],
            [.. outcome.Cleared.Select(Item)],
            [.. uncovered.Select(h => h.Jurisdiction).Where(j => j.Length > 0).Distinct()],
            outcome.ReportableFor,
            outcome.EngineVersion);
    }
}
