using POApprovalAPI.Services;
using QuestPDF.Infrastructure;
using POApprovalAPI.Interfaces;

LoadDotEnvFiles();

var builder = WebApplication.CreateBuilder(args);

// Load secrets from environment variables
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? "";

// Build connection strings with secrets from environment
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var loginConnection = builder.Configuration.GetConnectionString("LoginEntryConnection");
var productionConnection = builder.Configuration.GetConnectionString("ProductionConnection");

if (!string.IsNullOrEmpty(dbPassword))
{
    defaultConnection = $"{defaultConnection}Password={dbPassword};";
    loginConnection = $"{loginConnection}Password={dbPassword};";
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
builder.Services.AddScoped<SalesDashboardService>();
builder.Services.AddScoped<ExcelLedgerService>();
builder.Services.AddScoped<BillWiseTransactionService>();
builder.Services.AddScoped<LedgerSummaryService>();
builder.Services.AddScoped<BomService>();

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