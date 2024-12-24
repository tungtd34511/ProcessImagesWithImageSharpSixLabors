using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace ProcessImagesWithImageSharpSixLabors.Models
{
    public class UploadFileOption
    {
        #region Resize
        public bool IsResize { get; set; }
        public IResampler Method { get; set; } = KnownResamplers.
        public int Width { get; set; }
        public int Height { get; set; }
        public bool PremultiplyAlphaChannel { get; set; } = true;
        public bool LinearRGB { get; set; } = true;
        public bool MaintainAspectRatio { get; set; }
        public FitMethodOption FitMethod { get; set; } = FitMethodOption.Stretch;
        #endregion
    }
    public enum FitMethodOption
    {
        Stretch,
        Contain
    }
}
