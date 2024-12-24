using System.ComponentModel.DataAnnotations;

namespace ProcessImagesWithImageSharpSixLabors.Models
{
    public class UploadFileDTO
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
