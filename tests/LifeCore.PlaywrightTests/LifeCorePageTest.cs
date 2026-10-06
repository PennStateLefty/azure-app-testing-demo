using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Developer.Playwright;
using Azure.Identity;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace LifeCore.PlaywrightTests;

public abstract class LifeCorePageTest : PageTest
{
    protected static string BaseUrl => Environment.GetEnvironmentVariable("BASE_URL")?.TrimEnd('/') ?? "http://localhost:5119";
    protected static bool UsePlaywrightService => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_URL"));

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = BaseUrl,
        ViewportSize = new ViewportSize { Width = 1440, Height = 1100 },
        IgnoreHTTPSErrors = true,
        Locale = "en-US"
    };

    public override async Task<(string, BrowserTypeConnectOptions?)?> ConnectOptionsAsync()
    {
        if (!UsePlaywrightService) return null;

        var options = new PlaywrightServiceBrowserClientOptions
        {
            ServiceEndpoint = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_URL"),
            ServiceAuth = ServiceAuthType.EntraId,
            ExposeNetwork = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_EXPOSE_NETWORK") ?? "<loopback>"
        };
        var client = new PlaywrightServiceBrowserClient(new DefaultAzureCredential(), options);
        var connectOptions = await client.GetConnectOptionsAsync<BrowserTypeConnectOptions>();
        return (connectOptions.WsEndpoint, connectOptions.Options);
    }

    public override Task<BrowserTypeLaunchOptions> LaunchOptionsAsync()
    {
        var options = new BrowserTypeLaunchOptions { Headless = true };
        var channel = Environment.GetEnvironmentVariable("BROWSER_CHANNEL");
        if (!string.IsNullOrWhiteSpace(channel)) options.Channel = channel;
        return Task.FromResult(options);
    }

    protected async Task GoToAsync(string path, string readyTestId)
    {
        await Page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForLifeCoreAsync(readyTestId);
    }

    protected async Task WaitForLifeCoreAsync(string readyTestId)
    {
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync(new() { Timeout = 20_000 });
        await Expect(Page.GetByTestId(readyTestId)).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    protected async Task FillByTestIdAsync(string testId, string value)
    {
        var locator = Page.GetByTestId(testId).First;
        var input = locator.Locator("input,textarea").First;
        if (await input.CountAsync() > 0) await input.FillAsync(value);
        else await locator.FillAsync(value);
    }

    protected async Task SelectMudOptionAsync(string testId, string optionText)
    {
        // MudSelect copies data-testid onto a hidden input and the visible combobox; click the visible one.
        await Page.Locator($"[data-testid='{testId}']:not(input[type='hidden'])").ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = optionText, Exact = true }).ClickAsync();
    }

    protected async Task ClickMudTabAsync(string testId, string name)
    {
        var tab = Page.GetByRole(AriaRole.Tab, new() { Name = name });
        if (await tab.CountAsync() > 0) await tab.ClickAsync();
        else await Page.GetByTestId(testId).ClickAsync();
    }

    protected async Task ResetDataAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        try
        {
            using var response = await client.PostAsync("/api/v1/admin/reset", content: null);
            TestContext.Progress.WriteLine(response.IsSuccessStatusCode
                ? "Reset deterministic LifeCore seed data."
                : $"Seed reset skipped: {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TestContext.Progress.WriteLine($"Seed reset skipped: {ex.Message}");
        }
    }

    protected static async Task<T?> ReadJsonAsync<T>(IAPIResponse response)
    {
        var text = await response.TextAsync();
        return JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    protected static Regex UrlRegex(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
}
