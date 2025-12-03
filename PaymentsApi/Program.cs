using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaymentsApi.Middleware;
using PaymentsApi.Models;
using PaymentsApi.Services;
using PaymentsApi.Temporary;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
NpgsqlConnection.GlobalTypeMapper.MapEnum<TransactionStatus>("transactionstatus");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<IdempotencyHeaderOperationFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<IdempotencyMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
