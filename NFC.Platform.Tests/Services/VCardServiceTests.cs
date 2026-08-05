using System;
using System.Collections.Generic;
using System.Text;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Infrastructure.Services;
using Xunit;

namespace NFC.Platform.Tests.Services;

public class VCardServiceTests
{
    private readonly VCardService _sut = new();

    [Fact]
    public void BuildVCardString_ReturnsValidVCard3Format_WhenDtoIsValid()
    {
        var dto = new EmployeeDetailsDto
        {
            FullName = "Ahmed Ali",
            JobTitle = "Senior Software Engineer",
            CompanyName = "NFC Platform",
            Department = "Engineering",
            ContactEmail = "ahmed@example.com",
            Phone = "+201000000000",
            WhatsApp = "+201000000000",
            Address = "123 Tech Street, Cairo, Egypt",
            Bio = "Digital Transformation Expert",
            ProfileUrl = "https://nfc-platform.com/u/ahmed-ali",
            ProfilePictureUrl = "https://cdn.example.com/avatar.jpg",
            Links = new List<ProfileLinkDto>
            {
                new() { Title = "LinkedIn", Url = "https://linkedin.com/in/ahmed" }
            }
        };

        var result = _sut.BuildVCardString(dto);

        Assert.NotNull(result);
        Assert.Contains("BEGIN:VCARD", result);
        Assert.Contains("VERSION:3.0", result);
        Assert.Contains("FN:Ahmed Ali", result);
        Assert.Contains("N:Ali;Ahmed;;;", result);
        Assert.Contains("TITLE:Senior Software Engineer", result);
        Assert.Contains("ORG:NFC Platform;Engineering", result);
        Assert.Contains("EMAIL;TYPE=INTERNET,WORK:ahmed@example.com", result);
        Assert.Contains("TEL;TYPE=CELL,VOICE:+201000000000", result);
        Assert.Contains("TEL;TYPE=CELL,WA:+201000000000", result);
        Assert.Contains("ADR;TYPE=WORK:;;123 Tech Street\\, Cairo\\, Egypt;;;;", result);
        Assert.Contains("NOTE:Digital Transformation Expert", result);
        Assert.Contains("URL:https://nfc-platform.com/u/ahmed-ali", result);
        Assert.Contains("PHOTO;VALUE=URI:https://cdn.example.com/avatar.jpg", result);
        Assert.Contains("X-SOCIALPROFILE;TYPE=linkedin:https://linkedin.com/in/ahmed", result);
        Assert.Contains("END:VCARD", result);
    }

    [Fact]
    public void BuildVCardBytes_ReturnsUtf8EncodedBytes()
    {
        var dto = new EmployeeDetailsDto { FullName = "أحمد علي" };

        var bytes = _sut.BuildVCardBytes(dto);
        var decodedString = Encoding.UTF8.GetString(bytes);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.Contains("FN:أحمد علي", decodedString);
    }

    [Fact]
    public void BuildVCardString_ThrowsArgumentNullException_WhenDtoIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.BuildVCardString(null!));
    }

    [Theory]
    [InlineData("Ahmed", "Ahmed;;;;")]
    [InlineData("Ahmed Ali", "Ali;Ahmed;;;")]
    [InlineData("Ahmed Mohamed Hassan Ali", "Ali;Ahmed;Mohamed Hassan;;")]
    public void BuildVCardString_HandlesSingleAndMultiWordNamesCorrectly(string inputName, string expectedNTag)
    {
        var dto = new EmployeeDetailsDto { FullName = inputName };

        var result = _sut.BuildVCardString(dto);

        Assert.Contains($"FN:{inputName}", result);
        Assert.Contains($"N:{expectedNTag}", result);
    }

    [Fact]
    public void BuildVCardString_EscapesSpecialCharacters_CommasSemicolonsNewlines()
    {
        var dto = new EmployeeDetailsDto
        {
            FullName = "Ali, Jr.; C#",
            JobTitle = "Tech Lead; Architect",
            Bio = "Line 1\nLine 2\r\nLine 3",
            Address = "Street 1, Apt 2; City"
        };

        var result = _sut.BuildVCardString(dto);

        Assert.Contains("FN:Ali\\, Jr.\\; C#", result);
        Assert.Contains("TITLE:Tech Lead\\; Architect", result);
        Assert.Contains("NOTE:Line 1\\nLine 2\\nLine 3", result);
        Assert.Contains("ADR;TYPE=WORK:;;Street 1\\, Apt 2\\; City;;;;", result);
    }

    [Fact]
    public void BuildVCardString_HandlesMinimalDtoWithNullOptionalFieldsWithoutCrashing()
    {
        var dto = new EmployeeDetailsDto
        {
            FullName = "Simple User",
            JobTitle = null!,
            CompanyName = null!,
            Department = null!,
            ContactEmail = null!,
            Phone = null!,
            WhatsApp = null!,
            Address = null!,
            Bio = null!,
            ProfileUrl = null!,
            ProfilePictureUrl = null!,
            Links = null!
        };

        var result = _sut.BuildVCardString(dto);

        Assert.NotNull(result);
        Assert.Contains("BEGIN:VCARD", result);
        Assert.Contains("VERSION:3.0", result);
        Assert.Contains("FN:Simple User", result);
        Assert.DoesNotContain("TITLE:", result);
        Assert.DoesNotContain("TEL;", result);
        Assert.DoesNotContain("EMAIL;", result);
        Assert.Contains("END:VCARD", result);
    }
}
