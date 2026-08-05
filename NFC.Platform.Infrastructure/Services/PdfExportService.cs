using System;
using System.IO;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NFC.Platform.Infrastructure.Services
{
    public class PdfExportService : IPdfExportService
    {
        static PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GeneratePdf(ExportDataContainer dataContainer)
        {
            var isLandscape = dataContainer.Headers.Count > 6;
            var isRtl = dataContainer.IsRtl;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(isLandscape ? PageSizes.A4.Landscape() : PageSizes.A4.Portrait());
                    page.Margin(20);
                    page.PageColor(Colors.White);

                    if (isRtl)
                    {
                        page.ContentFromRightToLeft();
                    }

                    page.Header().Element(headerContainer =>
                    {
                        headerContainer.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(dataContainer.Title)
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);

                                col.Item().Text($"NFC Platform - {DateTime.Now:yyyy-MM-dd HH:mm}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Medium);
                            });
                        });
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in dataContainer.Headers)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var colHeader in dataContainer.Headers)
                            {
                                header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text(colHeader.DisplayName)
                                    .Bold()
                                    .FontColor(Colors.White)
                                    .FontSize(10);
                            }
                        });

                        int rowIndex = 0;
                        foreach (var dataRow in dataContainer.Rows)
                        {
                            var backgroundColor = rowIndex % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                            foreach (var colHeader in dataContainer.Headers)
                            {
                                dataRow.Cells.TryGetValue(colHeader.PropertyName, out var cellText);
                                table.Cell().Background(backgroundColor).Padding(6).Text(cellText ?? string.Empty)
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken3);
                            }
                            rowIndex++;
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span(isRtl ? "صفحة " : "Page ");
                        text.CurrentPageNumber();
                        text.Span(isRtl ? " من " : " of ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
