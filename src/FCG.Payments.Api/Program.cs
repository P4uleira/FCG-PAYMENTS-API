using FCG.Payments.Api.Middlewares;
using FCG.Payments.Application.Commands.ProcessPayment;
using FCG.Payments.Application.Consumers;
using FCG.Payments.Infrastructure;
using FCG.Payments.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

#region Controllers

builder.Services.AddControllers();

#endregion

#region Swagger

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "FCG Payments API",
            Version = "v1",
            Description =
                "Microsserviço responsável pelo processamento de pagamentos da FIAP Cloud Games."
        });
});

#endregion

#region MediatR

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(
        typeof(ProcessPaymentCommand).Assembly);
});

#endregion

#region Infrastructure

builder.Services.AddInfrastructure(
    builder.Configuration);

#endregion

#region MassTransit

builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<OrderPlacedEventConsumer>();

    config.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig =
            builder.Configuration.GetSection("RabbitMq");

        var rabbitMqHost =
            rabbitMqConfig["Host"]
            ?? throw new InvalidOperationException(
                "A configuração RabbitMq:Host não foi encontrada.");

        var rabbitMqUsername =
            rabbitMqConfig["Username"]
            ?? throw new InvalidOperationException(
                "A configuração RabbitMq:Username não foi encontrada.");

        var rabbitMqPassword =
            rabbitMqConfig["Password"]
            ?? throw new InvalidOperationException(
                "A configuração RabbitMq:Password não foi encontrada.");

        cfg.Host(
            rabbitMqHost,
            "/",
            host =>
            {
                host.Username(rabbitMqUsername);
                host.Password(rabbitMqPassword);
            });

        cfg.ReceiveEndpoint(
            "payments-order-placed-event",
            endpoint =>
            {
                endpoint.ConfigureConsumer<
                    OrderPlacedEventConsumer>(context);
            });
    });
});

#endregion

var app = builder.Build();

#region Database Migration

await ApplyMigrationsAsync<PaymentsDbContext>(
    app,
    "PaymentsAPI");

#endregion

#region Pipeline

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var isRunningInContainer =
    string.Equals(
        Environment.GetEnvironmentVariable(
            "DOTNET_RUNNING_IN_CONTAINER"),
        "true",
        StringComparison.OrdinalIgnoreCase);

if (!isRunningInContainer)
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        service = "PaymentsAPI",
        status = "Healthy"
    }));

#endregion

app.Run();

static async Task ApplyMigrationsAsync<TContext>(
    WebApplication app,
    string serviceName)
    where TContext : DbContext
{
    const int maxAttempts = 10;
    var retryDelay = TimeSpan.FromSeconds(5);

    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await using var scope =
                app.Services.CreateAsyncScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<TContext>();

            logger.LogInformation(
                "Aplicando migrations do {ServiceName}. Tentativa {Attempt}/{MaxAttempts}.",
                serviceName,
                attempt,
                maxAttempts);

            await dbContext.Database.MigrateAsync();

            logger.LogInformation(
                "Migrations do {ServiceName} aplicadas com sucesso.",
                serviceName);

            return;
        }
        catch (Exception exception)
        {
            if (attempt == maxAttempts)
            {
                logger.LogCritical(
                    exception,
                    "Não foi possível aplicar as migrations do {ServiceName} após {MaxAttempts} tentativas.",
                    serviceName,
                    maxAttempts);

                throw;
            }

            logger.LogWarning(
                exception,
                "Falha ao aplicar migrations do {ServiceName}. Nova tentativa em {RetrySeconds} segundos.",
                serviceName,
                retryDelay.TotalSeconds);

            await Task.Delay(retryDelay);
        }
    }
}