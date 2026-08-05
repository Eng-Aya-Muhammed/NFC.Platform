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
                var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile(new CardDesignMappingProfile()));
                var mapper = mapperConfig.CreateMapper();
                var unitOfWork = Substitute.For<IUnitOfWork>();
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

                var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

                var httpClientFactory = Substitute.For<IHttpClientFactory>();
                var realHttpClient = new HttpClient();
                httpClientFactory.CreateClient(Arg.Any<string>()).Returns(realHttpClient);

                var realExcelParser = new ExcelParser();
                var otpSettingsOptions = Options.Create(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });

                var companyRepo = Substitute.For<IGenericRepository<Company>>();
                companyRepo.GetQueryable().Returns(new List<Company> { new Company { TenantId = tenantId, Id = Guid.NewGuid() } }.AsQueryable().BuildMock());
                unitOfWork.Repository<Company>().Returns(companyRepo);

                var employeeService = Substitute.For<IEmployeeService>();
                employeeService.UpsertEmployeesFromExcelAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>())
                    .Returns(ServiceResult<List<Guid>>.Fail("FailedToParseExcel", 422));

                var cardOrderService = new CardOrderService(
                    unitOfWork,
                    mapper,
                    messageService,
                    currentTenant,
                    validator,
                    Substitute.For<IValidator<UpdateCardOrderRequest>>(),
                    backgroundJobClient,
                    employeeService,
                    otpSettingsOptions
                );

                var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();

                var cardDesignRepo = Substitute.For<IGenericRepository<CardDesign>>();
                var designsList = new List<CardDesign>();
                cardDesignRepo.AddAsync(Arg.Do<CardDesign>(d => designsList.Add(d))).Returns(Task.CompletedTask);
                cardDesignRepo.GetQueryable().Returns(_ => designsList.AsQueryable().BuildMock());
                unitOfWork.Repository<CardDesign>().Returns(cardDesignRepo);

                var unitPackage = new CardPackage { Id = Guid.NewGuid(), NumberOfCards = 1, Price = 10, IsActive = true };
                var packageRepo = Substitute.For<IGenericRepository<CardPackage>>();
                packageRepo.GetQueryable().Returns(new List<CardPackage> { unitPackage }.AsQueryable().BuildMock());
                unitOfWork.Repository<CardPackage>().Returns(packageRepo);

                var cardTypeRepo = Substitute.For<IGenericRepository<CardType>>();
                cardTypeRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(new CardType { Id = Guid.NewGuid(), IsActive = true });
                unitOfWork.Repository<CardType>().Returns(cardTypeRepo);

                var request = new CreateCardDesignRequest
                {
                    CardTypeId = Guid.NewGuid(),
                    CustomQuantity = 10,
                    CardDesignType = CardDesignType.NeedCustomDesign,
                    ExcelDataUrl = cloudinaryExcelUrl
                };

                var cardDesignService = new CardDesignService(
                    unitOfWork,
                    mapper,
                    messageService,
                    currentTenant,
                    employeeService,
                    Substitute.For<IConfiguration>()
                );

                // 4. Act: Call CreateDesignAsync for CompanyAdmin
                var result = await cardDesignService.CreateDesignAsync(request);

                Console.WriteLine($"[CompanyAdmin Result] IsSuccess: {result.IsSuccess}, StatusCode: {result.StatusCode}, Message: {result.Message}");

                // Validation MUST fail (IsSuccess = false, 422 status code)
                Assert.False(result.IsSuccess, "Design creation should fail validation for invalid Excel data.");
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
            var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile(new CardDesignMappingProfile()));
            var mapper = mapperConfig.CreateMapper();
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
            var createdOrder = new CardOrder { Id = Guid.NewGuid(), Quantity = 1 };
            cardOrderRepo.GetQueryable().Returns(new List<CardOrder> { createdOrder }.AsQueryable().BuildMock());
            unitOfWork.Repository<CardOrder>().Returns(cardOrderRepo);

            var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(new ValidationResult()));

            var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var realExcelParser = new ExcelParser();
            var otpSettingsOptions = Options.Create(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company> { new Company { TenantId = tenantId, Id = Guid.NewGuid() } }.AsQueryable().BuildMock());
            unitOfWork.Repository<Company>().Returns(companyRepo);

            var indCardTypeRepo = Substitute.For<IGenericRepository<CardType>>();
            indCardTypeRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(new CardType { Id = Guid.NewGuid(), IsActive = true });
            unitOfWork.Repository<CardType>().Returns(indCardTypeRepo);
            var cardOrderService = new CardOrderService(
                unitOfWork,
                mapper,
                messageService,
                currentTenant,
                validator,
                Substitute.For<IValidator<UpdateCardOrderRequest>>(),
                backgroundJobClient,
                Substitute.For<IEmployeeService>(),
                otpSettingsOptions
            );

            var cardPackageId = Guid.NewGuid();
            var cardPackageRepo = Substitute.For<IGenericRepository<CardPackage>>();
            cardPackageRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(new CardPackage { Id = cardPackageId, IsActive = true, NumberOfCards = 1, Price = 10.0m });
            unitOfWork.Repository<CardPackage>().Returns(cardPackageRepo);

            var cardDesignRepo = Substitute.For<IGenericRepository<CardDesign>>();
            var designsList = new List<CardDesign>();
            cardDesignRepo.AddAsync(Arg.Do<CardDesign>(d => designsList.Add(d))).Returns(Task.CompletedTask);
            cardDesignRepo.GetQueryable().Returns(_ => designsList.AsQueryable().BuildMock());
            unitOfWork.Repository<CardDesign>().Returns(cardDesignRepo);

            var request = new CreateCardDesignRequest
            {
                CardTypeId = Guid.NewGuid(),
                CardPackageId = cardPackageId,
                CardDesignType = CardDesignType.NeedCustomDesign,
                ExcelDataUrl = "https://res.cloudinary.com/fake-url-with-invalid-data.xlsx"
            };

            var cardDesignService = new CardDesignService(
                unitOfWork,
                mapper,
                messageService,
                currentTenant,
                Substitute.For<IEmployeeService>(),
                Substitute.For<IConfiguration>()
            );

            // Act: Call CreateDesignAsync for Individual Account with ExcelDataUrl
            var result = await cardDesignService.CreateDesignAsync(request);

            Console.WriteLine($"[Individual User Result] IsSuccess: {result.IsSuccess}, StatusCode: {result.StatusCode}");

            // ExcelDataUrl is ignored for Individual accounts
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public void GenerateSampleExcelFileForUserTesting()
        {
            var headers = new List<string>
            {
                "الاسم الكامل", "البريد الإلكتروني", "رقم الهاتف", "المسمى الوظيفي", "القسم",
                "واتساب", "فيسبوك", "إنستغرام", "لينكدإن", "موقع إلكتروني", "تويتر", "رابط إضافي"
            };

            var dataRows = new List<List<string>>
            {
                new List<string> { "آسر أحمد", "aser.ahmed@nfcplatform.com", "+96590001111", "مدير التقنية CTO", "تكنولوجيا المعلومات", "+96590001111", "https://facebook.com/aser.ahmed", "https://instagram.com/aser.ahmed", "https://linkedin.com/in/aser-ahmed", "https://nfcplatform.com", "https://x.com/aser_ahmed", "https://github.com/aser-ahmed" },
                new List<string> { "سارة المحمود", "sara.almahmoud@nfcplatform.com", "+96590002222", "مدير التسويق CMO", "التسويق والإعلام", "+96590002222", "https://facebook.com/sara.almahmoud", "https://instagram.com/sara.almahmoud", "https://linkedin.com/in/sara-almahmoud", "https://nfcplatform.com", "https://x.com/sara_m", "https://behance.net/sara-m" },
                new List<string> { "محمد الكندري", "mohammed.alkandari@nfcplatform.com", "+96590003333", "مهندس برمجيات أول", "التطوير والبرمجة", "+96590003333", "https://facebook.com/m.alkandari", "https://instagram.com/m.alkandari", "https://linkedin.com/in/m-alkandari", "https://nfcplatform.com", "https://x.com/m_alkandari", "https://dev.to/m-alkandari" }
            };

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Employees");

            for (int col = 0; col < headers.Count; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
            }

            for (int rowIdx = 0; rowIdx < dataRows.Count; rowIdx++)
            {
                for (int col = 0; col < headers.Count; col++)
                {
                    worksheet.Cell(rowIdx + 2, col + 1).Value = dataRows[rowIdx][col];
                }
            }

            workbook.SaveAs(@"d:\NFC.Platform\sample_employees_import.xlsx");
            workbook.SaveAs(@"C:\Users\DELL\.gemini\antigravity-ide\brain\34144dcf-2199-4ff9-aaf5-251ed9cd0165\sample_employees_import.xlsx");
        }
    }
}
