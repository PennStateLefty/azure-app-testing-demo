using Microsoft.Playwright;

namespace LifeCore.PlaywrightTests;

[Category("CaseWorkbench")]
public sealed class CaseWorkbenchTests : LifeCorePageTest
{
    [Test]
    public async Task PendingRequirementsCaseShowsApsRiskAndBlocksDecisionWithoutOverride()
    {
        await GoToAsync("/cases/UW100001", "case-header");

        await Expect(Page.Locator("[data-testid='oob-alert'][data-field='Bmi']")).ToBeVisibleAsync();
        await Page.GetByTestId("tab-requirements").ClickAsync();
        await Expect(Page.GetByTestId("requirements-grid")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='requirement-row'][data-type='Aps']")).ToBeVisibleAsync();

        await FillByTestIdAsync("decision-reason", "E2E validation should block outstanding APS without override");
        var responseTask = Page.WaitForResponseAsync(r => r.Url.Contains("/api/v1/cases/UW100001/decision") && r.Status == 422);
        await Page.GetByTestId("decision-submit").ClickAsync();
        await responseTask;
        await Expect(Page.GetByTestId("decision-error")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("decision-error")).ToContainTextAsync("outstanding", new() { IgnoreCase = true });
    }

    [Test]
    public async Task DecisionableCaseCanBeDecisionedSuccessfully()
    {
        await GoToAsync("/cases/UW100002", "case-header");

        await SelectMudOptionAsync("decision-risk-class", "Standard");
        await FillByTestIdAsync("decision-reason", "Meets standard underwriting guidelines");
        await FillByTestIdAsync("decision-note", "Automated E2E decision flow");
        var responseTask = Page.WaitForResponseAsync(r => r.Url.Contains("/api/v1/cases/UW100002/decision") && r.Status is >= 200 and < 300);
        await Page.GetByTestId("decision-submit").ClickAsync();
        await responseTask;

        await Expect(Page.GetByTestId("decision-success")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("case-header")).ToContainTextAsync("Decisioned");
        await Page.GetByTestId("tab-notes").ClickAsync();
        await Expect(Page.GetByTestId("note-item").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task WorkbenchTabsSwitchToExpectedPanels()
    {
        await GoToAsync("/cases/UW100001", "case-header");

        await Page.GetByTestId("tab-requirements").ClickAsync();
        await Expect(Page.GetByTestId("requirements-grid")).ToBeVisibleAsync();

        await Page.GetByTestId("tab-risk").ClickAsync();
        await Expect(Page.GetByTestId("suggested-risk-class")).ToBeVisibleAsync();

        await Page.GetByTestId("tab-notes").ClickAsync();
        await Expect(Page.GetByTestId("note-input")).ToBeVisibleAsync();

        await Page.GetByTestId("tab-overview").ClickAsync();
        await Expect(Page.Locator("[data-testid='oob-alert']").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task NotFoundCaseShowsNotFoundState()
    {
        await Page.GotoAsync("/cases/UW999999", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Expect(Page.GetByTestId("case-not-found")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(Page.GetByTestId("case-not-found")).ToContainTextAsync("UW999999");
    }
}
