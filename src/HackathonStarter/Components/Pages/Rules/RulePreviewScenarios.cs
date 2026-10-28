using HackathonStarter.Features.Changes;
using HackathonStarter.Features.Clients;
using HackathonStarter.Features.Rules;

namespace HackathonStarter.Components.Pages.Rules;

/// <param name="Title">English; the page shows it with T() (the Romanian is in Lang.Ro.Rules.cs).</param>
/// <param name="Evaluate">The call the real host makes: ChangeService (a change), CaseService (re-evaluation before closing)
/// or the self-certification check. Only the input is written here; the outcome is always the engine's.</param>
public sealed record RulePreviewScenario(string Id, string GoldenCase, string Title, Func<ICrsRuleEngine, RuleOutcome> Evaluate);

/// <summary>
/// The golden-case clients of docs/GOLDEN-CASES.md as snapshots, for the /rules/preview page. Synthetic data with the
/// card values (the same as R5's GoldenClients); moments are fixed, never the clock.
/// </summary>
public static class RulePreviewScenarios
{
    private static readonly ClientSnapshot Maria = RoResident(1, "Maria Popescu", new(1991, 6, 8), "2910608400319",
        "Bd. Unirii 15, bl. 7, ap. 22", "030823 București", "+40721384516",
        Account("RO19RZBR0000060021458391", AccountType.Depository, new(2019, 3, 14), 18_450m, "RON"));

    // One AddressChanged event carries both changes; the RO phone stops being current (GOLDEN-CASES.md, case 4).
    private static readonly ClientSnapshot MariaInMunich = MovedTo(Maria, "Leopoldstraße 12", "80802 München", "DE") with
    {
        Phones = [.. Maria.Phones.Select(p => p with { IsCurrent = false }), new PhoneSnapshot("+49 89 4521 7730", "DE")],
    };

    private static readonly StandingOrderSnapshot KellerOrder = new("DE02700100800918273645", "DE");
    private static readonly StandingOrderSnapshot ChicagoOrder = new("4417209358", "US");

    private static ClientSnapshot Radu(StandingOrderSnapshot? currentOrder = null, StandingOrderSnapshot? custodialOrder = null) =>
        RoResident(5, "Radu Ionescu", new(1982, 2, 14), "1820214400458", "Calea Moșilor 112, ap. 9", "020871 București", "+40726193458",
            Account("RO46RZBR0000060019823746", AccountType.Depository, new(2017, 5, 2), 82_500m, "EUR", currentOrder),
            Account("RO88RZBR0000069004471852", AccountType.Custodial, new(2020, 9, 10), 31_750m, "EUR", custodialOrder));

    private static readonly ClientSnapshot Andrei = RoResident(13, "Andrei Pop", new(1995, 9, 23), "1950923350125",
        "Str. Mărășești 6, ap. 3", "300086 Timișoara", "+40745210963",
        Account("RO20RZBR0000060033317205", AccountType.Depository, new(2021, 4, 6), 23_870m, "RON"));

    private static readonly ClientSnapshot Ioana = RoResident(15, "Ioana Mureșan", new(1988, 4, 11), "2880411120231",
        "Str. Memorandumului 21, ap. 4", "400114 Cluj-Napoca", "+40744518207",
        Account("RO43RZBR0000060007719364", AccountType.Depository, new(2014, 2, 17), 12_640m, "RON"));

    private static readonly ClientSnapshot Teodora = RoResident(18, "Teodora Iliescu", new(2004, 5, 2), "6040502401181",
        "", "București", "+40731904226",
        Account("RO38RZBR0000060153296860", AccountType.Depository, new(2022, 9, 1), 18_450m, "RON"));

    private static readonly ClientSnapshot Bogdan = RoResident(3, "Bogdan Lazăr", new(1987, 5, 22), "1870522083340",
        "", "București", "+40721000000",
        Account("RO57RZBR0000060052208841", AccountType.Depository, new(2026, 10, 26), 18_450m, "RON"));

