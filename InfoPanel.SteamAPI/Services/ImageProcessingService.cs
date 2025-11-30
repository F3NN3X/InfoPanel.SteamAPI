using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services
{
    public class ImageProcessingService
    {
        private readonly EnhancedLoggingService? _enhancedLogger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public ImageProcessingService(EnhancedLoggingService? enhancedLogger = null)
        {
            _enhancedLogger = enhancedLogger;
        }

        public async Task<byte[]?> DownloadImageAsync(string imageUrl)
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(imageUrl);
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("ImageProcessingService", $"Failed to download image from {imageUrl}", ex);
                return null;
            }
        }

        public bool ResizeAndSaveImage(byte[] imageData, string outputPath, int maxWidth, int maxHeight)
        {
            try
            {
                using (var ms = new MemoryStream(imageData))
                using (var originalImage = Image.FromStream(ms))
                {
                    // Calculate the scaling factor to preserve aspect ratio
                    // Use double for better precision as suggested
                    double ratioX = (double)maxWidth / originalImage.Width;
                    double ratioY = (double)maxHeight / originalImage.Height;
                    double ratio = Math.Min(ratioX, ratioY);

                    // Calculate new dimensions
                    int newWidth = (int)Math.Round(originalImage.Width * ratio);
                    int newHeight = (int)Math.Round(originalImage.Height * ratio);

                    // Ensure at least 1x1
                    newWidth = Math.Max(1, newWidth);
                    newHeight = Math.Max(1, newHeight);

                    // Create new bitmap with the FULL boundary size (maxWidth x maxHeight)
                    // This ensures the image is not stretched by the UI if the UI expects this specific size
                    // We center the image within this transparent canvas
                    using (var newImage = new Bitmap(maxWidth, maxHeight))
                    using (var graphics = Graphics.FromImage(newImage))
                    {
                        // Clear with transparency
                        graphics.Clear(Color.Transparent);

                        // High-quality settings to avoid artifacts
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        // Calculate centered position
                        int x = (maxWidth - newWidth) / 2;
                        int y = (maxHeight - newHeight) / 2;

                        // Draw scaled image centered
                        graphics.DrawImage(originalImage, x, y, newWidth, newHeight);

                        // Save directly to file to avoid intermediate byte array allocation
                        newImage.Save(outputPath, ImageFormat.Png);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("ImageProcessingService", "Failed to resize and save image", ex);
                return false;
            }
        }

        public byte[] ResizeImageToFit(byte[] imageData, int maxWidth, int maxHeight)
        {
            try
            {
                using (var ms = new MemoryStream(imageData))
                using (var originalImage = Image.FromStream(ms))
                {
                    // Calculate the scaling factor to preserve aspect ratio
                    // Use double for better precision as suggested
                    double ratioX = (double)maxWidth / originalImage.Width;
                    double ratioY = (double)maxHeight / originalImage.Height;
                    double ratio = Math.Min(ratioX, ratioY);

                    // Calculate new dimensions
                    int newWidth = (int)Math.Round(originalImage.Width * ratio);
                    int newHeight = (int)Math.Round(originalImage.Height * ratio);

                    // Ensure at least 1x1
                    newWidth = Math.Max(1, newWidth);
                    newHeight = Math.Max(1, newHeight);

                    // Create new bitmap with the FULL boundary size (maxWidth x maxHeight)
                    // This ensures the image is not stretched by the UI if the UI expects this specific size
                    // We center the image within this transparent canvas
                    using (var newImage = new Bitmap(maxWidth, maxHeight))
                    using (var graphics = Graphics.FromImage(newImage))
                    {
                        // Clear with transparency
                        graphics.Clear(Color.Transparent);

                        // High-quality settings to avoid artifacts
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        // Calculate centered position
                        int x = (maxWidth - newWidth) / 2;
                        int y = (maxHeight - newHeight) / 2;

                        // Draw scaled image centered
                        graphics.DrawImage(originalImage, x, y, newWidth, newHeight);

                        // Save to memory stream as PNG
                        using (var outputStream = new MemoryStream())
                        {
                            newImage.Save(outputStream, ImageFormat.Png);
                            return outputStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("ImageProcessingService", "Failed to resize image", ex);
                return imageData; // Return original on failure
            }
        }

        public async Task<string?> ProcessAndSaveImageAsync(string imageUrl, string cachePath, int maxWidth, int maxHeight)
        {
            try
            {
                // Check if cached file exists and is valid (optional: check age)
                if (File.Exists(cachePath))
                {
                    return cachePath;
                }

                var imageData = await DownloadImageAsync(imageUrl);
                if (imageData == null) return null;

                // Ensure directory exists
                var directory = Path.GetDirectoryName(cachePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Use the optimized method to save directly to file
                if (ResizeAndSaveImage(imageData, cachePath, maxWidth, maxHeight))
                {
                    return cachePath;
                }

                // Fallback if resizing failed (e.g. GDI error), just save original
                await File.WriteAllBytesAsync(cachePath, imageData);
                return cachePath;
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("ImageProcessingService", $"Failed to process and save image to {cachePath}", ex);
                return null;
            }
        }
    }
}
