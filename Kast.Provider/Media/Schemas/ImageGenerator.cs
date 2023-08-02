using System.Drawing.Drawing2D;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace Kast.Provider.Media
{
    internal static class ImageGenerator
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "WindowsOnly")]
        public static bool TryCreateThumbnail(ILogger logger, Stream stream, string path, int width, int height)
        {
            try
            {
                using var original = Image.FromStream(stream);
                int newWidth, newHeight;
                float aspectRatio = (float)original.Width / original.Height;

                if (original.Width > original.Height)
                {
                    newWidth = width;
                    newHeight = (int)Math.Round(width / aspectRatio);
                }
                else
                {
                    newWidth = (int)Math.Round(height * aspectRatio);
                    newHeight = height;
                }

                // Create a new bitmap with the desired dimensions
                using Bitmap thumbnailBitmap = new(newWidth, newHeight);
                using Graphics graphics = Graphics.FromImage(thumbnailBitmap);

                // Set the interpolation mode to high quality
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Resize the original image to the new dimensions
                graphics.DrawImage(original, 0, 0, newWidth, newHeight);

                // Save
                thumbnailBitmap.Save(path);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to create thumbnail of {originalPath}", path);
                return false;
            }
        }
    }
}
