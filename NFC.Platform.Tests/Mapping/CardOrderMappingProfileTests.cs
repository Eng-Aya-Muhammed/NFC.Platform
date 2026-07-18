using System;
using System.Collections.Generic;
using AutoMapper;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class CardOrderMappingProfileTests
    {
        private readonly IMapper _mapper;

        public CardOrderMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CardOrderMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void EmployeeImportJob_To_EmployeesImportStatusDto_MapsCorrectly()
        {
            // Arrange
            var job = new EmployeeImportJob
            {
                Status = EmployeeImportJobStatus.Completed,
                TotalRows = 10,
                Imported = 8,
                Skipped = 2,
                ErrorsJson = "[\"Row 2: Email already exists\",\"Row 5: Invalid email format\"]"
            };

            // Act
            var dto = _mapper.Map<EmployeesImportStatusDto>(job);

            // Assert
            Assert.Equal("Completed", dto.Status);
            Assert.Equal(10, dto.TotalRows);
            Assert.Equal(8, dto.Imported);
            Assert.Equal(2, dto.Skipped);
            Assert.NotNull(dto.Errors);
            Assert.Equal(2, dto.Errors.Count);
            Assert.Equal("Row 2: Email already exists", dto.Errors[0]);
            Assert.Equal("Row 5: Invalid email format", dto.Errors[1]);
        }

        [Fact]
        public void EmployeeImportJob_To_EmployeesImportStatusDto_HandlesNullOrEmptyErrors()
        {
            // Arrange
            var job = new EmployeeImportJob
            {
                Status = EmployeeImportJobStatus.Processing,
                TotalRows = 5,
                Imported = 0,
                Skipped = 0,
                ErrorsJson = null
            };

            // Act
            var dto = _mapper.Map<EmployeesImportStatusDto>(job);

            // Assert
            Assert.Equal("Processing", dto.Status);
            Assert.NotNull(dto.Errors);
            Assert.Empty(dto.Errors);
        }
    }
}
