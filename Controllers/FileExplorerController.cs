using Microsoft.AspNetCore.Mvc;

namespace ProcessImagesWithImageSharpSixLabors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileExplorerController : ControllerBase
    {
        [HttpGet("index")]
        public IActionResult Index()
        {
            return Ok();
        }
    }
}
