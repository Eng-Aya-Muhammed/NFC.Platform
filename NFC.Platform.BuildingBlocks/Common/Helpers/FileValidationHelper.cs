using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public static class FileValidationHelper
    {
        public const long DefaultMaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
        public const long DefaultMaxExcelSizeBytes = 50 * 1024 * 1024; // 50 MB
        public const long DefaultMaxPdfSizeBytes = 50 * 1024 * 1024;   // 50 MB

        private static readonly byte[] JpegMagicBytes = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] PngMagicBytes = [0x89, 0x50, 0x4E, 0x47];
        private static readonly byte[] GifMagicBytes = [0x47, 0x49, 0x46, 0x38];
        private static readonly byte[] RiffMagicBytes = [0x52, 0x49, 0x46, 0x46]; // RIFF header for WEBP
        private static readonly byte[] WebpMagicBytes = [0x57, 0x45, 0x42, 0x50]; // WEBP header at offset 8

        private static readonly byte[] XlsxMagicBytes = [0x50, 0x4B, 0x03, 0x04]; // PK zip header
        private static readonly byte[] XlsMagicBytes = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]; // Compound File (OLE2) header

        private static readonly byte[] PdfMagicBytes = [0x25, 0x50, 0x44, 0x46]; // %PDF header

        public static bool IsValidImageSignature(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            byte[] header = new byte[12];
            try
            {
                using var stream = file.OpenReadStream();
                if (stream == null) return false;
                int bytesRead = stream.Read(header, 0, header.Length);
                if (bytesRead < 4) return false;
            }
            catch
            {
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            return ext switch
            {
                ".jpg" or ".jpeg" => StartsWith(header, JpegMagicBytes),
                ".png" => StartsWith(header, PngMagicBytes),
                ".gif" => StartsWith(header, GifMagicBytes),
                ".webp" => StartsWith(header, RiffMagicBytes) && HeaderContainsAt(header, WebpMagicBytes, 8),
                _ => false
            };
        }

        public static bool IsValidExcelSignature(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            byte[] header = new byte[8];
            try
            {
                using var stream = file.OpenReadStream();
                if (stream == null) return false;
                int bytesRead = stream.Read(header, 0, header.Length);
                if (bytesRead < 4) return false;
            }
            catch
            {
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            return ext switch
            {
                ".xlsx" => StartsWith(header, XlsxMagicBytes),
                ".xls" => StartsWith(header, XlsMagicBytes),
                _ => false
            };
        }

        public static bool IsValidPdfSignature(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            byte[] header = new byte[4];
            try
            {
                using var stream = file.OpenReadStream();
                if (stream == null) return false;
                int bytesRead = stream.Read(header, 0, header.Length);
                if (bytesRead < 4) return false;
            }
            catch
            {
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            return ext == ".pdf" && StartsWith(header, PdfMagicBytes);
        }

        public static bool IsValidImageContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return true;
            var lower = contentType.Trim().ToLowerInvariant();
            return lower.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || lower == "application/octet-stream";
        }

        public static bool IsValidExcelContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return true;
            var lower = contentType.Trim().ToLowerInvariant();
            return lower.Contains("spreadsheetml") || lower.Contains("excel") || lower == "application/octet-stream" || lower == "application/x-zip-compressed" || lower == "application/zip";
        }

        public static bool IsValidPdfContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return true;
            var lower = contentType.Trim().ToLowerInvariant();
            return lower == "application/pdf" || lower == "application/x-pdf" || lower == "application/octet-stream";
        }

        private static bool StartsWith(byte[] header, byte[] prefix)
        {
            if (header.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (header[i] != prefix[i]) return false;
            }
            return true;
        }

        private static bool HeaderContainsAt(byte[] header, byte[] target, int offset)
        {
            if (header.Length < offset + target.Length) return false;
            for (int i = 0; i < target.Length; i++)
            {
                if (header[offset + i] != target[i]) return false;
            }
            return true;
        }
    }
}
