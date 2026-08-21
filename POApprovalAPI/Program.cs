using POApprovalAPI.Planning.Execution;
using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Integrated;
using POApprovalAPI.Planning.Loom;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Services;
using QuestPDF.Infrastructure;
using POApprovalAPI.Interfaces;

LoadDotEnvFiles();

var builder = WebApplication.CreateBuilder(args);

// Load secrets: env var first, then built-in defaults for Render/production.
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
if (string.IsNullOrWhiteSpace(dbPassword))
    dbPassword = AppSecretsDefaults.DbPassword;

var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
if (string.IsNullOrWhiteSpace(emailPassword))
    emailPassword = AppSecretsDefaults.EmailPassword;

// Build connection strings with secrets
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var loginConnection = builder.Configuration.GetConnectionString("LoginEntryConnection");
var productionConnection = builder.Configuration.GetConnectionString("ProductionConnection");

if (!string.IsNullOrEmpty(dbPassword))
{
    defaultConnection = $"{defaultConnection}Password={dbPassword};";
    loginConnection = $"{loginConnection}Password={dbPassword};";
    if (!string.IsNullOrEmpty(productionConnection))
        productionConnection = $"{productionConnection}Password={dbPassword};";
}

if (!string.IsNullOrEmpty(emailPassword))
    builder.Configuration["EmailSettings:Password"] = emailPassword;

// Override connection strings in configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;
builder.Configuration["ConnectionStrings:LoginEntryConnection"] = loginConnection;
builder.Configuration["ConnectionStrings:ProductionConnection"] = productionConnection;

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 60_000_000;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<DailyReportService>();
builder.Services.AddScoped<HtmlParserService>();
builder.Services.AddScoped<MessageFormatterService>();
builder.Services.AddScoped<ManagerService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<DailyReportProcessorService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PoApprovalService>();
builder.Services.AddScoped<WorkOrderApprovalService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient(nameof(DmsRemoteFileService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddScoped<DmsAttachmentService>();
builder.Services.AddScoped<DmsRemoteFileService>();
builder.Services.AddScoped<SalesDashboardService>();
builder.Services.AddScoped<ExcelLedgerService>();
builder.Services.AddScoped<BillWiseTransactionService>();
builder.Services.AddScoped<LedgerSummaryService>();
builder.Services.AddScoped<BomService>();
builder.Services.Configure<FibcPlanningOptions>(builder.Configuration.GetSection("FibcPlanning"));
builder.Services.Configure<LoomPlanningOptions>(builder.Configuration.GetSection("LoomPlanning"));
builder.Services.AddScoped<IFibcPlanningRepository, FibcPlanningRepository>();
builder.Services.AddScoped<IFibcQuotationHoldRepository, FibcQuotationHoldRepository>();
builder.Services.AddScoped<IFibcPlanningEngine, FibcPlanningEngine>();
builder.Services.AddScoped<IFibcCriticalShiftEngine, FibcCriticalShiftEngine>();
builder.Services.AddScoped<FibcPlanningService>();
builder.Services.AddScoped<FibcQuotationHoldService>();
builder.Services.AddScoped<FibcPlanningEmailNotifier>();
builder.Services.AddHostedService<FibcQuotationHoldExpiryReminderService>();
builder.Services.AddScoped<ILoomPlanningRepository, LoomPlanningRepository>();
builder.Services.AddScoped<ILoomPlanningEngine, LoomPlanningEngine>();
builder.Services.AddScoped<LoomPlanningService>();
builder.Services.AddScoped<IntegratedPlanningService>();
builder.Services.AddScoped<IPlanningSetupRepository, PlanningSetupRepository>();
builder.Services.AddScoped<PlanningSetupService>();
builder.Services.AddScoped<OrderPlanningRouteService>();
builder.Services.AddScoped<PlanningRuntimeContextLoader>();
builder.Services.AddScoped<ExecutionPlanningService>();
builder.Services.AddSingleton<BomEmailBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BomEmailBackgroundService>());
builder.Services.AddHostedService<BomCacheWarmupService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var holdRepo = scope.ServiceProvider.GetRequiredService<IFibcQuotationHoldRepository>();
    await holdRepo.EnsureSchemaAsync();
    app.Logger.LogInformation("FibcQuotationHold schema ready.");

    var setupRepo = scope.ServiceProvider.GetRequiredService<IPlanningSetupRepository>();
    await setupRepo.EnsureSchemaAsync();
    app.Logger.LogInformation("Planning setup schema ready.");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to initialize planning portal tables. Quotation holds / setup may not work until this is fixed.");
}

app.UseCors("AllowFrontend");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok("PO Approval API is running!"));
app.Run();

static void LoadDotEnvFiles()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
    };

    foreach (var path in candidates)
    {
        LoadDotEnvFile(path);
    }
}

static void LoadDotEnvFile(string path)
{
    if (!File.Exists(path))
        return;

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        var separator = line.IndexOf('=');
        if (separator <= 0)
            continue;

        var key = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim().Trim('"');
        if (string.IsNullOrEmpty(key))
            continue;

        // Do not override variables already set (e.g. launchSettings / shell)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, value);
    }
}