using ProcessImagesWithImageSharpSixLabors.Models;
using ProcessImagesWithImageSharpSixLabors.Util.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.Text;
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
        public string _defaultUploadPathFullName => Path.Combine(_environment.WebRootPath, _defaultUploadPathName);
        private readonly string[] _permittedExtensions = { ".txt", ".pdf", ".jpg", ".jpeg",".png" };
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
        private static JpegEncodingColor[] _yCbCrEncodingColors = new JpegEncodingColor[] {
            JpegEncodingColor.YCbCrRatio420,
            JpegEncodingColor.YCbCrRatio444,
            JpegEncodingColor.YCbCrRatio422,
            JpegEncodingColor.YCbCrRatio411,
            JpegEncodingColor.YCbCrRatio410
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
                return await SaveImageAsync(file, new UploadFileOption());
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
            // Nén ảnh về JPEG trước khi chuyển đổi
            IImageEncoder encoder = new JpegEncoder();
            if (!Directory.Exists(_defaultUploadPathFullName))
            {
                Directory.CreateDirectory(_defaultUploadPathFullName);
            }
            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(file.OpenReadStream()))
            {
                if (option != null)
                {
                    //Resize
                    if (option.IsResize)
                    {
                        image.Mutate(c => c.Resize(new ResizeOptions()
                        {
                            Size = new Size(image.Width, image.Height),
                            Mode = option.MaintainAspectRatio ? ResizeMode.Manual : option.FitMethod,
                            Sampler = option.Method,
                            PremultiplyAlpha = option.PremultiplyAlpha
                        }));
                        if (option.LinearRGB)
                        {
                            image.ProgressLinearRGB();
                        }
                    }
                    #region Advanced
                    using (MemoryStream jpegStream = new MemoryStream())
                    {
                        encoder = new JpegEncoder()
                        {
                            Quality = option.Quality,
                            ColorType = option.EncodingColor
                        };
                        if(option.Smooth > 0)
                        {
                            image.Mutate(c=>c.GaussianBlur(option.Smooth));
                        }
                        // Lưu ảnh vào jpegStream dưới dạng JPEG
                        await image.SaveAsync(jpegStream, encoder);
                        // Đặt lại vị trí của stream về đầu sau khi lưu ảnh
                        jpegStream.Position = 0;
                        // Nếu CompressType không phải Original, chọn encoder cho định dạng đích
                        encoder = option.CompressType != CompressType.Original ? GetEncoder(option.CompressType) : image.DetectEncoder(newFileFullName);
                        // Đọc ảnh từ jpegStream và lưu lại với encoder đã chọn
                        using (var finalImage = await Image.LoadAsync<Rgba32>(jpegStream))  // Đọc ảnh JPEG từ stream
                        {
                            await finalImage.SaveAsync(newFileFullName, encoder);  // Lưu ảnh vào file
                        }
                    }
                    #endregion
                }
                else
                {
                    await image.SaveAsync(newFileFullName);
                }
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
        private static IImageEncoder GetEncoder(CompressType type)
        {
            switch (type)
            {
                case CompressType.Bmp:
                    return new BmpEncoder();
                case CompressType.Gif:
                    return new GifEncoder();
                case CompressType.Jpeg:
                    return new JpegEncoder();
                case CompressType.Pbm:
                    return new PbmEncoder();
                case CompressType.Png:
                    return new PngEncoder();
                case CompressType.Qoi:
                    return new QoiEncoder();
                case CompressType.Tga:
                    return new TgaEncoder();
                case CompressType.Tiff:
                    return new TiffEncoder();
                case CompressType.WebP:
                    return new WebpEncoder();
                default:
                    return new JpegEncoder();
            }
        }
        #endregion
    }
}
