# Indicii și capcane (R1)

`RuleEvidencePanel` shows one `RuleOutcome` of the rules engine:

- the decision, first: Fără caz / Conform / Necesită clarificare / Alertă FATCA, with the regime of the uncovered
  indicia, the jurisdictions they affect and `ReportableFor`, and the engine's own explanation;
- the indicia from the law (uncovered first, FATCA before CRS), then the covered ones (self-certification or accepted
  explanation);
- the traps and rules checked without a case (`Cleared`), each with the reason;
- for every rule: the guide page, Verified source / Interpretation / Bank policy (`RuleHit.Basis`), and, behind
  "Detaliile regulii", what the rule says, the legal basis, the data field and the rule ID.

It never evaluates or rewords a rule: every text is the outcome's (`ExplanationRo` / `Explanation`) or the catalog's
(`TitleRo` / `Title`, `DescriptionRo` / `Description`, `LegalBasis`). Romanian by default, English with the RO/EN button.

## On the case page

```razor
@using HackathonStarter.Components.Rules

<RuleEvidencePanel Outcome="_outcome" Loading="_loading" Error="@_error" OnRetry="LoadAsync" />
```

Where `_outcome` comes from (all real engine output, never rebuilt by hand):

| Page | Source |
| --- | --- |
| `/changes/new`, after recording | `ChangeResult.Outcome` from `IChangeService.RecordAsync` |
| `/cases/{code}`, what opened the case | `JsonSerializer.Deserialize<RuleOutcome>(evaluation.ResultJson)` for the trigger change, with the options ChangeService serialized it with |
| `/cases/{code}`, the state now (after an answer) | `engine.EvaluateCurrent(await clients.SnapshotAsync(case.ClientId))` |

Do not rebuild an outcome from `Case.Indicia`: `Indicium` has no `CoveredBy` and no `Basis`, and the case keeps no
`Cleared` list, so coverage and the traps would be lost.

`Loading` shows a skeleton while `_outcome` is null; `Error` shows `ErrorState` with a retry. With neither, a null
outcome shows "Încă nicio evaluare a regulilor".

## Preview

`/rules/preview` (not in the nav: `SiteConfig.Nav` is shared) runs the real `ICrsRuleEngine` on the synthetic
golden-case clients 3, 4 (and Maria's answer), 5a, 5b, 5c, 13, 15 and 18. `?case=5a` opens one directly. Once R5's
`GoldenCases` with input (PR #22) is on main, the scenarios can come from there.

Tests: `tests/HackathonStarter.Tests/Crs/Rules/RuleEvidencePanelTests.cs` (rendered with Blazor's `HtmlRenderer`) and
`tests/HackathonStarter.E2E/RulesPreviewTests.cs` (browser, keyboard, 390 px).
