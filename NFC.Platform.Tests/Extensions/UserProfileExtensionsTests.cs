using System;
using System.Collections.Generic;
using System.Linq;
using NFC.Platform.Application.DTOs.Profile;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Extensions
{
    public class UserProfileExtensionsTests
    {
        // -------------------------------------------------------------------------------------------------
        // UpdateCustomLinks (IEnumerable<CustomLinkInput> links) - Used in B2B Flow (Create/Update Employee)
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void UpdateCustomLinks_WithCustomLinkInput_ClearsOldLinksAndAddsNewOnesInOrder()
        {
            // Arrange
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                CustomLinks = new List<ProfileLink>
                {
                    new ProfileLink { Title = "OldLink", Url = "https://old.com", DisplayOrder = 1 }
                }
            };

            var newLinks = new List<CustomLinkInput>
            {
                new CustomLinkInput { Title = "LinkedIn", Url = "https://linkedin.com/new" },
                new CustomLinkInput { Title = "My Website", Url = "https://mywebsite.com" }
            };

            // Act
            profile.UpdateCustomLinks(newLinks);

            // Assert
            Assert.Equal(2, profile.CustomLinks.Count);
            
            var linkedIn = profile.CustomLinks.First(l => l.Title == "LinkedIn");
            Assert.Equal("https://linkedin.com/new", linkedIn.Url);
            Assert.Equal(1, linkedIn.DisplayOrder);
            Assert.Equal(profile.Id, linkedIn.UserProfileId);
            Assert.Equal(profile.TenantId, linkedIn.TenantId);

            var myWebsite = profile.CustomLinks.First(l => l.Title == "My Website");
            Assert.Equal("https://mywebsite.com", myWebsite.Url);
            Assert.Equal(2, myWebsite.DisplayOrder);

            Assert.DoesNotContain(profile.CustomLinks, l => l.Title == "OldLink");
        }

        [Fact]
        public void UpdateCustomLinks_WithCustomLinkInput_IgnoresEmptyOrWhitespaceLinks()
        {
            // Arrange
            var profile = new UserProfile();
            var newLinks = new List<CustomLinkInput>
            {
                new CustomLinkInput { Title = "Valid", Url = "https://valid.com" },
                new CustomLinkInput { Title = "", Url = "https://missing-title.com" },
                new CustomLinkInput { Title = "MissingUrl", Url = " " },
                new CustomLinkInput { Title = null!, Url = null! }
            };

            // Act
            profile.UpdateCustomLinks(newLinks);

            // Assert
            Assert.Single(profile.CustomLinks);
            Assert.Equal("Valid", profile.CustomLinks.First().Title);
        }

        [Fact]
        public void UpdateCustomLinks_WithCustomLinkInput_HandlesNullListGracefully()
        {
            // Arrange
            var profile = new UserProfile
            {
                CustomLinks = new List<ProfileLink> { new ProfileLink { Title = "Old", Url = "OldUrl" } }
            };

            // Act
            profile.UpdateCustomLinks((IEnumerable<CustomLinkInput>)null!);

            // Assert
            Assert.Empty(profile.CustomLinks);
        }

        // -------------------------------------------------------------------------------------------------
        // UpdateCustomLinks (IEnumerable<string> links) - Used in B2C Flow (Sync Links)
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void UpdateCustomLinks_WithStringUrls_AddsNewLinksAndRemovesObsoleteOnes()
        {
            // Arrange
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                CustomLinks = new List<ProfileLink>
                {
                    new ProfileLink { Title = "https://keep.com", Url = "https://keep.com", DisplayOrder = 99 },
                    new ProfileLink { Title = "https://remove.com", Url = "https://remove.com", DisplayOrder = 1 }
                }
            };

            var syncUrls = new List<string>
            {
                "https://keep.com",
                "https://new.com"
            };

            // Act
            profile.UpdateCustomLinks(syncUrls);

            // Assert
            Assert.Equal(2, profile.CustomLinks.Count);
            
            var keepLink = profile.CustomLinks.First(l => l.Url == "https://keep.com");
            Assert.Equal(1, keepLink.DisplayOrder);
            
            var newLink = profile.CustomLinks.First(l => l.Url == "https://new.com");
            Assert.Equal("https://new.com", newLink.Title);
            Assert.Equal(2, newLink.DisplayOrder);

            Assert.DoesNotContain(profile.CustomLinks, l => l.Url == "https://remove.com");
        }

        [Fact]
        public void UpdateCustomLinks_WithStringUrls_RemovesDuplicatesAndWhitespace()
        {
            // Arrange
            var profile = new UserProfile();
            var syncUrls = new List<string>
            {
                "https://valid.com",
                " https://valid.com ",
                "https://VALID.com",
                "   ",
                ""
            };

            // Act
            profile.UpdateCustomLinks(syncUrls);

            // Assert
            Assert.Single(profile.CustomLinks);
            Assert.Equal("https://valid.com", profile.CustomLinks.First().Url);
        }

        [Fact]
        public void UpdateCustomLinks_WithStringUrls_HandlesNullListGracefully()
        {
            // Arrange
            var profile = new UserProfile
            {
                CustomLinks = new List<ProfileLink> { new ProfileLink { Url = "https://old.com" } }
            };

            // Act
            profile.UpdateCustomLinks((IEnumerable<string>)null!);

            // Assert
            Assert.Empty(profile.CustomLinks);
        }
    }
}
