using System;
using System.Collections.Generic;
using AutoMapper;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class UserProfileMappingProfileTests
    {
        private readonly IMapper _mapper;

        public UserProfileMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UserProfileMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void UserProfile_To_EmployeeDetailsDto_MapsAllProfileFields()
        {
            var empId = Guid.NewGuid();
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                EmployeeId = empId,
                FullName = "Mahmoud Hassan",
                JobTitle = "Product Manager",
                Department = "Product",
                Bio = "Building awesome products",
                Phone = "+201234567890",
                ContactEmail = "mahmoud@company.com",
                Address = "Building 5, Business Park",
                ProfilePictureUrl = "https://cdn.example.com/pic.jpg",
                CustomLinks = new List<ProfileLink>
                {
                    new ProfileLink { Title = "LinkedIn", Url = "https://linkedin.com/in/mahmoud", DisplayOrder = 1 }
                }
            };

            var dto = _mapper.Map<EmployeeDetailsDto>(profile);

            Assert.NotNull(dto);
            Assert.Equal(empId, dto.Id);
            Assert.Equal("Mahmoud Hassan", dto.FullName);
            Assert.Equal("Product Manager", dto.JobTitle);
            Assert.Equal("Product", dto.Department);
            Assert.Equal("Building awesome products", dto.Bio);
            Assert.Equal("+201234567890", dto.Phone);
            Assert.Equal("mahmoud@company.com", dto.ContactEmail);
            Assert.Single(dto.Links);
        }

        [Fact]
        public void User_To_EmployeeDetailsDto_ConvertsUsingUserProfile()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "user123",
                Email = "user@test.com",
                Status = UserStatus.Active,
                UserProfile = new UserProfile
                {
                    FullName = "User Full Name",
                    JobTitle = "Engineer"
                }
            };

            var dto = _mapper.Map<EmployeeDetailsDto>(user);

            Assert.NotNull(dto);
            Assert.Equal(userId, dto.Id);
            Assert.Equal("user@test.com", dto.Email);
            Assert.Equal("User Full Name", dto.FullName);
            Assert.Equal("Active", dto.Status);
        }

        [Fact]
        public void UpdateEmployeeRequest_To_UserProfile_PreservesJobTitleAndDepartment_WhenNull()
        {
            var existingProfile = new UserProfile
            {
                FullName = "Existing Name",
                JobTitle = "Senior Developer",
                Department = "Engineering"
            };

            var updateRequest = new UpdateEmployeeRequest
            {
                FullName = "Updated Name",
                JobTitle = null,
                Department = null
            };

            _mapper.Map(updateRequest, existingProfile);

            Assert.Equal("Updated Name", existingProfile.FullName);
            Assert.Equal("Senior Developer", existingProfile.JobTitle);
            Assert.Equal("Engineering", existingProfile.Department);
        }
    }
}
