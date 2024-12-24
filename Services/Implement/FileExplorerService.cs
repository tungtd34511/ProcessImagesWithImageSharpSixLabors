using ProcessImagesWithImageSharpSixLabors.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;

namespace ProcessImagesWithImageSharpSixLabors.Services.Implement
{
    public class FileExplorerService : IFileExplorerService
    {
        public enum FileType
        {
            Image
        }
        /// <summary>
        /// Local upload directory path name
        /// </summary>
        public const string _localPathName = "u";
        /// <summary>
        /// Default upload location path name
        /// </summary>
        public const string _defaultUploadPathName = "u\\d";

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        public string _defaultUploadPathFullName => Path.Combine( _environment.WebRootPath, _defaultUploadPathName );
        private readonly string[] _permittedExtensions = { ".txt", ".pdf",".jpg",".jpeg" };
        private static List<byte[]> _imageSignature = new List<byte[]>
        {
            new byte[] { 0xFF, 0xD8, 0xFF },                // JPEG
            new byte[] { 0x89, 0x50, 0x4E, 0x47 },          // PNG
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, // GIF89a
            new byte[] { 0x42, 0x4D },                     // BMP
            new byte[] { 0x49, 0x49, 0x2A, 0x00 },         // TIFF (Little Endian)
            new byte[] { 0x4D, 0x4D, 0x00, 0x2A },         // TIFF (Big Endian)
            new byte[] { 0x52, 0x49, 0x46, 0x46 },         // WEBP (RIFF-based)
        };
        private static readonly Dictionary<FileType, List<byte[]>> _fileSignature = new()
        {
            {
                FileType.Image, _imageSignature
            },
        };
        public FileExplorerService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        /// <summary>
        /// Save file to server
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<string> SaveAsync(IFormFile file)
        {
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
            {
                throw new Exception("The extension is invalid ... discontinue processing the file");
            }
            if (IsImageBySignature(file))
            {
                return await SaveImageAsync(file);
            }
            string newFileName = GetRandomFileName(ext);
            string newFileFullName = Path.Combine(_defaultUploadPathFullName, newFileName);
            using (var stream = new FileStream(newFileFullName, FileMode.Create))
            {
                if (!Directory.Exists(_defaultUploadPathFullName))
                {
                    Directory.CreateDirectory(_defaultUploadPathFullName);
                }
                await file.CopyToAsync(stream);
            }
            string webPath = GetWebPath(newFileFullName);
            return webPath;
        }

        public async Task<string> SaveImageAsync(IFormFile file, UploadFileOption? option = null)
        {
            string newFileName = GetRandomFileName(Path.GetExtension(file.FileName));
            string newFileFullName = Path.Combine(_defaultUploadPathFullName, newFileName);
            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(file.OpenReadStream()))
            {
                if (option != null)
                {
                    //Resize
                    if (option.IsResize)
                    {
                        //If you pass 0 as any of the values for width and height dimensions then ImageSharp will automatically determine the correct opposite dimensions size to preserve the original aspect ratio.
                        image.Mutate(c => c.Resize(option.Width, option.Height, option.Method));
                        if (option.PremultiplyAlphaChannel || option.LinearRGB)
                        {
                            // Iterate through each pixel
                            image.ProcessPixelRows(accessor =>
                            {
                                for (int y = 0; y < accessor.Height; y++)
                                {
                                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                                    for (int x = 0; x < pixelRow.Length; x++)
                                    {

                                        ref Rgba32 pixel = ref pixelRow[x];
                                        if (option.PremultiplyAlphaChannel)
                                        {

                                            // Normalize alpha to range [0, 1]
                                            float alpha = pixel.A / 255f;

                                            // Premultiply color channels
                                            pixel.R = (byte)(pixel.R * alpha);
                                            pixel.G = (byte)(pixel.G * alpha);
                                            pixel.B = (byte)(pixel.B * alpha);
                                        }

                                        if (option.LinearRGB)
                                        {
                                            // Convert sRGB to Linear RGB
                                            float linearR = sRgbToLinear(pixel.R);
                                            float linearG = sRgbToLinear(pixel.G);
                                            float linearB = sRgbToLinear(pixel.B);

                                            // Perform Linear RGB processing (e.g., brighten by 10%)
                                            linearR = Math.Clamp(linearR * 1.1f, 0.0f, 1.0f);
                                            linearG = Math.Clamp(linearG * 1.1f, 0.0f, 1.0f);
                                            linearB = Math.Clamp(linearB * 1.1f, 0.0f, 1.0f);

                                            // Convert Linear RGB back to sRGB
                                            pixel.R = linearToSRgb(linearR);
                                            pixel.G = linearToSRgb(linearG);
                                            pixel.B = linearToSRgb(linearB);
                                        }
                                    }
                                }
                            });
                        }
                    }
                }
                await image.SaveAsync(newFileFullName);
            }
            using (var stream = new FileStream(newFileFullName, FileMode.Create))
            {
                if (!Directory.Exists(_defaultUploadPathFullName))
                {
                    Directory.CreateDirectory(_defaultUploadPathFullName);
                }
                await file.CopyToAsync(stream);
            }
            string webPath = GetWebPath(newFileFullName);
            return webPath;
        }

