using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace LifeCore.PlaywrightTests;

[Category("Smoke")]
public sealed class SmokeTests : LifeCorePageTest
{
    [Test]
    public async Task AppLoadsNavWorksAndFooterDisclaimerIsPresent()
    {
        await GoToAsync("/dashboard", "worklist-grid");

        await Expect(Page.GetByTestId("env-badge")).ToHaveTextAsync("DEMO");
        await Expect(Page.GetByTestId("footer-disclaimer")).ToContainTextAsync("not affiliated with or endorsed by Accenture");
        await Expect(Page.GetByTestId("footer-disclaimer")).ToContainTextAsync("Synthetic data only");

        await Page.GetByTestId("nav-policies").ClickAsync();
        await WaitForLifeCoreAsync("policy-search-input");
        await Expect(Page).ToHaveURLAsync(UrlRegex("/policies"));

        await Page.GetByTestId("nav-dashboard").ClickAsync();
        await WaitForLifeCoreAsync("worklist-grid");
        await Expect(Page).ToHaveURLAsync(UrlRegex("/dashboard"));
    }

    [TestCase("UW100001", "/cases/UW100001", "case-header")]
    [TestCase("LC1000001", "/policies/LC1000001", "policy-header")]
    [TestCase("smith", "/policies?query=smith", "policy-results")]
    public async Task GlobalSearchRoutesByPrefix(string query, string urlPart, string readyTestId)
    {
        await GoToAsync("/dashboard", "worklist-grid");
        await FillByTestIdAsync("global-search", query);
        await Page.GetByTestId("global-search").PressAsync("Enter");

        await WaitForLifeCoreAsync(readyTestId);
        await Expect(Page).ToHaveURLAsync(UrlRegex(System.Text.RegularExpressions.Regex.Escape(urlPart).Replace("\\?", "[?]")));
    }

    [TestCase("/dashboard", "worklist-grid")]
    [TestCase("/cases/UW100001", "case-header")]
    [TestCase("/policies/LC1000001", "policy-header")]
    public async Task AxeAccessibilityScanHasNoCriticalViolations(string path, string readyTestId)
    {
        await GoToAsync(path, readyTestId);
        var result = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = RunOnlyOptions.Tags("wcag2a", "wcag2aa", "wcag21a", "wcag21aa")
        });

        var critical = result.Violations.Where(v => string.Equals(v.Impact, "critical", StringComparison.OrdinalIgnoreCase)).ToList();
        var serious = result.Violations.Where(v => string.Equals(v.Impact, "serious", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var violation in serious)
        {
            TestContext.Progress.WriteLine($"Serious a11y violation (logged only): {violation.Id} - {violation.Help}");
        }

        Assert.That(critical, Is.Empty, string.Join(Environment.NewLine, critical.Select(v => $"{v.Id}: {v.Help}")));
    }
}
