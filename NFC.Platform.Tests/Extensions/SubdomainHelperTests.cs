using NFC.Platform.Application.Extensions;
using Xunit;

namespace NFC.Platform.Tests.Extensions
{
    public class SubdomainHelperTests
    {
        [Theory]
        [InlineData("Ahmed Ali", "ahmed-ali")]
        [InlineData("John   Doe  ", "john-doe")]
        [InlineData("Special! @#$ Characters", "special-characters")]
        [InlineData("", "user")]
        [InlineData(null, "user")]
        public void Slugify_ConvertsInputToUrlSafeSlug(string input, string expected)
        {
            var result = SubdomainHelper.Slugify(input);
            Assert.Equal(expected, result);
        }
    }
}
