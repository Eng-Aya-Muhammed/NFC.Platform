using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NFC.Platform.BuildingBlocks.Localization;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Localization
{
    public class MessageServiceTests
    {

        private static LocalizedString Found(string key, string value)
            => new(key, value, resourceNotFound: false);

        private static LocalizedString NotFound(string key)
            => new(key, key, resourceNotFound: true);

        private static MessageService BuildService(
            IStringLocalizer<SuccessMessages>    success,
            IStringLocalizer<ErrorMessages>      error,
            IStringLocalizer<ValidationMessages> validation,
            IStringLocalizer<BusinessMessages>   business)
            => new(success, error, validation, business);


        [Fact]
        public void Get_DelegatesToSuccessLocalizer_WhenKeyExistsThere()
        {
            const string key           = "OperationSuccess";
            const string expectedValue = "Operation completed successfully.";

            var successLocalizer    = Substitute.For<IStringLocalizer<SuccessMessages>>();
            var errorLocalizer      = Substitute.For<IStringLocalizer<ErrorMessages>>();
            var validationLocalizer = Substitute.For<IStringLocalizer<ValidationMessages>>();
            var businessLocalizer   = Substitute.For<IStringLocalizer<BusinessMessages>>();

            successLocalizer[key].Returns(Found(key, expectedValue));
            successLocalizer[key, Arg.Any<object[]>()].Returns(Found(key, expectedValue));

            errorLocalizer[key].Returns(NotFound(key));
            errorLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));

            validationLocalizer[key].Returns(NotFound(key));
            validationLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));

            businessLocalizer[key].Returns(NotFound(key));
            businessLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));

            var sut = BuildService(successLocalizer, errorLocalizer, validationLocalizer, businessLocalizer);

            var result = sut.Get(key);

            
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void Get_DelegatesToValidationLocalizer_WhenKeyOnlyExistsThere()
        {
            const string key       = "RequiredField";
            const string fieldName = "Email";
            const string formatted = "The field Email is required.";

            var successLocalizer    = Substitute.For<IStringLocalizer<SuccessMessages>>();
            var errorLocalizer      = Substitute.For<IStringLocalizer<ErrorMessages>>();
            var validationLocalizer = Substitute.For<IStringLocalizer<ValidationMessages>>();
            var businessLocalizer   = Substitute.For<IStringLocalizer<BusinessMessages>>();

            successLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));
            errorLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));
            validationLocalizer[key, Arg.Any<object[]>()].Returns(Found(key, formatted));
            businessLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));

            var sut = BuildService(successLocalizer, errorLocalizer, validationLocalizer, businessLocalizer);

            var result = sut.Get(key, fieldName);

            Assert.Equal(formatted, result);
        }

        [Fact]
        public void Get_ReturnsFallbackKey_WhenKeyExistsInNoLocalizer()
        {
            const string key = "NonExistentKey";

            var successLocalizer    = Substitute.For<IStringLocalizer<SuccessMessages>>();
            var errorLocalizer      = Substitute.For<IStringLocalizer<ErrorMessages>>();
            var validationLocalizer = Substitute.For<IStringLocalizer<ValidationMessages>>();
            var businessLocalizer   = Substitute.For<IStringLocalizer<BusinessMessages>>();

            successLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));
            errorLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));
            validationLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));
            businessLocalizer[key, Arg.Any<object[]>()].Returns(NotFound(key));

            var sut = BuildService(successLocalizer, errorLocalizer, validationLocalizer, businessLocalizer);

            Assert.Equal(key, sut.Get(key));
        }


        [Fact]
        public void Get_AndDirectLocalizer_ReturnSameValue_WhenCultureIsArabic()
        {
            
            var services = new ServiceCollection();
            services.AddLogging();          
            services.AddLocalization(options => options.ResourcesPath = string.Empty);
            services.AddSingleton<IMessageService, MessageService>();

            var provider = services.BuildServiceProvider();

            var directLocalizer = provider.GetRequiredService<IStringLocalizer<SuccessMessages>>();
            var messageService  = provider.GetRequiredService<IMessageService>();

            const string key = "OperationSuccess";

            CultureInfo.CurrentUICulture = new CultureInfo("ar");

            var directAr  = directLocalizer[key].Value;
            var serviceAr = messageService.Get(key);

            Assert.Equal(directAr, serviceAr);

            CultureInfo.CurrentUICulture = new CultureInfo("en");

            var directEn  = directLocalizer[key].Value;
            var serviceEn = messageService.Get(key);

            Assert.Equal(directEn, serviceEn);

           
            Assert.NotEqual(directAr, directEn);
        }
    }
}
