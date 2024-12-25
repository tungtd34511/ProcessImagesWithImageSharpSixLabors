using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ProcessImagesWithImageSharpSixLabors.Util.Extensions
{
    public static class ImageExtension
    {
        public static void ProgressLinearRGB(this Image<Rgba32> image)
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

                        // Convert each color channel from Linear to sRGB
                        pixel.R = LinearToSrgb(pixel.R);
                        pixel.G = LinearToSrgb(pixel.G);
                        pixel.B = LinearToSrgb(pixel.B);
                    }
                }
            });
        }

        private static byte LinearToSrgb(byte linearValue)
        {
            // Normalize to 0–1 range
            float normalized = linearValue / 255f;

            // Apply sRGB conversion formula
            float srgb = normalized <= 0.0031308f
                ? normalized * 12.92f
                : 1.055f * MathF.Pow(normalized, 1f / 2.4f) - 0.055f;

            // Convert back to 0–255 range and clamp
            return (byte)Math.Clamp(srgb * 255f, 0, 255);
        }
    }
}
