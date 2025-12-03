using Microsoft.EntityFrameworkCore;
using PaymentsApi.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PaymentsApi.Services
{
    public interface IPaymentService
    {
        Task<(bool IsDuplicate, string ResponseJson)> CreatePaymentAsync(PaymentRequest request);

    }

    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool IsDuplicate, string ResponseJson)> CreatePaymentAsync(PaymentRequest request)
        {
            var (idempotencyKey, userId) = GetHeaders();

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    Userid = userId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Providerid = request.ProviderId,
                    Status = TransactionStatus.Pending,
                    Createdat = DateTime.UtcNow
                };

                _db.Payments.Add(payment);

                var evt = new
                {
                    messageId = Guid.NewGuid(),
                    transactionId = payment.Id,
                    type = "PaymentCreated",
                    timestamp = DateTime.UtcNow,
                    payload = new
                    {
                        amount = payment.Amount,
                        currency = payment.Currency,
                        userId = payment.Userid,
                        providerId = payment.Providerid,
                    }
                };

                var outbox = new Outboxmessage
                {
                    Id = Guid.NewGuid(),
                    Transactionid = payment.Id,
                    Payload = JsonSerializer.Serialize(evt),
                    Type = "PaymentCreated",
                    Status = TransactionStatus.Pending,
                    Createdat = DateTime.UtcNow
                };

                _db.Outboxmessages.Add(outbox);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var response = JsonSerializer.Serialize(new { transactionId = payment.Id, status = payment.Status });

                if (!string.IsNullOrEmpty(idempotencyKey))
                {

                    var existing = await _db.Idempotencykeys
                        .FirstOrDefaultAsync(x => x.Key == idempotencyKey && x.Userid == userId);

                    if (existing != null)
                    {
                        existing.Resultdata = response;
                        _db.Idempotencykeys.Update(existing);
                        await _db.SaveChangesAsync();
                    }
                }


                return (false, response);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        private (string idempotencyKey, Guid userId) GetHeaders()
        {
            var context = _httpContextAccessor.HttpContext;

            var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            var userIdStr = context.Request.Headers["X-User-Id"].FirstOrDefault();
            Guid.TryParse(userIdStr, out var userId);

            return (key, userId);
        }
    }
}
