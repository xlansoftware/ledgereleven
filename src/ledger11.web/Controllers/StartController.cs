using Microsoft.AspNetCore.Mvc;
using ledger11.data;
using Microsoft.AspNetCore.Authorization;

namespace ledger11.web.Controllers;

public class StartController : Controller
{
    private readonly ILogger<StartController> _logger;

    public StartController(ILogger<StartController> logger, AppDbContext appDbContext)
    {
        _logger = logger;
    }

    [Authorize]
    public IActionResult Index()
    {
        return LocalRedirect("/app");
    }

}
