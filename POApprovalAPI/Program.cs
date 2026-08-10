using POApprovalAPI.Services;
using QuestPDF.Infrastructure;
using POApprovalAPI.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Load secrets from environment variables
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? "";

// Build connection strings with secrets from environment
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var loginConnection = builder.Configuration.GetConnectionString("LoginEntryConnection");

if (!string.IsNullOrEmpty(dbPassword))
{
    defaultConnection = $"{defaultConnection}Password={dbPassword};";
    loginConnection = $"{loginConnection}Password={dbPassword};";
}

// Override connection strings in configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;
builder.Configuration["ConnectionStrings:LoginEntryConnection"] = loginConnection;

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
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
builder.Services.AddScoped<ExcelLedgerService>();

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