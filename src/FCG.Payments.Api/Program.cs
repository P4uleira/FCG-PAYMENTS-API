using FCG.Payments.Api.Middlewares;
using FCG.Payments.Application.Commands.ProcessPayment;
using FCG.Payments.Application.Consumers;
using FCG.Payments.Infrastructure;
using MassTransit;
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

        cfg.Host(rabbitMqConfig["Host"], "/", h =>
        {
            h.Username(rabbitMqConfig["Username"]!);
            h.Password(rabbitMqConfig["Password"]!);
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

#region Middleware

app.UseMiddleware<ExceptionHandlingMiddleware>();

#endregion

#region Swagger

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#endregion

app.UseHttpsRedirection();

app.MapControllers();

app.Run();