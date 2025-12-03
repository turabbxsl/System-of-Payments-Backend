using PaymentsApi.Models;
using PaymentsApi.Services;

namespace PaymentsApi.Middleware
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        public IdempotencyMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context, AppDbContext _db, IIdempotencyService idempotencyService)
        {
            if (context.Request.Method is not ("POST" or "PUT" or "PATCH"))
            {
                await _next(context);
                return;
            }


            bool shouldContinue = await idempotencyService.CheckAndProcessRequestAsync(context);

            if (shouldContinue)
            {
                var originalBody = context.Response.Body;
                using var memStream = new MemoryStream();
                context.Response.Body = memStream;

                await _next(context);

                if (context.Response.StatusCode is >= 200 and <= 299)
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    var responseBody = await new StreamReader(memStream).ReadToEndAsync();
                    await idempotencyService.SaveResponseAsync(context, responseBody);
                }

                memStream.Seek(0, SeekOrigin.Begin);
                await memStream.CopyToAsync(originalBody);
                context.Response.Body = originalBody;
            }


        }

     

    }
}
