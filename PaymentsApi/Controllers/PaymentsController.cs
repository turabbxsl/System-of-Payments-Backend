using Microsoft.AspNetCore.Mvc;
using PaymentsApi.Services;

namespace PaymentsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService payment;

        public PaymentsController(IPaymentService payment)
        {
            this.payment = payment;
        }

        [HttpPost("/payments")]
        public async Task<IActionResult> CreatePayment([FromBody] Models.PaymentRequest request)
        {
            var (isDuplicate, responseJson) = await payment.CreatePaymentAsync(request);
            if (isDuplicate)
            {
                return Conflict(new { message = "Duplicate payment request", data = responseJson });
            }
            return Ok(new { message = "Payment created successfully", data = responseJson });
        }
    }
}
