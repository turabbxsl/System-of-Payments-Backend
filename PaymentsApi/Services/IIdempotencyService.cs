using Microsoft.EntityFrameworkCore;
using PaymentsApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace PaymentsApi.Services
{
    public interface IIdempotencyService
    {
        Task<bool> CheckAndProcessRequestAsync(HttpContext context);
        Task SaveResponseAsync(HttpContext context, string responseBody);
        string ComputeHash(string input);
    }

    public class IdempotencyService : IIdempotencyService
    {
        private readonly AppDbContext _db;
        private const string IdempotencyHeaderKey = "Idempotency-Key";
        private const string UserIdHeaderKey = "X-User-Id";
        public IdempotencyService(AppDbContext db)
        {
            _db = db;
        }
        public async Task<bool> CheckAndProcessRequestAsync(HttpContext context)
        {
            var idempotencyKey = GetIdempotencyKey(context);
            var userId = GetUserId(context);

            if (string.IsNullOrEmpty(idempotencyKey))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync($"{IdempotencyHeaderKey} header is required.");
                return false;
            }
            else if (userId == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Valid user ID is required (X-User-Id header is missing or invalid).");
                return false;
            }


            var requestBody = await ReadBodyAsync(context);
            var hash = ComputeHash(requestBody + context.Request.Path);

            var existing = await _db.Idempotencykeys
            .FirstOrDefaultAsync(x => x.Key == idempotencyKey && x.Userid == userId);

            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(existing.Resultdata))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync(existing.Resultdata);
                    return false;
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    await context.Response.WriteAsync("This request is already processing.");
                    return false;
                }
            }

            var idemp = new Idempotencykey
            {
                Id = Guid.NewGuid(),
                Key = idempotencyKey,
                Userid = userId,
                Requesthash = hash,
                Createdat = DateTime.UtcNow
            };
            _db.Idempotencykeys.Add(idemp);
            await _db.SaveChangesAsync();

            return true;
        }
        public string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private Guid GetUserId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(UserIdHeaderKey, out var userIdStr)
                && Guid.TryParse(userIdStr, out var userId))
                return userId;

            return Guid.Empty;
        }

        private string GetIdempotencyKey(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(IdempotencyHeaderKey, out var key))
                return key.ToString();

            return string.Empty;
        }

        private async Task<string> ReadBodyAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }

        public async Task SaveResponseAsync(HttpContext context, string responseBody)
        {
            var key = context.Request.Headers[IdempotencyHeaderKey].ToString();
            var userId = GetUserId(context);

            var record = await _db.Idempotencykeys
                .FirstOrDefaultAsync(x => x.Key == key && x.Userid == userId);

            if (record != null)
            {
                record.Resultdata = responseBody;
                record.Updatedat = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

        }
    }
}
