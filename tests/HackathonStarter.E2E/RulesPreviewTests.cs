using Microsoft.Playwright;

namespace HackathonStarter.E2E;

/// <summary>"Indicii și capcane" on /rules/preview: the real engine's outcome, in a real browser, on a phone too. Owner: R1.</summary>
[Collection("app")]
public class RulesPreviewTests(AppFixture app)
{
    [Fact]
    public async Task Maria_shows_her_uncovered_indicia_and_the_details_open_from_the_keyboard()
    {
        var page = await app.NewPageAsync(lang: null);
        await app.GoAsync(page, "/rules/preview?case=4");

        var panel = page.Locator("section.re");
        await Assertions.Expect(panel.Locator(".re-summary-title")).ToHaveTextAsync("Necesită clarificare");
        await Assertions.Expect(panel.Locator("article.re-open")).ToHaveCountAsync(2);
        await Assertions.Expect(panel.GetByText("CRS-IND-ADDR", new() { Exact = true })).ToBeVisibleAsync();

        var details = panel.Locator("details.re-details").First;
        await details.Locator("summary").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(details.GetByText("Temei legal")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_trap_explains_why_no_case_is_needed_in_english()
    {
        var page = await app.NewPageAsync();
        await app.GoAsync(page, "/rules/preview?case=5a");

        var panel = page.Locator("section.re");
        await Assertions.Expect(panel.Locator(".re-summary-title")).ToHaveTextAsync("No case needed");
        await Assertions.Expect(panel.Locator("article.re-cleared")).ToHaveCountAsync(1);
        await Assertions.Expect(panel.Locator(".re-none").First).ToHaveTextAsync("No indicium rule applies.");

        await page.Locator("a.rp-row[href$='case=5c']").ClickAsync();
        await Assertions.Expect(panel.Locator(".re-summary-title")).ToHaveTextAsync("FATCA alert");
    }

    [Fact]
    public async Task Fits_a_phone_without_sideways_scrolling()
    {
        var page = await app.NewPageAsync(width: 390, height: 844, lang: null);
        await app.GoAsync(page, "/rules/preview?case=4");

        await Assertions.Expect(page.Locator("section.re")).ToBeVisibleAsync();
        Assert.False(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > window.innerWidth"));
    }
}
