using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public sealed class GstPortalBrowserService : IAsyncDisposable
{
    private const string LoginUrl = "https://services.gst.gov.in/services/login";
    private const string DashboardUrl = "https://return.gst.gov.in/returns/auth/dashboard";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(15);

    private readonly DatabaseService _database;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<GstPortalBrowserService> _logger;
    private readonly ConcurrentDictionary<string, PortalSession> _sessions = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserLock = new(1, 1);

    public GstPortalBrowserService(
        DatabaseService database,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IServiceScopeFactory scopes,
        ILogger<GstPortalBrowserService> logger)
    {
        _database = database;
        _configuration = configuration;
        _environment = environment;
        _scopes = scopes;
        _logger = logger;
    }

    public async Task<GstPortalStep> StartAsync(GstPortalStartRequest request, CancellationToken ct)
    {
        await RemoveExpiredSessionsAsync();
        if (request.DateTo.Date < request.DateFrom.Date)
            throw new ArgumentException("Date to is before date from.");

        var credentials = await ResolveCredentialsAsync(request.CompanyName, ct);
        using var scope = _scopes.CreateScope();
        var coverage = await scope.ServiceProvider.GetRequiredService<Gst2BSnapshotService>()
            .CoverageAsync(request.CompanyName, request.DateFrom, request.DateTo, ct);
        if (!coverage.NeedsDownload)
            throw new InvalidOperationException(coverage.Message);

        var id = Guid.NewGuid().ToString("N");
        var session = new PortalSession
        {
            Id = id,
            Request = request,
            MonthsToDownload = coverage.MissingMonths,
            RemainingCredentials = new Queue<PortalCredential>(credentials),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _sessions[id] = session;
        return await BeginNextCredentialAsync(session);
    }

    public async Task<GstPortalStep> SubmitCaptchaAsync(string sessionId, string captcha)
    {
        var session = GetSession(sessionId);
        var page = session.Page!;
        var credential = session.Credential ?? throw new InvalidOperationException("GST login is not ready.");
        await page.Locator("#username").FillAsync(credential.Login);
        await page.Locator("#user_pass").FillAsync(credential.Password);
        await page.Locator("#captcha").FillAsync((captcha ?? "").Trim());
        await page.Locator("button.btn-primary").First.ClickAsync();
        await page.WaitForTimeoutAsync(2_500);

        if (await IsVisibleAsync(page.Locator("#captcha")))
        {
            var image = await CaptureCaptchaAsync(page);
            return Step(session, "captcha", "GST rejected the login or CAPTCHA. Enter the refreshed CAPTCHA.", image);
        }

        var otp = await FindOtpInputAsync(page);
        if (otp is not null)
            return Step(session, "otp", "GST requested an OTP. Enter the OTP sent by the portal.");

        if (session.Request.ExploreOnly)
            return Step(session, "authenticated", "GST login succeeded. The diagnostic session is ready.");
        return await DownloadAsync(session);
    }

    public async Task<GstPortalStep> SubmitOtpAsync(string sessionId, string otpValue)
    {
        var session = GetSession(sessionId);
        var otp = await FindOtpInputAsync(session.Page!)
            ?? throw new ArgumentException("The GST portal is not currently requesting an OTP.");
        await otp.FillAsync((otpValue ?? "").Trim());
        await session.Page!.Locator("button.btn-primary").Last.ClickAsync();
        await session.Page.WaitForTimeoutAsync(2_500);

        var remaining = await FindOtpInputAsync(session.Page);
        if (remaining is not null)
            return Step(session, "otp", "GST rejected or expired the OTP. Enter the current OTP.");

        if (session.Request.ExploreOnly)
            return Step(session, "authenticated", "GST login succeeded. The diagnostic session is ready.");
        return await DownloadAsync(session);
    }

    public async Task<GstPortalDiagnostic> DiagnoseNavigationAsync(string sessionId)
    {
        var session = GetSession(sessionId);
        var page = session.Page ?? throw new ArgumentException("GST portal page is not available.");
        var stages = new List<string> { $"Authenticated: {page.Url}" };

        var services = await VisibleTextAsync(page, "Services");
        if (services is not null)
        {
            await services.ClickAsync();
            await page.WaitForTimeoutAsync(500);
            stages.Add("Services menu clicked.");
        }
        else
        {
            stages.Add("Visible Services menu was not found.");
        }

        var returns = await VisibleTextAsync(page, "Returns");
        if (returns is not null)
        {
            await returns.ClickAsync();
            await page.WaitForTimeoutAsync(500);
            stages.Add("Returns submenu clicked.");
        }
        else
        {
            stages.Add("Visible Returns submenu was not found.");
        }

        var dashboard = await VisibleTextAsync(page, "Returns Dashboard");
        if (dashboard is not null)
        {
            var pagesBefore = session.Context?.Pages.Count ?? 1;
            await dashboard.ClickAsync();
            await page.WaitForTimeoutAsync(8_000);
            if (session.Context is not null && session.Context.Pages.Count > pagesBefore)
            {
                page = session.Context.Pages.Last();
                session.Page = page;
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                stages.Add("Returns Dashboard opened in a new page.");
            }
            else
            {
                stages.Add("Returns Dashboard click stayed in the current page.");
            }
        }
        else
        {
            stages.Add("Visible Returns Dashboard entry was not found.");
        }

        var links = new List<string>();
        var anchors = page.Locator("a");
        for (var index = 0; index < Math.Min(await anchors.CountAsync(), 150); index++)
        {
            var anchor = anchors.Nth(index);
            if (!await IsVisibleAsync(anchor))
                continue;
            var text = (await anchor.InnerTextAsync()).Trim();
            var href = await anchor.GetAttributeAsync("href") ?? "";
            if (text.Length > 0)
                links.Add($"{text} => {href}");
        }

        var selectDescriptions = new List<string>();
        var selects = page.Locator("select");
        for (var index = 0; index < await selects.CountAsync(); index++)
        {
            var select = selects.Nth(index);
            var id = await select.GetAttributeAsync("id") ?? "";
            var name = await select.GetAttributeAsync("name") ?? "";
            var options = await select.Locator("option").AllTextContentsAsync();
            selectDescriptions.Add(
                $"select[{index}] id='{id}' name='{name}' options=[{string.Join(" | ", options.Select(value => value.Trim()))}]");
        }

        var bodyText = await page.Locator("body").InnerTextAsync();
        if (bodyText.Length > 12_000)
            bodyText = bodyText[..12_000];

        return new GstPortalDiagnostic
        {
            SessionId = session.Id,
            Url = page.Url,
            Title = await page.TitleAsync(),
            PageCount = session.Context?.Pages.Count ?? 1,
            Stages = stages,
            VisibleLinks = links,
            Selects = selectDescriptions,
            BodyText = bodyText
        };
    }

    private async Task<GstPortalStep> BeginNextCredentialAsync(PortalSession session)
    {
        if (session.RemainingCredentials.Count == 0)
            throw new InvalidOperationException("No GST registration remains in this session.");
        if (session.Context is not null)
            await session.Context.DisposeAsync();

        var credential = session.RemainingCredentials.Dequeue();
        session.Credential = credential;
        var browser = await GetBrowserAsync();
        session.Context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            AcceptDownloads = true,
            IgnoreHTTPSErrors = false,
            Locale = "en-IN",
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        });
        await session.Context.AddInitScriptAsync(
            "Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");
        session.Page = await session.Context.NewPageAsync();
        session.Page.SetDefaultTimeout(30_000);
        session.Gstin = credential.Gstin;

        await session.Page.GotoAsync(LoginUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await session.Page.Locator("#username").FillAsync(credential.Login);
        await session.Page.Locator("#user_pass").FillAsync(credential.Password);
        var captchaImage = await CaptureCaptchaAsync(session.Page);
        var months = Gst2BSnapshotService.FormatMonthSpan(session.MonthsToDownload);
        var remaining = session.RemainingCredentials.Count;
        var message = remaining == 0
            ? $"Enter the CAPTCHA to fetch {months}."
            : $"Enter the CAPTCHA to fetch {months}. {remaining} more registration(s) will follow.";
        return Step(session, "captcha", message, captchaImage);
    }

    private async Task<GstPortalStep> DownloadAsync(PortalSession session)
    {
        var workbooks = new List<byte[]>();
        try
        {
            var page = session.Page!;
            var months = session.MonthsToDownload.Count > 0
                ? session.MonthsToDownload
                : Months(session.Request.DateFrom, session.Request.DateTo).ToList();
            foreach (var month in months)
            {
                var downloaded = await DownloadMonthAsync(page, month);
                page = downloaded.Page;
                session.Page = page;
                workbooks.Add(downloaded.File);
                await StoreMonthAsync(downloaded.File, month);
            }

            session.CompletedWorkbooks.Add(CombineWorkbooks(workbooks));
            if (session.RemainingCredentials.Count > 0)
                return await BeginNextCredentialAsync(session);

            var completed = Step(session, "complete", $"Fetched {Gst2BSnapshotService.FormatMonthSpan(months)} and stored them.");
            completed.Workbooks = session.CompletedWorkbooks;
            await RemoveSessionAsync(session.Id);
            return completed;
        }
        catch (Exception ex)
        {
            return Step(
                session,
                "error",
                "GST login succeeded, but the GSTR-2B download could not be completed: " + ex.Message);
        }
    }

    private async Task StoreMonthAsync(byte[] file, DateTime month)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<Gst2BSnapshotService>();
            await using var stream = new MemoryStream(file, writable: false);
            var result = await store.SavePortalFileAsync(stream, $"{month:yyyy-MM}.xlsx");
            if (result.AlreadyFrozen)
                _logger.LogInformation("GSTR-2B {Period} for {Gstin} is already frozen.", month.ToString("yyyy-MM"), result.BuyerGstin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GSTR-2B {Period} downloaded but was not stored.", month.ToString("yyyy-MM"));
        }
    }

    private static async Task<(IPage Page, byte[] File)> DownloadMonthAsync(IPage page, DateTime month)
    {
        page = await EnsureReturnsDashboardAsync(page);

        var financialYearStart = month.Month >= 4 ? month.Year : month.Year - 1;
        var financialYear = $"{financialYearStart}-{(financialYearStart + 1) % 100:00}";
        await SelectPortalFieldAsync(
            page,
            "Financial Year",
            financialYear,
            $"{financialYearStart} - {(financialYearStart + 1) % 100:00}",
            $"{financialYearStart}-{financialYearStart + 1}");
        await page.WaitForTimeoutAsync(750);
        await SelectPortalFieldAsync(page, "Quarter", QuarterLabel(month.Month));
        await page.WaitForTimeoutAsync(750);
        await SelectPortalFieldAsync(
            page,
            "Period",
            month.ToString("MMMM", CultureInfo.InvariantCulture),
            month.ToString("MMM", CultureInfo.InvariantCulture));

        var search = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex("^SEARCH$", RegexOptions.IgnoreCase)
        });
        await search.ClickAsync();
        await page.WaitForTimeoutAsync(2_000);

        var gstr2b = page.GetByText("GSTR-2B", new PageGetByTextOptions { Exact = false }).First;
        await gstr2b.WaitForAsync();
        var tile = gstr2b.Locator("xpath=ancestor::*[self::div or self::section][.//button][1]");
        var downloadButton = tile.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("DOWNLOAD", RegexOptions.IgnoreCase)
        }).First;
        await downloadButton.ClickAsync();
        await page.WaitForTimeoutAsync(1_500);

        var directExcel = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex("DOWNLOAD.*(SUMMARY)?.*EXCEL", RegexOptions.IgnoreCase)
        });
        if (await directExcel.CountAsync() > 0 && await IsVisibleAsync(directExcel.First))
            return (page, NormalizeWorkbook(await ClickAndReadDownloadAsync(page, directExcel.First)));

        return (page, await DownloadGeneratedExcelAsync(page, month));
    }

    private static async Task<byte[]> DownloadGeneratedExcelAsync(IPage page, DateTime month)
    {
        var ready = await FindExcelDownloadLinkAsync(page);
        if (ready is not null)
            return NormalizeWorkbook(await ClickAndReadDownloadAsync(page, ready));

        var generate = await WaitForGenerateExcelAsync(page);
        try
        {
            return NormalizeWorkbook(await ClickAndReadDownloadAsync(page, generate));
        }
        catch (TimeoutException)
        {
            var ok = await FirstVisibleRoleAsync(page, AriaRole.Button, "^OK$");
            if (ok is not null)
                return NormalizeWorkbook(await ClickAndReadDownloadAsync(page, ok));
        }

        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            var downloaded = await ReadReadyExcelFilesAsync(page);
            if (downloaded is not null)
                return downloaded;
            await page.WaitForTimeoutAsync(3_000);
        }

        var labels = await VisibleControlLabelsAsync(page);
        throw new InvalidOperationException(
            $"The GSTR-2B Excel did not download for {month:MMM yyyy}. Visible controls: {labels}");
    }

    private static async Task<ILocator> WaitForGenerateExcelAsync(IPage page)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var generate = await FirstVisibleRoleAsync(page, AriaRole.Button, "GENERATE\\s+EXCEL\\s+FILE\\s+TO\\s+DOWNLOAD");
            if (generate is not null)
                return generate;
            await page.WaitForTimeoutAsync(1_000);
        }
        throw new InvalidOperationException("GENERATE EXCEL FILE TO DOWNLOAD was not visible. Visible controls: " + await VisibleControlLabelsAsync(page));
    }

    private static async Task<IPage> EnsureReturnsDashboardAsync(IPage page)
    {
        if (page.Url.Contains("return.gst.gov.in/returns", StringComparison.OrdinalIgnoreCase))
        {
            var offline = await FirstVisibleRoleAsync(page, AriaRole.Button, "GENERATE\\s+EXCEL\\s+FILE\\s+TO\\s+DOWNLOAD");
            var back = await FirstVisibleRoleAsync(page, AriaRole.Button, "^BACK$");
            if (offline is not null && back is not null)
            {
                await back.ClickAsync();
                await page.WaitForTimeoutAsync(1_500);
            }
            if (await page.Locator("select").CountAsync() > 0)
                return page;
        }
        return await NavigateToReturnsDashboardAsync(page);
    }

    private static async Task<byte[]?> ReadReadyExcelFilesAsync(IPage page)
    {
        var links = await VisibleRolesAsync(page, AriaRole.Link, "download\\s+excel|click here to download");
        if (links.Count == 0)
            return null;
        var files = new List<byte[]>();
        foreach (var link in links)
            files.Add(NormalizeWorkbook(await ClickAndReadDownloadAsync(page, link)));
        return CombineWorkbooks(files);
    }

    private static async Task<string> VisibleControlLabelsAsync(IPage page)
    {
        var labels = new List<string>();
        foreach (var frame in page.Frames)
        {
            var controls = frame.Locator("a, button, input[type='button'], input[type='submit']");
            for (var index = 0; index < Math.Min(await controls.CountAsync(), 80); index++)
            {
                var control = controls.Nth(index);
                if (!await IsVisibleAsync(control))
                    continue;
                var label = await control.InnerTextAsync();
                if (string.IsNullOrWhiteSpace(label))
                    label = await control.GetAttributeAsync("value") ?? "";
                label = Regex.Replace(label, "\\s+", " ").Trim();
                if (label.Length == 0 || label.Length > 80)
                    continue;
                if (label.Contains("excel", StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("download", StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("generate", StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("json", StringComparison.OrdinalIgnoreCase))
                    labels.Add(label);
            }
        }
        var text = string.Join(" || ", labels.Distinct(StringComparer.OrdinalIgnoreCase));
        return text.Length == 0 ? "(none)" : text;
    }

    private static Task<ILocator?> FindExcelDownloadLinkAsync(IPage page) =>
        FirstVisibleRoleAsync(page, AriaRole.Link, "download\\s+excel|click here to download");

    private static async Task<ILocator?> FirstVisibleRoleAsync(IPage page, AriaRole role, string pattern)
    {
        var matches = await VisibleRolesAsync(page, role, pattern);
        return matches.Count == 0 ? null : matches[0];
    }

    private static async Task<List<ILocator>> VisibleRolesAsync(IPage page, AriaRole role, string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var found = new List<ILocator>();
        foreach (var frame in page.Frames)
        {
            var matches = frame.GetByRole(role, new FrameGetByRoleOptions { NameRegex = regex });
            for (var index = 0; index < await matches.CountAsync(); index++)
            {
                var candidate = matches.Nth(index);
                if (await IsVisibleAsync(candidate))
                    found.Add(candidate);
            }
        }
        return found;
    }

    private static byte[] NormalizeWorkbook(byte[] data)
    {
        if (data.Length < 4 || data[0] != (byte)'P' || data[1] != (byte)'K')
            return data;
        using var archive = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read, leaveOpen: false);
        var workbooks = new List<byte[]>();
        foreach (var entry in archive.Entries)
        {
            if (!entry.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || entry.Length == 0)
                continue;
            using var source = entry.Open();
            using var copy = new MemoryStream();
            source.CopyTo(copy);
            workbooks.Add(copy.ToArray());
        }
        if (workbooks.Count == 0)
            return data;
        return CombineWorkbooks(workbooks);
    }

    private static async Task<IPage> NavigateToReturnsDashboardAsync(IPage page)
    {
        if (await TryOpenReturnsDashboardAsync(page))
            return page;

        if (!page.Url.Contains("/services/auth/fowelcome", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync("https://services.gst.gov.in/services/auth/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
        }
        await page.WaitForTimeoutAsync(1_500);

        var quick = await FirstVisibleAsync(page, "button, a, span", "Return Dashboard");
        if (quick is not null)
        {
            var opened = await ClickAndFollowAsync(page, quick);
            if (!opened.Url.Contains("accessdenied", StringComparison.OrdinalIgnoreCase))
            {
                await WaitForReturnsDashboardAsync(opened);
                return opened;
            }
            if (opened != page)
                await opened.CloseAsync();
            else
                await page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        var services = await VisibleTextAsync(page, "Services");
        if (services is not null)
            await services.ClickAsync();
        await page.WaitForTimeoutAsync(500);
        var returns = await VisibleTextAsync(page, "Returns");
        if (returns is not null)
            await returns.ClickAsync();

        var dashboard = await WaitForVisibleAsync(page, "a, button, span, li", "Returns Dashboard", TimeSpan.FromSeconds(20));
        if (dashboard is null)
            throw new InvalidOperationException("Returns Dashboard did not open. Fetch from GST again.");

        var target = await ClickAndFollowAsync(page, dashboard);
        if (target.Url.Contains("accessdenied", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("GST opened Access Denied instead of the Returns Dashboard.");
        await WaitForReturnsDashboardAsync(target);
        return target;
    }

    private static async Task<bool> TryOpenReturnsDashboardAsync(IPage page)
    {
        try
        {
            await page.GotoAsync(DashboardUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            if (page.Url.Contains("accessdenied", StringComparison.OrdinalIgnoreCase) || page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
                return false;
            await page.Locator("select").First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15_000
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static async Task<ILocator?> WaitForVisibleAsync(IPage page, string selector, string text, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var found = await FirstVisibleAsync(page, selector, text);
            if (found is not null)
                return found;
            await page.WaitForTimeoutAsync(500);
        }
        return null;
    }

    private static async Task<IPage> ClickAndFollowAsync(IPage page, ILocator control)
    {
        var popup = page.Context.WaitForPageAsync(new BrowserContextWaitForPageOptions { Timeout = 8_000 });
        await control.ClickAsync();
        try
        {
            var opened = await popup;
            await opened.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            return opened;
        }
        catch (TimeoutException)
        {
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            return page;
        }
    }

    private static async Task<ILocator?> FirstVisibleAsync(IPage page, string selector, string text)
    {
        var matches = page.Locator(selector).Filter(new LocatorFilterOptions
        {
            HasTextRegex = new Regex(Regex.Escape(text), RegexOptions.IgnoreCase)
        });
        for (var index = 0; index < await matches.CountAsync(); index++)
        {
            var match = matches.Nth(index);
            if (await IsVisibleAsync(match))
                return match;
        }
        return null;
    }

    private static string NavigationSnapshotPath() => Path.Combine(Path.GetTempPath(), "gst-nav-debug.txt");

    private static async Task<ILocator?> MenuLinkAsync(IPage page, string text)
    {
        var anchors = page.Locator("a");
        ILocator? fallback = null;
        for (var index = 0; index < await anchors.CountAsync(); index++)
        {
            var anchor = anchors.Nth(index);
            if (!await IsVisibleAsync(anchor))
                continue;
            var label = (await anchor.InnerTextAsync()).Replace("\n", " ").Trim();
            if (!label.Contains(text, StringComparison.OrdinalIgnoreCase))
                continue;
            var href = await anchor.GetAttributeAsync("href") ?? "";
            if (href.Contains("return.gst.gov.in", StringComparison.OrdinalIgnoreCase))
                return anchor;
            fallback ??= anchor;
        }
        return fallback;
    }

    private static async Task WriteNavigationSnapshotAsync(IPage page, string stage)
    {
        var lines = new List<string> { $"stage={stage}", $"url={page.Url}" };
        var controls = page.Locator("a, button");
        for (var index = 0; index < Math.Min(await controls.CountAsync(), 250); index++)
        {
            var control = controls.Nth(index);
            if (!await IsVisibleAsync(control))
                continue;
            var label = (await control.InnerTextAsync()).Replace("\n", " ").Trim();
            if (label.Length == 0 || label.Length > 120)
                continue;
            var href = await control.GetAttributeAsync("href") ?? "";
            var onclick = await control.GetAttributeAsync("onclick") ?? "";
            if (label.Contains("Return", StringComparison.OrdinalIgnoreCase) ||
                label.Contains("Service", StringComparison.OrdinalIgnoreCase) ||
                href.Contains("return", StringComparison.OrdinalIgnoreCase) ||
                onclick.Contains("return", StringComparison.OrdinalIgnoreCase))
                lines.Add($"{label} | {href} | {onclick}");
        }
        await File.WriteAllLinesAsync(NavigationSnapshotPath(), lines);
    }

    private static async Task WaitForReturnsDashboardAsync(IPage page)
    {
        if (page.Url.Contains("accessdenied", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("GST redirected the browser to Access Denied while opening Returns Dashboard.");
        await page.Locator("select").First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await page.WaitForTimeoutAsync(750);
    }

    private static async Task<ILocator?> VisibleTextAsync(IPage page, string text)
    {
        var matches = page.GetByText(text, new PageGetByTextOptions { Exact = true });
        for (var index = 0; index < await matches.CountAsync(); index++)
        {
            var match = matches.Nth(index);
            if (await IsVisibleAsync(match))
                return match;
        }
        return null;
    }

    private static async Task SelectPortalFieldAsync(IPage page, string label, params string[] choices)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var labelled = page.GetByLabel(label, new PageGetByLabelOptions { Exact = false });
            for (var index = 0; index < await labelled.CountAsync(); index++)
            {
                var control = labelled.Nth(index);
                if (!await IsVisibleAsync(control))
                    continue;
                var tag = await control.EvaluateAsync<string>("element => element.tagName.toLowerCase()");
                if (tag == "select" && await SelectFromElementAsync(control, choices))
                    return;

                await control.ClickAsync();
                foreach (var choice in choices)
                {
                    var option = page.GetByRole(AriaRole.Option, new PageGetByRoleOptions
                    {
                        NameRegex = new Regex(Regex.Escape(choice), RegexOptions.IgnoreCase)
                    });
                    for (var optionIndex = 0; optionIndex < await option.CountAsync(); optionIndex++)
                    {
                        if (!await IsVisibleAsync(option.Nth(optionIndex)))
                            continue;
                        await option.Nth(optionIndex).ClickAsync();
                        return;
                    }
                }
            }

            var selects = page.Locator("select");
            for (var index = 0; index < await selects.CountAsync(); index++)
            {
                if (await SelectFromElementAsync(selects.Nth(index), choices))
                    return;
            }
            await page.WaitForTimeoutAsync(500);
        }
        throw new InvalidOperationException(
            $"GST portal {label} option '{choices[0]}' was not found after opening Services > Returns > Returns Dashboard.");
    }

    private static async Task<bool> SelectFromElementAsync(ILocator select, IReadOnlyList<string> choices)
    {
        var options = await select.Locator("option").AllTextContentsAsync();
        foreach (var choice in choices)
        {
            var normalizedChoice = Normalize(choice);
            var match = options.FirstOrDefault(option =>
                Normalize(option).Contains(normalizedChoice, StringComparison.OrdinalIgnoreCase));
            if (match is null)
                continue;
            await select.SelectOptionAsync(new SelectOptionValue { Label = match });
            return true;
        }
        return false;
    }

    private static async Task<byte[]> ClickAndReadDownloadAsync(IPage page, ILocator control)
    {
        var download = await page.RunAndWaitForDownloadAsync(
            async () => await control.ClickAsync(),
            new PageRunAndWaitForDownloadOptions { Timeout = 120_000 });
        var tempPath = Path.Combine(Path.GetTempPath(), "gst-2b-" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            await download.SaveAsAsync(tempPath);
            return await File.ReadAllBytesAsync(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static byte[] CombineWorkbooks(IReadOnlyList<byte[]> files)
    {
        if (files.Count == 0)
            throw new InvalidOperationException("GST did not download a GSTR-2B workbook.");
        if (files.Count == 1)
            return files[0];

        using var output = new XLWorkbook();
        var target = output.Worksheets.Add("B2B");
        var destinationRow = 1;

        for (var fileIndex = 0; fileIndex < files.Count; fileIndex++)
        {
            using var input = new XLWorkbook(new MemoryStream(files[fileIndex]));
            var source = input.Worksheets.FirstOrDefault(sheet =>
                    Normalize(sheet.Name).Contains("B2B") &&
                    (Normalize(sheet.Name).Contains("CNDN") || Normalize(sheet.Name).Contains("CN")))
                ?? input.Worksheets.FirstOrDefault(sheet => Normalize(sheet.Name).Contains("B2B"))
                ?? throw new InvalidOperationException("A downloaded workbook has no Invoice Wise 2B sheet.");

            var lastRow = source.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = source.LastColumnUsed()?.ColumnNumber() ?? 0;
            var headerRow = FindTwoBHeader(source, lastRow, lastColumn);
            var firstRow = fileIndex == 0 ? 1 : headerRow + 1;
            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var column = 1; column <= lastColumn; column++)
                    target.Cell(destinationRow, column).Value = source.Cell(row, column).Value;
                destinationRow++;
            }
        }

        using var stream = new MemoryStream();
        output.SaveAs(stream);
        return stream.ToArray();
    }

    private static int FindTwoBHeader(IXLWorksheet sheet, int lastRow, int lastColumn)
    {
        for (var row = 1; row <= Math.Min(lastRow, 25); row++)
        {
            for (var column = 1; column <= lastColumn; column++)
            {
                if (Normalize(sheet.Cell(row, column).GetString()) == "2BPERIOD")
                    return row;
            }
        }
        throw new InvalidOperationException("A downloaded workbook has no 2B Period header.");
    }

    private async Task<List<PortalCredential>> ResolveCredentialsAsync(string groupLabel, CancellationToken ct)
    {
        var gstins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var connection = _database.CreateConnection())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT LTRIM(RTRIM(ISNULL(NewGSTNo, '')))
FROM FactoryInfo WITH (NOLOCK)
WHERE COALESCE(NULLIF(LTRIM(RTRIM(GroupName)), ''), LTRIM(RTRIM(Name))) = @GroupLabel";
            command.Parameters.Add("@GroupLabel", System.Data.SqlDbType.NVarChar, 150).Value = groupLabel.Trim();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var gstin = NormalizeGstin(Convert.ToString(reader.GetValue(0)));
                if (gstin.Length > 0)
                    gstins.Add(gstin);
            }
        }
        if (gstins.Count == 0)
            throw new ArgumentException($"No GSTIN is configured for company group {groupLabel}.");

        var workbookPath = CredentialWorkbookPath();
        if (!File.Exists(workbookPath))
            throw new InvalidOperationException("GST credential workbook was not found on the API server.");

        var matches = new List<PortalCredential>();
        using var file = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var workbook = new XLWorkbook(file);
        var sheet = workbook.Worksheets.First();
        var header = sheet.FirstRowUsed()?.RowNumber() ?? 1;
        var columns = Enumerable.Range(1, sheet.LastColumnUsed()?.ColumnNumber() ?? 0)
            .ToDictionary(column => Normalize(sheet.Cell(header, column).GetString()), column => column);
        var gstinColumn = RequiredColumn(columns, "GSTIN");
        var loginColumn = RequiredColumn(columns, "LOGIN");
        var passwordColumn = RequiredColumn(columns, "PASSWORD");

        for (var row = header + 1; row <= (sheet.LastRowUsed()?.RowNumber() ?? header); row++)
        {
            var gstin = NormalizeGstin(sheet.Cell(row, gstinColumn).GetString());
            if (!gstins.Contains(gstin))
                continue;
            var login = sheet.Cell(row, loginColumn).GetString().Trim();
            var password = sheet.Cell(row, passwordColumn).GetString();
            if (login.Length > 0 && password.Length > 0)
                matches.Add(new PortalCredential(gstin, login, password));
        }

        var distinct = matches.GroupBy(item => item.Gstin, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First()).ToList();
        if (distinct.Count == 0)
            throw new ArgumentException($"No GST portal credential matches the GSTINs in {groupLabel}.");
        return distinct;
    }

    private string CredentialWorkbookPath()
    {
        var configured = _configuration["GstPortal:CredentialWorkbook"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);
        return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "gst login credential.xls.xlsx"));
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null)
            return _browser;
        await _browserLock.WaitAsync();
        try
        {
            if (_browser is not null)
                return _browser;
            _playwright = await Playwright.CreateAsync();
            var options = new BrowserTypeLaunchOptions
            {
                Headless = false,
                Channel = "chrome",
                IgnoreDefaultArgs = ["--enable-automation"],
                Args = ["--disable-blink-features=AutomationControlled"]
            };
            try
            {
                _browser = await _playwright.Chromium.LaunchAsync(options);
            }
            catch (PlaywrightException)
            {
                options.Channel = "msedge";
                _browser = await _playwright.Chromium.LaunchAsync(options);
            }
            return _browser;
        }
        catch (PlaywrightException ex)
        {
            throw new InvalidOperationException(
                "The GST browser runtime is not installed. Run the Playwright Chromium install command on the API host.",
                ex);
        }
        finally
        {
            _browserLock.Release();
        }
    }

    private PortalSession GetSession(string id)
    {
        if (!_sessions.TryGetValue((id ?? "").Trim(), out var session))
            throw new ArgumentException("GST portal session was not found or has expired.");
        if (DateTimeOffset.UtcNow - session.CreatedAt > SessionLifetime)
        {
            _ = RemoveSessionAsync(session.Id);
            throw new ArgumentException("GST portal session expired. Start again.");
        }
        return session;
    }

    private async Task RemoveExpiredSessionsAsync()
    {
        var expired = _sessions.Values
            .Where(session => DateTimeOffset.UtcNow - session.CreatedAt > SessionLifetime)
            .Select(session => session.Id).ToList();
        foreach (var id in expired)
            await RemoveSessionAsync(id);
    }

    private async Task RemoveSessionAsync(string id)
    {
        if (_sessions.TryRemove(id, out var session) && session.Context is not null)
            await session.Context.DisposeAsync();
    }

    private static async Task<string> CaptureCaptchaAsync(IPage page)
    {
        var image = page.Locator("#imgCaptcha");
        await image.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        for (var attempt = 0; attempt < 40; attempt++)
        {
            var ready = await image.EvaluateAsync<bool>(
                "element => element.tagName === 'IMG' && element.complete && element.naturalWidth > 20 && element.naturalHeight > 10");
            if (ready)
            {
                try
                {
                    var dataUrl = await image.EvaluateAsync<string>(
                        """
                        element => {
                            const canvas = document.createElement('canvas');
                            canvas.width = element.naturalWidth;
                            canvas.height = element.naturalHeight;
                            const context = canvas.getContext('2d');
                            context.drawImage(element, 0, 0);
                            return canvas.toDataURL('image/png');
                        }
                        """);
                    if (!string.IsNullOrWhiteSpace(dataUrl) && dataUrl.Length > 200)
                        return dataUrl;
                }
                catch (PlaywrightException)
                {
                    break;
                }
            }
            await page.WaitForTimeoutAsync(250);
        }

        var bytes = await image.ScreenshotAsync();
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    private static async Task<ILocator?> FindOtpInputAsync(IPage page)
    {
        foreach (var selector in new[] { "#otp", "#mobileOtp", "input[name*='otp']", "input[id*='otp']" })
        {
            var locator = page.Locator(selector).First;
            if (await IsVisibleAsync(locator))
                return locator;
        }
        return null;
    }

    private static async Task<bool> IsVisibleAsync(ILocator locator)
    {
        try
        {
            return await locator.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<DateTime> Months(DateTime from, DateTime to)
    {
        var current = new DateTime(from.Year, from.Month, 1);
        var last = new DateTime(to.Year, to.Month, 1);
        while (current <= last)
        {
            yield return current;
            current = current.AddMonths(1);
        }
    }

    private static string QuarterLabel(int month) => month switch
    {
        >= 4 and <= 6 => "Apr - Jun",
        >= 7 and <= 9 => "Jul - Sep",
        >= 10 and <= 12 => "Oct - Dec",
        _ => "Jan - Mar"
    };

    private static int RequiredColumn(Dictionary<string, int> columns, string name)
    {
        var match = columns.FirstOrDefault(pair => pair.Key == name || pair.Key.Contains(name, StringComparison.Ordinal));
        return match.Value > 0
            ? match.Value
            : throw new InvalidOperationException($"GST credential workbook is missing the {name} column.");
    }

    private static string Normalize(string? value) =>
        new((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string NormalizeGstin(string? value) =>
        new((value ?? "").Where(character => !char.IsWhiteSpace(character)).Select(char.ToUpperInvariant).ToArray());

    private static GstPortalStep Step(PortalSession session, string status, string message, string captcha = "") =>
        new()
        {
            SessionId = session.Id,
            Status = status,
            Gstin = session.Gstin,
            CaptchaImage = captcha,
            Message = message,
            CompanyName = session.Request.CompanyName,
            DateFrom = session.Request.DateFrom,
            DateTo = session.Request.DateTo,
            AmountTolerance = session.Request.AmountTolerance
        };

    public async ValueTask DisposeAsync()
    {
        foreach (var id in _sessions.Keys)
            await RemoveSessionAsync(id);
        if (_browser is not null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
        _browserLock.Dispose();
    }

    private sealed record PortalCredential(string Gstin, string Login, string Password);

    private sealed class PortalSession
    {
        public string Id { get; set; } = "";
        public IBrowserContext? Context { get; set; }
        public IPage? Page { get; set; }
        public GstPortalStartRequest Request { get; set; } = new();
        public string Gstin { get; set; } = "";
        public PortalCredential? Credential { get; set; }
        public List<DateTime> MonthsToDownload { get; set; } = [];
        public Queue<PortalCredential> RemainingCredentials { get; set; } = new();
        public List<byte[]> CompletedWorkbooks { get; set; } = [];
        public DateTimeOffset CreatedAt { get; set; }
    }
}
