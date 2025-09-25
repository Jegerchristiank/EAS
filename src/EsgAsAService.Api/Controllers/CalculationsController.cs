using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("calculations")]
public class CalculationsController : ControllerBase
{
    private readonly ICalculationRunner _calc;
    public CalculationsController(ICalculationRunner calc) => _calc = calc;

    [HttpPost("run")]
    [Authorize(Policy = "CanCalculate")]
    public async Task<IActionResult> Run([FromQuery] Guid periodId)
    {
        var count = await _calc.RunAsync(periodId);
        return Ok(new { results = count });
    }
}

