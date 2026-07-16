using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.API.Models;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Enums;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.BuildingBlocks.Localization;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class EmployeeControllerTests
    {
        private readonly IEmployeeService _employeeService;
        private readonly EmployeeController _sut;

        public EmployeeControllerTests()
        {
            _employeeService = Substitute.For<IEmployeeService>();
            _sut = new EmployeeController(_employeeService);
        }

        [Fact]
        public void EmployeeController_ShouldHaveAuthorizeAttributeWithCompanyAdminOnlyPolicy()
        {
            // Arrange & Act
            var type = typeof(EmployeeController);
            var attributes = type.GetCustomAttributes(typeof(AuthorizeAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
            var authorizeAttribute = attributes.First() as AuthorizeAttribute;
            Assert.NotNull(authorizeAttribute);
            Assert.Equal(AppPolicies.CompanyAdminOnly, authorizeAttribute.Policy);
        }

        [Fact]
        public async Task Create_ShouldCallCreateEmployeeAsync_OnEmployeeService()
        {
            // Arrange
            var request = new CreateEmployeeRequest
            {
                Email = "test@onpoint.com",
                FullName = "Test Employee",
                JobTitle = "Developer",
                Department = "Engineering"
            };

            var expectedResult = ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto());
            _employeeService.CreateEmployeeAsync(request).Returns(expectedResult);

            // Act
            var result = await _sut.Create(request) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _employeeService.Received(1).CreateEmployeeAsync(request);
        }

        [Fact]
        public async Task Create_ShouldReturnError_WhenServiceFails()
        {
            // Arrange
            var request = new CreateEmployeeRequest { Email = "test@onpoint.com" };
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Fail("Some error occurred", 400);
            _employeeService.CreateEmployeeAsync(request).Returns(expectedResult);

            // Act
            var result = await _sut.Create(request) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetPaged_ShouldCallGetPagedEmployeesAsync_OnEmployeeService()
        {
            // Arrange
            var request = new PaginationRequest();
            var search = "test";
            var expectedResult = ServiceResult<PagedResult<EmployeeDto>>.Success(PagedResult<EmployeeDto>.Create(new List<EmployeeDto>(), 0, 1, 10));
            _employeeService.GetPagedEmployeesAsync(request, search).Returns(expectedResult);

            // Act
            var result = await _sut.GetPaged(request, search) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _employeeService.Received(1).GetPagedEmployeesAsync(request, search);
        }

        [Fact]
        public async Task GetById_ShouldCallGetEmployeeDetailsAsync_OnEmployeeService()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto());
            _employeeService.GetEmployeeDetailsAsync(id).Returns(expectedResult);

            // Act
            var result = await _sut.GetById(id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _employeeService.Received(1).GetEmployeeDetailsAsync(id);
        }

        [Fact]
        public async Task Update_ShouldCallUpdateEmployeeJobDetailsAsync_OnEmployeeService()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateEmployeeRequest();
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto());
            _employeeService.UpdateEmployeeJobDetailsAsync(id, request).Returns(expectedResult);

            // Act
            var result = await _sut.Update(id, request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _employeeService.Received(1).UpdateEmployeeJobDetailsAsync(id, request);
        }

        [Fact]
        public async Task Delete_ShouldCallSoftDeleteEmployeeAsync_OnEmployeeService()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedResult = ServiceResult.Success();
            _employeeService.SoftDeleteEmployeeAsync(id).Returns(expectedResult);

            // Act
            var result = await _sut.Delete(id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _employeeService.Received(1).SoftDeleteEmployeeAsync(id);
        }
    }
}
