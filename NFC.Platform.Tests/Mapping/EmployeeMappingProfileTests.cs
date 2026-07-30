using System;
using AutoMapper;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
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
        public void Employee_To_EmployeeDto_MapsStatusAsString()
        {
            // Arrange
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Ahmed Ali",
                Email = "ahmed@company.com",
                JobTitle = "Software Engineer",
                Department = "IT",
                Status = UserStatus.Active
            };

            // Act
            var dto = _mapper.Map<EmployeeDto>(employee);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Ahmed Ali", dto.FullName);
            Assert.Equal("ahmed@company.com", dto.Email);
            Assert.Equal("Software Engineer", dto.JobTitle);
            Assert.Equal("IT", dto.Department);
            Assert.Equal("Active", dto.Status);
        }

        [Fact]
        public void Employee_To_EmployeeExportDto_MapsPhoneAndIsActive()
        {
            // Arrange
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Sara Hassan",
                Email = "sara@company.com",
                JobTitle = "HR Manager",
                Department = "HR",
                Status = UserStatus.Active,
                UserProfile = new UserProfile { Phone = "+201001234567" }
            };

            // Act
            var dto = _mapper.Map<EmployeeExportDto>(employee);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Sara Hassan", dto.FullName);
            Assert.Equal("+201001234567", dto.PhoneNumber);
            Assert.True(dto.IsActive);
        }
    }
}
