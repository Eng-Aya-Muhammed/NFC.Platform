using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class EmployeeMappingProfileTests
    {
        private readonly IMapper _mapper;

        public EmployeeMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EmployeeMappingProfile>();
                cfg.AddProfile<UserProfileMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void UserProfile_To_EmployeeDetailsDto_MapsStandardLinksCorrectly()
        {
            // Arrange
            var profile = new UserProfile
            {
                FullName = "John Doe",
                CustomLinks = new List<ProfileLink>
                {
                    new ProfileLink { Title = "LinkedIn", Url = "https://linkedin.com/in/john" },
                    new ProfileLink { Title = "Facebook", Url = "https://facebook.com/john" },
                    new ProfileLink { Title = "Custom Site", Url = "https://mycustom.com", DisplayOrder = 2 },
                    new ProfileLink { Title = "Another Site", Url = "https://another.com", DisplayOrder = 1 }
                }
            };

            // Act
            var dto = _mapper.Map<EmployeeDetailsDto>(profile);

            // Assert
            Assert.Equal("https://linkedin.com/in/john", dto.LinkedInUrl);
            Assert.Equal("https://facebook.com/john", dto.FacebookUrl);
            Assert.Equal(string.Empty, dto.InstagramUrl);
            Assert.Equal(string.Empty, dto.WebsiteUrl);

            // Standard links should be excluded from the custom links collection
            Assert.Equal(2, dto.CustomLinks.Count);
            Assert.Equal("Another Site", dto.CustomLinks[0].Title);
            Assert.Equal("Custom Site", dto.CustomLinks[1].Title);
        }

        [Fact]
        public void CreateEmployeeRequest_To_UserProfile_CreatesStandardLinksCorrectly()
        {
            // Arrange
            var request = new CreateEmployeeRequest
            {
                FullName = "Jane Doe",
                LinkedInUrl = "https://linkedin.com/in/jane",
                WebsiteUrl = "https://jane.com"
            };

            // Act
            var profile = _mapper.Map<UserProfile>(request);

            // Assert
            Assert.Equal(2, profile.CustomLinks.Count);
            Assert.Contains(profile.CustomLinks, l => l.Title == "LinkedIn" && l.Url == "https://linkedin.com/in/jane");
            Assert.Contains(profile.CustomLinks, l => l.Title == "Website" && l.Url == "https://jane.com");
        }

        [Fact]
        public void UpdateMyProfileRequest_To_UserProfile_UpdatesStandardLinksCorrectly()
        {
            // Arrange
            var profile = new UserProfile
            {
                FullName = "Original Name",
                CustomLinks = new List<ProfileLink>
                {
                    new ProfileLink { Title = "LinkedIn", Url = "https://linkedin.com/in/original" },
                    new ProfileLink { Title = "Instagram", Url = "https://instagram.com/original" }
                }
            };

            var request = new UpdateMyProfileRequest
            {
                FullName = "Updated Name",
                LinkedInUrl = "https://linkedin.com/in/updated", // should update
                InstagramUrl = "",                              // should remove
                FacebookUrl = "https://facebook.com/new"        // should add
            };

            // Act
            _mapper.Map(request, profile);

            // Assert
            Assert.Equal("Updated Name", profile.FullName);
            Assert.Equal(2, profile.CustomLinks.Count);
            Assert.Contains(profile.CustomLinks, l => l.Title == "LinkedIn" && l.Url == "https://linkedin.com/in/updated");
            Assert.Contains(profile.CustomLinks, l => l.Title == "Facebook" && l.Url == "https://facebook.com/new");
            Assert.DoesNotContain(profile.CustomLinks, l => l.Title == "Instagram");
        }

        [Fact]
        public void Employee_To_EmployeeDetailsDto_MapsUsingUserProfile()
        {
            // Arrange
            var employee = new Employee
            {
                FullName = "Employee Name",
                UserProfile = new UserProfile
                {
                    FullName = "Profile Name",
                    CustomLinks = new List<ProfileLink>
                    {
                        new ProfileLink { Title = "LinkedIn", Url = "https://linkedin.com/in/employee" }
                    }
                }
            };

            // Act
            var dto = _mapper.Map<EmployeeDetailsDto>(employee);

            // Assert
            Assert.Equal("https://linkedin.com/in/employee", dto.LinkedInUrl);
        }

        [Fact]
        public void UpdateEmployeeRequest_To_Employee_MapsCorrectly()
        {
            // Arrange
            var request = new UpdateEmployeeRequest
            {
                FullName = "Updated Name",
                JobTitle = "Senior Dev",
                Department = "Engineering",
                Status = NFC.Platform.Domain.Enums.UserStatus.Active
            };
            var employee = new Employee { FullName = "Old Name" };

            // Act
            _mapper.Map(request, employee);

            // Assert
            Assert.Equal("Updated Name", employee.FullName);
            Assert.Equal("Senior Dev", employee.JobTitle);
            Assert.Equal("Engineering", employee.Department);
            Assert.Equal(NFC.Platform.Domain.Enums.UserStatus.Active, employee.Status);
        }
    }
}
