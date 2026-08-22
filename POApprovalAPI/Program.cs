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
builder.Services.AddScoped<AdvancePaymentService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient(nameof(DmsRemoteFileService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddScoped<DmsAttachmentService>();
builder.Services.AddScoped<DmsRemoteFileService>();
builder.Services.AddScoped<SalesDashboardService>();
builder.Services.AddScoped<ExportBillOverdueService>();
builder.Services.AddScoped<ExcelLedgerService>();
builder.Services.AddScoped<BillWiseTransactionService>();
builder.Services.AddScoped<LedgerSummaryService>();
builder.Services.AddScoped<PnlService>();
builder.Services.AddScoped<IntercompanyBalanceService>();
builder.Services.AddScoped<BomService>();
builder.Services.AddScoped<DailyProductionPriceService>();
builder.Services.AddSingleton<BomEmailBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BomEmailBackgroundService>());
builder.Services.AddHostedService<BomCacheWarmupService>();
builder.Services.AddHostedService<SalesDashboardCacheWarmupService>();
builder.Services.AddHostedService<ExportBillOverdueCacheWarmupService>();
builder.Services.AddScoped<AgeingReportService>();
builder.Services.AddScoped<LedgerStatementChatService>();
builder.Services.AddScoped<ErpFinanceReportService>();
builder.Services.AddScoped<ErpInventoryReportService>();

builder.Services.AddSingleton<SchemaRetrievalService>();
builder.Services.AddSingleton<SchemaCatalogService>();
builder.Services.AddSingleton<SqlGuardService>();

var llmProvider = (Environment.GetEnvironmentVariable("LLM_PROVIDER")
                   ?? builder.Configuration["Llm:Provider"]
                   ?? "gemini").Trim().ToLowerInvariant();
if (llmProvider == "groq")
{
    builder.Services.AddHttpClient<GroqChatService>();
    builder.Services.AddScoped<IChatCompletionService>(sp => sp.GetRequiredService<GroqChatService>());
}
else
{
    builder.Services.AddHttpClient<GeminiChatService>();
    builder.Services.AddScoped<IChatCompletionService>(sp => sp.GetRequiredService<GeminiChatService>());
}

builder.Services.AddScoped<ChatEntityResolutionService>();
builder.Services.AddScoped<ChatOrchestratorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("X-Row-Count", "X-Truncated", "X-Total-Count");
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok("PO Approval API is running!"));
app.MapGet("/api/health", () => Results.Ok(new { ok = true, service = "PO Approval API" }));
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
        var value = line[(separator + 1)..].Trim();
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            value = value[1..^1];
        }

        if (string.IsNullOrEmpty(key))
            continue;

        // Do not override variables already set (e.g. launchSettings / shell)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, value);
    }
}
