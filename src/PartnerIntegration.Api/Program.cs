using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using PartnerIntegration.Api.Middleware;
using PartnerIntegration.Api.Security;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Services;
using PartnerIntegration.Application.Validation;
using PartnerIntegration.Infrastructure.Messaging;
using PartnerIntegration.Infrastructure.PartnerVerification;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Partner Integration BFF",
        Version = "v1",
        Description = "Accepts partner transactions, verifies the partner, and queues work for legacy systems."
    });

    options.AddSecurityDefinition(ApiKeyOptions.SchemeName, new OpenApiSecurityScheme
    {
        Description = $"API key sent in the {ApiKeyOptions.HeaderName} header.",
        Type = SecuritySchemeType.ApiKey,
        Name = ApiKeyOptions.HeaderName,
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ApiKeyOptions.SchemeName
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<PartnerVerificationOptions>(builder.Configuration.GetSection(PartnerVerificationOptions.SectionName));

builder.Services
    .AddAuthentication(ApiKeyOptions.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyOptions.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddValidatorsFromAssembly(Assembly.GetAssembly(typeof(PartnerTransactionRequestValidator)));
builder.Services.AddSingleton<IRandomProvider, SystemRandomProvider>();
builder.Services.AddSingleton<FlakyPartnerVerificationService>();
builder.Services.AddScoped<PartnerTransactionProcessor>();

var verificationOptions = builder.Configuration
    .GetSection(PartnerVerificationOptions.SectionName)
    .Get<PartnerVerificationOptions>() ?? new PartnerVerificationOptions();

builder.Services.AddHttpClient<HttpPartnerVerificationClient>(client =>
{
    client.BaseAddress = new Uri(verificationOptions.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(verificationOptions.HttpTimeoutSeconds);
});

builder.Services.AddTransient<IPartnerVerificationClient>(sp =>
    new ResilientPartnerVerificationClient(
        sp.GetRequiredService<HttpPartnerVerificationClient>(),
        sp.GetRequiredService<IOptions<PartnerVerificationOptions>>(),
        sp.GetRequiredService<ILogger<ResilientPartnerVerificationClient>>()));

builder.Services.AddSingleton<ITransactionQueuePublisher, RabbitMqTransactionPublisher>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
