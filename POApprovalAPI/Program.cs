using POApprovalAPI.Services;
using QuestPDF.Infrastructure;
using POApprovalAPI.Interfaces;


var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
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