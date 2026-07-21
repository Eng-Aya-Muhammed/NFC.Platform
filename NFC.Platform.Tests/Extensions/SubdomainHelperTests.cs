using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Domain.Entities;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Extensions
{
    public class SubdomainHelperTests
    {
        [Theory]
        [InlineData("Ahmed Ali", "ahmed-ali")]
        [InlineData("Ghaith Mohammed", "ghaith-mohammed")]
        [InlineData("  John   Doe  ", "john-doe")]
        [InlineData("User@123", "user123")]
        [InlineData("Some-Name", "some-name")]
        [InlineData("", "user")]
        [InlineData(null, "user")]
        public void Slugify_ShouldFormatCorrectly(string input, string expected)
        {
            var result = SubdomainHelper.Slugify(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateUnique_ShouldReturnBaseSlug_WhenNotInHashSet()
        {
            var existing = new HashSet<string>();
            var result = SubdomainHelper.GenerateUnique("Ahmed Ali", existing);
            
            Assert.Equal("ahmed-ali", result);
            Assert.Contains("ahmed-ali", existing);
        }

        [Fact]
        public void GenerateUnique_ShouldAppendNumber_WhenExistsInHashSet()
        {
            var existing = new HashSet<string> { "ahmed-ali", "ahmed-ali-1" };
            var result = SubdomainHelper.GenerateUnique("Ahmed Ali", existing);
            
            Assert.Equal("ahmed-ali-2", result);
            Assert.Contains("ahmed-ali-2", existing);
        }

        [Fact]
        public async Task GenerateUniqueAsync_ShouldReturnBaseSlug_WhenNotInDatabase()
        {
            var repo = Substitute.For<IGenericRepository<UserProfile>>();
            var profiles = new List<UserProfile>().AsQueryable().BuildMock();
            repo.GetQueryable().Returns(profiles);

            var result = await SubdomainHelper.GenerateUniqueAsync("Ahmed Ali", repo);
            
            Assert.Equal("ahmed-ali", result);
        }

        [Fact]
        public async Task GenerateUniqueAsync_ShouldAppendNumber_WhenExistsInDatabase()
        {
            var repo = Substitute.For<IGenericRepository<UserProfile>>();
            var profiles = new List<UserProfile> 
            { 
                new UserProfile { Id = Guid.NewGuid(), Subdomain = "ahmed-ali" },
                new UserProfile { Id = Guid.NewGuid(), Subdomain = "ahmed-ali-1" }
            }.AsQueryable().BuildMock();
            
            repo.GetQueryable().Returns(profiles);

            var result = await SubdomainHelper.GenerateUniqueAsync("Ahmed Ali", repo);
            
            Assert.Equal("ahmed-ali-2", result);
        }

        [Fact]
        public async Task GenerateUniqueAsync_ShouldIgnoreExcludedProfileId()
        {
            var excludeId = Guid.NewGuid();
            var repo = Substitute.For<IGenericRepository<UserProfile>>();
            var profiles = new List<UserProfile> 
            { 
                new UserProfile { Id = excludeId, Subdomain = "ahmed-ali" }
            }.AsQueryable().BuildMock();
            
            repo.GetQueryable().Returns(profiles);

            var result = await SubdomainHelper.GenerateUniqueAsync("Ahmed Ali", repo, excludeId);
            
            // Should return ahmed-ali because the existing one is the one we are excluding
            Assert.Equal("ahmed-ali", result);
        }
    }
}
