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
            Assert.Equal(4, dto.Links.Count);
            Assert.Contains(dto.Links, l => l.Title == "LinkedIn" && l.Url == "https://linkedin.com/in/john");
            Assert.Contains(dto.Links, l => l.Title == "Facebook" && l.Url == "https://facebook.com/john");
            Assert.Contains(dto.Links, l => l.Title == "Custom Site" && l.Url == "https://mycustom.com");
            Assert.Contains(dto.Links, l => l.Title == "Another Site" && l.Url == "https://another.com");
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
            Assert.Single(dto.Links);
            Assert.Equal("https://linkedin.com/in/employee", dto.Links[0].Url);
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
