using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace ProcessImagesWithImageSharpSixLabors.Models
{
    public class UploadFileOption
    {
        public UploadFileOption()
        {
            if (Smooth is < 0 or > 100)
            {
                throw new ArgumentException("Smooth factor must be in [1..100] range.");
            }
        }
        #region Resize
        public bool IsResize { get; set; }
        public IResampler Method { get; set; } = KnownResamplers.Lanczos3;
        /// <summary>
        /// 0 to set auto size. if width and height set to 0, save image to original size
        /// </summary>
        public int Width { get; set; } = 0;
        /// <summary>
        /// 0 to set auto size
        /// </summary>
        public int Height { get; set; } = 0;
        public bool PremultiplyAlpha { get; set; } = true;
        public bool LinearRGB { get; set; } = true;
        public bool MaintainAspectRatio { get; set; }
        public ResizeMode FitMethod { get; set; } = ResizeMode.Manual;
        #endregion
        #region Compress
        public CompressType CompressType { get; set; } = CompressType.Jpeg;
        /// <summary>
        /// set it 1-100
        /// </summary>
        public int Quality { get; set; } = 75;
        #endregion
        #region Advanced
        public JpegEncodingColor EncodingColor { get; set; } = JpegEncodingColor.YCbCrRatio420;
        public int Smooth { get; set; } = 0;
        public int Quantization { get; set; }
        #endregion
    }
    public enum FitMethodOption
    {
        Stretch,
        Contain
    }
    public enum CompressType
    {
        /// <summary>
        /// Original Image
        /// </summary>
        Original,
        /// <summary>
        /// bitmap
        /// </summary>
        Bmp,
        /// <summary>
        /// Gif
        /// </summary>
        Gif,
        /// <summary>
        /// Jpeg
        /// </summary>
        Jpeg,
        /// <summary>
        /// Pbm
        /// </summary>
        Pbm,
        /// <summary>
        /// Png
        /// </summary>
        Png,
        /// <summary>
        /// QOI
        /// </summary>
        Qoi,
        /// <summary>
        /// Tga
        /// </summary>
        Tga,
        /// <summary>
        /// Tiff
        /// </summary>
        Tiff,
        /// <summary>
        /// WebP
        /// </summary>
        WebP
    }
}
