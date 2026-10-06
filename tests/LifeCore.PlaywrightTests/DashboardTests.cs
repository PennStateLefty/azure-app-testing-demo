using Microsoft.Playwright;

namespace LifeCore.PlaywrightTests;

[Category("Dashboard")]
public sealed class DashboardTests : LifeCorePageTest
{
    [Test]
    public async Task KpiTilesAndWorklistRowsRender()
    {
        await GoToAsync("/dashboard", "worklist-grid");

        foreach (var testId in new[] { "kpi-open-cases", "kpi-pending-requirements", "kpi-aps-pending", "kpi-avg-days", "kpi-sla-risk", "kpi-decisions-today" })
        {
            await Expect(Page.GetByTestId(testId)).ToBeVisibleAsync();
            await Expect(Page.GetByTestId(testId).GetByTestId("kpi-value")).Not.ToHaveTextAsync("—");
        }

        await Expect(Page.GetByTestId("worklist-row").First).ToBeVisibleAsync();
        Assert.That(await Page.GetByTestId("worklist-row").CountAsync(), Is.GreaterThan(0));
    }

    [Test]
    public async Task FilteringPagingAndViewToggleWork()
    {
        await GoToAsync("/dashboard", "worklist-grid");

        await Page.GetByTestId("worklist-view-case").ClickAsync();
        await Expect(Page.GetByTestId("worklist-grid")).ToContainTextAsync("Open tasks");

        await SelectMudOptionAsync("worklist-filter-status", "Pending Requirements");
        await Expect(Page.GetByTestId("worklist-grid")).ToContainTextAsync("Pending Requirements");

        await FillByTestIdAsync("worklist-search", "UW100001");
        await Expect(Page.GetByTestId("worklist-grid")).ToContainTextAsync("UW100001", new() { Timeout = 15_000 });

        await FillByTestIdAsync("worklist-search", string.Empty);
        var next = Page.GetByTestId("worklist-next-page");
        if (await next.IsEnabledAsync())
        {
            var before = await Page.GetByTestId("worklist-page-info").InnerTextAsync();
            await next.ClickAsync();
            await Expect(Page.GetByTestId("worklist-page-info")).Not.ToHaveTextAsync(before);
        }
    }

    [Test]
    public async Task ClickingWorklistRowOpensCaseWorkbench()
    {
        await GoToAsync("/dashboard", "worklist-grid");
        await FillByTestIdAsync("worklist-search", "UW100001");
        await Expect(Page.Locator("[data-testid='worklist-row'][data-case-number='UW100001']").First).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Page.Locator("[data-testid='worklist-row'][data-case-number='UW100001'] button").First.ClickAsync();
        await Expect(Page.GetByTestId("case-summary-panel")).ToBeVisibleAsync();
        await Page.GetByTestId("case-summary-open-workbench").ClickAsync();

        await WaitForLifeCoreAsync("case-header");
        await Expect(Page).ToHaveURLAsync(UrlRegex("/cases/UW100001"));
    }

    [Test]
    public async Task PersonaSwitcherCanSelectUnderwriterWhenPresent()
    {
        await GoToAsync("/dashboard", "worklist-grid");
        var switcher = Page.GetByTestId("persona-switcher");
        if (await switcher.CountAsync() == 0)
        {
            Assert.Ignore("Persona switcher is not present in this deployment.");
        }

        await switcher.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Dana Whitfield (Underwriter)", Exact = true }).ClickAsync();
        await Expect(switcher).ToContainTextAsync("Dana Whitfield");
    }
}
