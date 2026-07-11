using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthCheckController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        [HttpGet]
        public async Task<IActionResult> CheckHealth()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (canConnect)
                {
                    return Ok(new
                    {
                        Status = "Healthy",
                        Database = "Connected",
                        Time = DateTime.UtcNow
                    });
                }

                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    Database = "Disconnected"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    Database = "ConnectionFailed",
                    Error = ex.Message
                });
            }
        }
    }
}
