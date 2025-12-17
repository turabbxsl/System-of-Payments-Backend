using Consumer;
using Consumer.Services;
using Payments.Common.Config;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IMessageConsumer, MessageConsumer>();

builder.Services.AddOptions<RabbitMqConnectionConfig>()
    .Bind(builder.Configuration.GetSection("RabbitMQConnection"));
builder.Services.AddOptions<Consumer.Config.ConsumerRabbitMqConfig>()
    .Bind(builder.Configuration.GetSection("Publisher"));

var host = builder.Build();
host.Run();
