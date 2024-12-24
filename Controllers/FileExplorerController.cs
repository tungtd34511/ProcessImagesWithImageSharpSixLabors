using Microsoft.AspNetCore.Mvc;
using ProcessImagesWithImageSharpSixLabors.Models;
using ProcessImagesWithImageSharpSixLabors.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ProcessImagesWithImageSharpSixLabors.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileExplorerController : ControllerBase
    {
        private readonly IFileExplorerService _fileExplorerService;

        public FileExplorerController(IFileExplorerService fileExplorerService)
        {
            _fileExplorerService = fileExplorerService;
        }

        // GET: api/<FileExplorerController>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadFileDTO obj)
        {
            try
            {
                string path = await _fileExplorerService.SaveAsync(obj.File);
                return Ok(path);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}
