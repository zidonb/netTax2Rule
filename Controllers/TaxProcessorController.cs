using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class TaxProcessorController : ControllerBase
{
    private readonly TaxProcessorService _taxProcessor;

    public TaxProcessorController(TaxProcessorService taxProcessor)
    {
        _taxProcessor = taxProcessor;
    }

    [HttpPost("process-all")]
    public async Task<IActionResult> ProcessAllClauses()
    {
        try
        {
            await _taxProcessor.ProcessAllClauses();
            return Ok("All clauses processed successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "Service is running",
            timestamp = DateTime.UtcNow
        });
    }
}