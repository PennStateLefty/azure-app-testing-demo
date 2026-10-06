using Azure.Developer.Playwright;
using Azure.Developer.Playwright.NUnit;
using Azure.Identity;

namespace LifeCore.PlaywrightTests;

[SetUpFixture]
public sealed class PlaywrightServiceSetup
{
    private PlaywrightServiceBrowserNUnit? _service;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_URL");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            TestContext.Progress.WriteLine("PLAYWRIGHT_SERVICE_URL is not set; using local Playwright browsers.");
            return;
        }

        var options = new PlaywrightServiceBrowserClientOptions
        {
            ServiceEndpoint = endpoint,
            ServiceAuth = ServiceAuthType.EntraId,
            ExposeNetwork = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_EXPOSE_NETWORK") ?? "<loopback>"
        };
        _service = new PlaywrightServiceBrowserNUnit(new DefaultAzureCredential(), options);
        await _service.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        if (_service is not null) await _service.DisposeAsync();
    }
}
