using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class LocalizationAuditTests
    {
        private static string GetLocalizationFolderPath()
        {
            var baseDir = AppContext.BaseDirectory;
            // Traverse up to find NFC.Platform solution root directory
            var current = new DirectoryInfo(baseDir);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "NFC.Platform.sln")))
            {
                current = current.Parent;
            }

            if (current == null)
            {
                throw new DirectoryNotFoundException("Could not locate NFC.Platform.sln root directory.");
            }

            var locPath = Path.Combine(current.FullName, "NFC.Platform.BuildingBlocks", "Localization");
            Assert.True(Directory.Exists(locPath), $"Localization folder not found at: {locPath}");
            return locPath;
        }

        [Theory]
        [InlineData("ErrorMessages")]
        [InlineData("ValidationMessages")]
        [InlineData("BusinessMessages")]
        [InlineData("SuccessMessages")]
        [InlineData("ExportMessages")]
        public void EnglishAndArabicResxFiles_HaveMatchingKeyCounts(string baseFileName)
        {
            var folder = GetLocalizationFolderPath();
            var enPath = Path.Combine(folder, $"{baseFileName}.resx");
            var arPath = Path.Combine(folder, $"{baseFileName}.ar.resx");

            Assert.True(File.Exists(enPath), $"File missing: {enPath}");
            Assert.True(File.Exists(arPath), $"File missing: {arPath}");

            var enKeys = GetResxKeyNames(enPath);
            var arKeys = GetResxKeyNames(arPath);

            var missingInAr = enKeys.Except(arKeys).ToList();
            var missingInEn = arKeys.Except(enKeys).ToList();

            Assert.Empty(missingInAr);
            Assert.Empty(missingInEn);
        }

        private static HashSet<string> GetResxKeyNames(string filePath)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var xml = new XmlDocument();
            xml.Load(filePath);

            var nodes = xml.SelectNodes("//data");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var nameAttr = node.Attributes?["name"];
                    if (nameAttr != null && !string.IsNullOrWhiteSpace(nameAttr.Value))
                    {
                        keys.Add(nameAttr.Value);
                    }
                }
            }

            return keys;
        }
    }
}
