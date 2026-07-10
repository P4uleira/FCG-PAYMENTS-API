using FCG.Payments.Application.DTOs;
using FCG.Payments.Application.Queries.GetPayments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Payments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<PaymentDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var payments = await _sender.Send(
            new GetPaymentsQuery(),
            cancellationToken);

        return Ok(payments);
    }
}