    // Declared after the clients: static fields initialize in order.
    public static IReadOnlyList<RulePreviewScenario> All { get; } =
    [
        new("4", "4", "Maria: Munich address and German phone",
            e => e.Evaluate(Maria, MariaInMunich, ChangeKind.AddressChanged)),
        new("4-answer", "4", "Maria answers with a self-certification for Germany",
            e => e.EvaluateCurrent(MariaInMunich with
            {
                SelfCertification = new(Midnight(2027, 1, 21), [new TaxResidenceSnapshot("DE", "86095174223")], false),
            })),
        new("5a", "5a", "Trap: standing order to Germany from a current account",
            e => e.Evaluate(Radu(), Radu(currentOrder: KellerOrder), ChangeKind.StandingOrderCreated)),
        new("5b", "5b", "The same order from a custodial account",
            e => e.Evaluate(Radu(), Radu(custodialOrder: KellerOrder), ChangeKind.StandingOrderCreated)),
        new("5c", "5c", "Standing order to the US from a current account",
            e => e.Evaluate(Radu(), Radu(currentOrder: ChicagoOrder), ChangeKind.StandingOrderCreated)),
        new("13", "13", "Trap: German phone next to the Romanian phone",
            e => e.Evaluate(Andrei, Andrei with { Phones = [.. Andrei.Phones, new PhoneSnapshot("+4915144201187", "DE")] },
                ChangeKind.PhoneAdded)),
        new("15", "15", "Trap: Cluj to Iași",
            e => e.Evaluate(Ioana, MovedTo(Ioana, "Bd. Ștefan cel Mare și Sfânt 8, ap. 12", "700064 Iași", "RO"), ChangeKind.AddressChanged)),
        new("18", "18", "Student in Munich with an accepted explanation",
            e => e.EvaluateCurrent(MovedTo(Teodora, "Helene-Mayer-Ring 9", "80809 München", "DE") with
            {
                AcceptedExplanations = [new("DE", ExplanationCategory.Student, "CRS-2026-0018", new(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc))],
            })),
        new("3", "3", "Self-certification for Austria without a TIN, with a reason",
            e => e.EvaluateSelfCertification(MovedTo(Bogdan, "Annenstraße 24", "8020 Graz", "AT"),
                new(Midnight(2026, 10, 26), [new TaxResidenceSnapshot("AT", null, NoTinReason.B_UnableToObtain, "Nu am primit Steuernummer.")], false))),
    ];

    private static DateTime Midnight(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static AccountSnapshot Account(string iban, AccountType type, DateOnly openedOn, decimal balance, string currency,
        StandingOrderSnapshot? order = null) => new(iban, type, openedOn, balance, currency, order is null ? [] : [order]);

    /// <summary>"Standard RO resident" (GOLDEN-CASES.md, 1): RO address, phone and citizenship, a valid self-certification [RO].</summary>
    private static ClientSnapshot RoResident(int id, string name, DateOnly born, string cnp, string line, string city, string phone,
        params AccountSnapshot[] accounts)
    {
        var opened = accounts.Min(a => a.OpenedOn);
        return new(id, $"CL-{id:0000}", name, born, "RO", ["RO"], [new AddressSnapshot(AddressKind.Residence, line, city, "RO")],
            [new PhoneSnapshot(phone, "RO")], accounts, [],
            new SelfCertificationSnapshot(Midnight(opened.Year, opened.Month, opened.Day), [new TaxResidenceSnapshot("RO", cnp)], false), []);
    }

    /// <summary>A new residence; the current ones stop being current.</summary>
    private static ClientSnapshot MovedTo(ClientSnapshot s, string line, string city, string country) => s with
    {
        Addresses = [.. s.Addresses.Select(a => a with { IsCurrent = false }), new AddressSnapshot(AddressKind.Residence, line, city, country)],
    };
}
