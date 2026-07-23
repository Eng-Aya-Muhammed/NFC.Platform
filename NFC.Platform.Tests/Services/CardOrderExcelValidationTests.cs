using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.DTOs.Settings;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Services;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CardOrderExcelValidationTests
    {
        private static byte[] CreateExcelBytes(List<(string Name, string Email, string Phone, string JobTitle, string Department)> rows)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var contentTypeEntry = archive.CreateEntry("[Content_Types].xml");
                using (var writer = new StreamWriter(contentTypeEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>
</Types>");
                }

                var relsEntry = archive.CreateEntry("_rels/.rels");
                using (var writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");
                }

                var wbRelsEntry = archive.CreateEntry("xl/_rels/workbook.xml.rels");
                using (var writer = new StreamWriter(wbRelsEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
</Relationships>");
                }

                var wbEntry = archive.CreateEntry("xl/workbook.xml");
                using (var writer = new StreamWriter(wbEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Sheet1"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");
                }

                var sharedStrings = new List<string> { "Name", "Email", "Phone", "JobTitle", "Department" };
                foreach (var r in rows)
                {
                    sharedStrings.Add(r.Name ?? "");
                    sharedStrings.Add(r.Email ?? "");
                    sharedStrings.Add(r.Phone ?? "");
                    sharedStrings.Add(r.JobTitle ?? "");
                    sharedStrings.Add(r.Department ?? "");
                }

                var ssEntry = archive.CreateEntry("xl/sharedStrings.xml");
                using (var writer = new StreamWriter(ssEntry.Open(), Encoding.UTF8))
                {
                    var sb = new StringBuilder();
                    sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" count=""").Append(sharedStrings.Count).Append(@""" uniqueCount=""").Append(sharedStrings.Count).Append(@""">");
                    foreach (var s in sharedStrings)
                    {
                        sb.Append("<si><t>").Append(System.Security.SecurityElement.Escape(s)).Append("</t></si>");
                    }
                    sb.Append("</sst>");
                    writer.Write(sb.ToString());
                }

                var sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml");
                using (var writer = new StreamWriter(sheetEntry.Open(), Encoding.UTF8))
                {
                    var sb = new StringBuilder();
                    sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""><sheetData>");

                    sb.Append(@"<row r=""1"">");
                    for (int col = 0; col < 5; col++)
                    {
                        sb.Append(@"<c r=""").Append((char)('A' + col)).Append(@"1"" t=""s""><v>").Append(col).Append(@"</v></c>");
                    }
                    sb.Append("</row>");

                    int strIndex = 5;
                    for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
                    {
                        int rNum = rowIdx + 2;
                        sb.Append(@"<row r=""").Append(rNum).Append(@""">");
                        for (int col = 0; col < 5; col++)
                        {
                            sb.Append(@"<c r=""").Append((char)('A' + col)).Append(rNum).Append(@" t=""s""><v>").Append(strIndex++).Append(@"</v></c>");
                        }
                        sb.Append("</row>");
                    }

                    sb.Append("</sheetData></worksheet>");
                    writer.Write(sb.ToString());
                }
            }
            return ms.ToArray();
        }

        private class FormFileMock : IFormFile
        {
            private readonly byte[] _data;
            public FormFileMock(byte[] data, string fileName)
            {
                _data = data;
                FileName = fileName;
                Length = data.Length;
            }
            public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public string ContentDisposition => $"inline; filename={FileName}";
            public IHeaderDictionary Headers => new HeaderDictionary();
            public long Length { get; }
            public string Name => "file";
            public string FileName { get; }
            public void CopyTo(Stream target) => target.Write(_data, 0, _data.Length);
            public Task CopyToAsync(Stream target, System.Threading.CancellationToken cancellationToken = default) => target.WriteAsync(_data, 0, _data.Length, cancellationToken);
            public Stream OpenReadStream() => new MemoryStream(_data);
        }

        [Fact]
        public async Task Test_CloudinaryUpload_And_CardOrderValidation_WithInvalidExcelData()
        {
            // 1. Create Invalid Excel File Bytes
            // Row 1: Header (Name, Email, Phone, JobTitle, Department)
            // Row 2: Invalid Email ("invalid-email-format")
            // Row 3: Duplicate Email ("dup@test.com")
            // Row 4: Duplicate Email ("dup@test.com")
            var invalidRows = new List<(string Name, string Email, string Phone, string JobTitle, string Department)>
            {
                ("User One", "invalid-email-format", "123456", "Dev", "IT"),
                ("User Two", "dup@test.com", "123456", "Dev", "IT"),
                ("User Three", "dup@test.com", "123456", "Dev", "IT")
            };

            var excelBytes = CreateExcelBytes(invalidRows);
            Assert.NotEmpty(excelBytes);

            // 2. Upload Excel to Cloudinary using real credentials from appsettings
            var cloudinaryOptions = Options.Create(new CloudinarySettings
            {
                CloudName = "zn8nwlr1",
                ApiKey = "155122221446327",
                ApiSecret = "X0_dwB9RDZWCojHm3rc_uwhVcUg"
            });
            var storageService = new CloudinaryService(cloudinaryOptions);
            var formFile = new FormFileMock(excelBytes, "invalid_employees.xlsx");

            var uploadResult = await storageService.UploadRawFileAsync(formFile, "test-excel-orders");
            Assert.NotNull(uploadResult);
            Assert.NotEmpty(uploadResult.SecureUrl);

            string cloudinaryExcelUrl = uploadResult.SecureUrl;
            Console.WriteLine($"Uploaded Invalid Excel to Cloudinary URL: {cloudinaryExcelUrl}");

            try
            {
                // 3. Setup Mocks for CardOrderService (CompanyAdmin User)
                var unitOfWork = Substitute.For<IUnitOfWork>();
                var mapper = Substitute.For<AutoMapper.IMapper>();
                var messageService = Substitute.For<IMessageService>();
                messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(c => (string)c.Args()[0]);
                messageService.Get(Arg.Any<string>()).Returns(c => (string)c.Args()[0]);

                var currentTenant = Substitute.For<ICurrentTenant>();
                var userId = Guid.NewGuid();
                var tenantId = Guid.NewGuid();
                currentTenant.UserId.Returns(userId);
                currentTenant.TenantId.Returns(tenantId);

                var companyAdminUser = new User
                {
                    Id = userId,
                    AccountType = AccountType.CompanyAdmin
                };
                var userRepo = Substitute.For<IGenericRepository<User>>();
                userRepo.GetQueryable().Returns(new List<User> { companyAdminUser }.AsQueryable().BuildMock());
                unitOfWork.Repository<User>().Returns(userRepo);

                var cardOrderRepo = Substitute.For<IGenericRepository<CardOrder>>();
                unitOfWork.Repository<CardOrder>().Returns(cardOrderRepo);

                var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
                validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                    .Returns(Task.FromResult(new ValidationResult()));

                var cardPricingService = Substitute.For<ICardPricingService>();
                cardPricingService.CalculateOrderPricingAsync(Arg.Any<CardType>(), Arg.Any<int>())
                    .Returns(NFC.Platform.BuildingBlocks.Results.ServiceResult<OrderPricingResponseDto>.Success(new OrderPricingResponseDto { UnitPrice = 10, TotalPrice = 10 }));

                var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

                var httpClientFactory = Substitute.For<IHttpClientFactory>();
                var realHttpClient = new HttpClient();
                httpClientFactory.CreateClient(Arg.Any<string>()).Returns(realHttpClient);

                var realExcelParser = new ExcelParser();
                var otpSettingsOptions = Options.Create(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });

                var cardOrderService = new CardOrderService(
                    unitOfWork,
                    mapper,
                    messageService,
                    currentTenant,
                    cardPricingService,
                    validator,
                    backgroundJobClient,
                    httpClientFactory,
                    realExcelParser,
                    otpSettingsOptions
                );

                var request = new CreateCardOrderRequest
                {
                    Quantity = 1,
                    CardType = CardType.Plastic,
                    CardDesignType = CardDesignType.NeedCustomDesign,
                    ExcelDataUrl = cloudinaryExcelUrl
                };

                // 4. Act: Call CreateOrderAsync for CompanyAdmin
                var result = await cardOrderService.CreateOrderAsync(request);

                Console.WriteLine($"[CompanyAdmin Result] IsSuccess: {result.IsSuccess}, StatusCode: {result.StatusCode}, Errors: {string.Join(" | ", result.Errors ?? new List<string>())}");

                // Validation MUST fail (IsSuccess = false, 422 status code)
                Assert.False(result.IsSuccess, "Order creation should fail validation for invalid Excel data.");
                Assert.Equal(422, result.StatusCode);
            }
            finally
            {
                // Clean up Cloudinary asset
                await storageService.DeleteFileAsync(cloudinaryExcelUrl);
            }
        }

        [Fact]
        public async Task Test_IndividualAccount_BypassesExcelValidation_EvenWithInvalidExcelUrl()
        {
            // Setup Mocks for Individual User
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var mapper = Substitute.For<AutoMapper.IMapper>();
            var messageService = Substitute.For<IMessageService>();
            messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(c => (string)c.Args()[0]);
            messageService.Get(Arg.Any<string>()).Returns(c => (string)c.Args()[0]);

            var currentTenant = Substitute.For<ICurrentTenant>();
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            currentTenant.UserId.Returns(userId);
            currentTenant.TenantId.Returns(tenantId);

            // ACCOUNT TYPE IS INDIVIDUAL
            var individualUser = new User
            {
                Id = userId,
                AccountType = AccountType.Individual
            };
            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { individualUser }.AsQueryable().BuildMock());
            unitOfWork.Repository<User>().Returns(userRepo);

            var cardOrderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            var createdOrder = new CardOrder { Id = Guid.NewGuid(), Quantity = 1, CardType = CardType.Plastic };
            cardOrderRepo.GetQueryable().Returns(new List<CardOrder> { createdOrder }.AsQueryable().BuildMock());
            unitOfWork.Repository<CardOrder>().Returns(cardOrderRepo);

            var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(new ValidationResult()));

            var cardPricingService = Substitute.For<ICardPricingService>();
            cardPricingService.CalculateOrderPricingAsync(Arg.Any<CardType>(), Arg.Any<int>())
                .Returns(NFC.Platform.BuildingBlocks.Results.ServiceResult<OrderPricingResponseDto>.Success(new OrderPricingResponseDto { UnitPrice = 10, TotalPrice = 10 }));

            var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var realExcelParser = new ExcelParser();
            var otpSettingsOptions = Options.Create(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });

            var cardOrderService = new CardOrderService(
                unitOfWork,
                mapper,
                messageService,
                currentTenant,
                cardPricingService,
                validator,
                backgroundJobClient,
                httpClientFactory,
                realExcelParser,
                otpSettingsOptions
            );

            var request = new CreateCardOrderRequest
            {
                Quantity = 1,
                CardType = CardType.Plastic,
                CardDesignType = CardDesignType.NeedCustomDesign,
                ExcelDataUrl = "https://res.cloudinary.com/fake-url-with-invalid-data.xlsx"
            };

            // Act: Call CreateOrderAsync for Individual Account with invalid ExcelDataUrl
            var result = await cardOrderService.CreateOrderAsync(request);

            Console.WriteLine($"[Individual User Result] IsSuccess: {result.IsSuccess}, StatusCode: {result.StatusCode}");

            // ExcelDataUrl is now validated for all accounts if provided!
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }
    }
}
