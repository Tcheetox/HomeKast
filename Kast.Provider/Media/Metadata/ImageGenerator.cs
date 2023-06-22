using System.Drawing.Drawing2D;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace Kast.Provider.Media
{
    internal static class ImageGenerator
    {
        public static bool TryCreateThumbnail(ILogger logger, Image original, string originalImagePath, int desiredWidth, int desiredHeight, out string? thumbnailPath)
        {
            try
            {
                int newWidth, newHeight;
                float aspectRatio = (float)original.Width / original.Height;

                if (original.Width > original.Height)
                {
                    newWidth = desiredWidth;
                    newHeight = (int)Math.Round(desiredWidth / aspectRatio);
                }
                else
                {
                    newWidth = (int)Math.Round(desiredHeight * aspectRatio);
                    newHeight = desiredHeight;
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

                // Get the directory and file name of the original image
                string originalDirectory = Path.GetDirectoryName(originalImagePath);
                string originalFileName = Path.GetFileNameWithoutExtension(originalImagePath);
                string originalExtension = Path.GetExtension(originalImagePath);

                thumbnailPath = Path.Combine(originalDirectory, $"{originalFileName}_thumbnail{originalExtension}");
                thumbnailBitmap.Save(thumbnailPath);

                return true;
            }
            catch (Exception ex)
            {
                thumbnailPath = null;
                logger.LogError(ex, "Unable to create thumbnail of {originalPath}", originalImagePath);
                return false;
            }
        }
    }
}
