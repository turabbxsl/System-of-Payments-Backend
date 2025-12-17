using Payments.Common.Config;
using PaymentsPublisher;
using PaymentsPublisher.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<IDbService, DbService>();
builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();

builder.Services.AddOptions<RabbitMqConnectionConfig>()
    .Bind(builder.Configuration.GetSection("RabbitMQConnection"));
builder.Services.AddOptions<PaymentsPublisher.Config.PublisherRabbitMqConfig>()
    .Bind(builder.Configuration.GetSection("Publisher"));

var host = builder.Build();
host.Run();
