using Microsoft.Playwright;

namespace LifeCore.PlaywrightTests;

[Category("Policy360")]
public sealed class Policy360Tests : LifeCorePageTest
{
    [Test]
    public async Task SearchByPolicyPrefixAndOwnerLastNameOpensPolicy()
    {
        await GoToAsync("/policies", "policy-search-input");
        await SearchPoliciesAsync("LC1000001");
        await Expect(Page.Locator("[data-testid='policy-result-row'][data-policy-number='LC1000001']")).ToBeVisibleAsync();

        var ownerName = await Page.Locator("[data-testid='policy-result-row'][data-policy-number='LC1000001'] td").Nth(4).InnerTextAsync();
        var lastName = ownerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
        await SearchPoliciesAsync(lastName);
        await Expect(Page.GetByTestId("policy-result-row").First).ToBeVisibleAsync();

        await Page.Locator("[data-testid='policy-result-row'][data-policy-number='LC1000001']").ClickAsync();
        await WaitForLifeCoreAsync("policy-header");
        await Expect(Page).ToHaveURLAsync(UrlRegex("/policies/LC1000001"));
    }

    [Test]
    public async Task PolicyTabsRenderExpectedContent()
    {
        await GoToAsync("/policies/LC1000001", "policy-header");

        await Expect(Page.GetByTestId("coverages-grid")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("coverage-row").First).ToBeVisibleAsync();

        await ClickMudTabAsync("tab-parties", "Parties");
        await Expect(Page.GetByTestId("party-owner")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("party-insured")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("beneficiaries-grid")).ToBeVisibleAsync();
        Assert.That(await Page.GetByTestId("beneficiary-row").CountAsync(), Is.EqualTo(2));

        await ClickMudTabAsync("tab-billing", "Billing");
        await Expect(Page.GetByTestId("billing-summary")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("payments-grid")).ToBeVisibleAsync();

        await ClickMudTabAsync("tab-values", "Values");
        await Expect(Page.GetByTestId("values-summary")).ToBeVisibleAsync();

    }

    [Test]
    public async Task BeneficiaryUpdateShowsValidationWhenPrimaryTotalIsNot100()
    {
        await GoToAsync("/policies/LC1000001", "policy-header");
        await Page.GetByTestId("action-change-beneficiary").ClickAsync();
        await Expect(Page.GetByTestId("beneficiary-percent").First).ToBeVisibleAsync();

        await Page.GetByTestId("beneficiary-remove-row").First.ClickAsync();
        await Expect(Page.GetByTestId("beneficiary-primary-total")).ToContainTextAsync("must total 100");
        var responseTask = Page.WaitForResponseAsync(r => r.Url.Contains("/api/v1/policies/LC1000001/beneficiaries") && r.Status == 422);
        await Page.GetByTestId("beneficiary-submit").ClickAsync();
        await responseTask;
        await Expect(Page.GetByTestId("beneficiary-error")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("beneficiary-error")).ToContainTextAsync("100");
    }

    [Test]
    public async Task AddressChangeServiceActionCreatesPendingTransaction()
    {
        await GoToAsync("/policies/LC1000001", "policy-header");
        await Page.GetByTestId("action-change-address").ClickAsync();

        await FillByTestIdAsync("address-line1", "100 E2E Test Ave");
        await FillByTestIdAsync("address-line2", "Suite 10");
        await FillByTestIdAsync("address-city", "Columbus");
        await FillByTestIdAsync("address-state", "OH");
        await FillByTestIdAsync("address-postal", "43215");
        var responseTask = Page.WaitForResponseAsync(r => r.Url.Contains("/api/v1/policies/LC1000001/address") && r.Status is >= 200 and < 300);
        await Page.GetByTestId("address-submit").ClickAsync();
        await responseTask;

        await ClickMudTabAsync("tab-transactions", "Transactions");
        await Expect(Page.Locator("[data-testid='transaction-row'][data-type='AddressChange'][data-status='Pending']").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task QuoteServiceActionReturnsNetProceeds()
    {
        await GoToAsync("/policies/LC1000001", "policy-header");
        await Page.GetByTestId("action-quote").ClickAsync();
        await FillByTestIdAsync("quote-amount", "1000");
        var responseTask = Page.WaitForResponseAsync(r => r.Url.Contains("/api/v1/policies/LC1000001/quotes") && r.Status is >= 200 and < 300);
        await Page.GetByTestId("quote-submit").ClickAsync();
        await responseTask;

        await Expect(Page.GetByTestId("quote-result")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("quote-net-proceeds")).ToContainTextAsync("$");
    }

    [Test]
    public async Task FixedIndexedAnnuityValuesTabShowsThreeFunds()
    {
        await GoToAsync("/policies/LC1000002", "policy-header");
        await ClickMudTabAsync("tab-values", "Values");

        await Expect(Page.GetByTestId("funds-grid")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("funds-chart")).ToBeVisibleAsync();
        Assert.That(await Page.Locator("[data-testid='funds-grid'] tbody tr").CountAsync(), Is.EqualTo(3));
    }

    [Test]
    public async Task NotFoundPolicyShowsNotFoundState()
    {
        await Page.GotoAsync("/policies/LC9999999", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Expect(Page.GetByTestId("policy-not-found")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    private async Task SearchPoliciesAsync(string query)
    {
        await FillByTestIdAsync("policy-search-input", query);
        await Page.GetByTestId("policy-search-submit").ClickAsync();
        await Expect(Page.GetByTestId("policy-results")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("policy-result-row").First).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}