        #region Extend
        /// <summary>
        /// Get unique file name
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetUniqueFileName(string directoryPath, string fileName)
        {
            // Danh sách ký tự không hợp lệ trong tên tệp
            string invalidCharsPattern = $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]";

            // Thay thế ký tự không hợp lệ bằng dấu '-'
            string validFileName = Regex.Replace(fileName, invalidCharsPattern, "-");

            // Tách tên tệp và phần mở rộng
            string fileExtension = Path.GetExtension(validFileName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(validFileName);

            string newFileName = validFileName;
            int counter = 0;

            // Kiểm tra nếu tệp đã tồn tại
            while (File.Exists(Path.Combine(directoryPath, newFileName)))
            {
                counter++;
                newFileName = $"{fileNameWithoutExtension} ({counter}){fileExtension}";
            }

            return newFileName;
        }
        /// <summary>
        /// Get full path name
        /// </summary>
        /// <param name="paths">Folder path name or file path name</param>
        /// <returns></returns>
        public string GetFullPath(params string[] paths)
        {
            return Path.Combine(_environment.WebRootPath, Path.Combine(paths));
        }
        /// <summary>
        /// Get web path name
        /// </summary>
        /// <param name="paths">Folder path name or file path name</param>
        /// <returns></returns>
        public string GetWebPath(string pathFullName)
        {
            return _configuration.GetValue<string>("FileExplorer:Host") + pathFullName.Replace(_environment.WebRootPath, "").Replace("\\", "/");
        }
        private bool IsImageBySignature(IFormFile file)
        {
            try
            {
                using (var reader = new BinaryReader(file.OpenReadStream()))
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var signatures = _fileSignature[FileType.Image];
                    var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));
                    return signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature));
                }
            }
            catch
            {
                return false; // Nếu có lỗi trong khi đọc file (có thể là file không hợp lệ)
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext">extension</param>
        /// <returns></returns>
        private string GetRandomFileName(string ext)
        {
            string fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            return $"{DateTimeOffset.Now.ToUnixTimeSeconds()}_{fileName}{ext}";
        }
        private float sRgbToLinear(byte sRgb)
        {
            float value = sRgb / 255f;
            return (value <= 0.04045f) ? (value / 12.92f) : (float)Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        private byte linearToSRgb(float linear)
        {
            return (linear <= 0.0031308f)
                ? (byte)(linear * 12.92f * 255f)
                : (byte)((1.055f * Math.Pow(linear, 1.0 / 2.4) - 0.055f) * 255f);
        }
        #endregion
    }
}
