
namespace NFC.Platform.Tests.Services
{
    public class RateLimitingTests
    {
        [Fact]
        public void AddRateLimitingConfig_RegistersRateLimitingAndPolicies()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddLocalization();
            services.AddDistributedMemoryCache();
            services.AddSingleton<IMessageService, MessageService>();

            services.AddRateLimitingConfig();
            var provider = services.BuildServiceProvider();
            var rateLimiterOptions = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

            Assert.NotNull(rateLimiterOptions);
            Assert.Equal(429, rateLimiterOptions.RejectionStatusCode);
            Assert.NotNull(rateLimiterOptions.OnRejected);
        }

        [Theory]
        [InlineData("LoginPolicy", "/api/auth/login", "en", "Too many login attempts. Please try again in 60 seconds.")]
        [InlineData("LoginPolicy", "/api/auth/login", "ar", "لقد تجاوزت الحد الأقصى لمحاولات تسجيل الدخول. يرجى المحاولة مرة أخرى بعد 60 ثانية.")]
        [InlineData("RegisterPolicy", "/api/auth/register", "en", "Too many registration attempts. Please try again in 60 seconds.")]
        [InlineData("RegisterPolicy", "/api/auth/register", "ar", "لقد تجاوزت الحد الأقصى لمحاولات إنشاء الحساب. يرجى المحاولة مرة أخرى بعد 60 ثانية.")]
        [InlineData("ResetPasswordPolicy", "/api/auth/reset-password", "en", "Too many password reset attempts. Please try again in 60 seconds.")]
        [InlineData("ResetPasswordPolicy", "/api/auth/reset-password", "ar", "لقد تجاوزت الحد الأقصى لمحاولات إعادة تعيين كلمة المرور. يرجى المحاولة مرة أخرى بعد 60 ثانية.")]
        [InlineData("ForgotPasswordPolicy", "/api/auth/forgot-password", "en", "Too many forgot password requests. Please try again in 120 seconds.")]
        [InlineData("ResolvePublicProfilePolicy", "/api/public/cards/resolve/abc", "en", "Too many requests. Please try again in 60 seconds.")]
        public async Task OnRejected_ReturnsCorrectLocalizedMessage(
            string policyName, string requestPath, string culture, string expectedMessage)
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(culture);

            var httpContext = CreateHttpContextWithPolicy(policyName, "1.2.3.4", requestPath, provider);
            var context = new OnRejectedContext { HttpContext = httpContext, Lease = Substitute.For<RateLimitLease>() };

            await rateLimiterOptions.OnRejected!(context, CancellationToken.None);

            Assert.Equal(429, httpContext.Response.StatusCode);
            Assert.StartsWith("application/json", httpContext.Response.ContentType);

            var body = await ReadBodyAsync(httpContext);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.False(root.GetProperty("isSuccess").GetBoolean());
            Assert.Equal(429, root.GetProperty("statusCode").GetInt32());
            Assert.Equal(expectedMessage, root.GetProperty("message").GetString());

            var errors = root.GetProperty("errors");
            Assert.Equal(1, errors.GetArrayLength());
            Assert.Equal(expectedMessage, errors[0].GetString());
        }

        [Theory]
        [InlineData("LoginPolicy", "/api/auth/login", "60")]
        [InlineData("RegisterPolicy", "/api/auth/register", "60")]
        [InlineData("ResetPasswordPolicy", "/api/auth/reset-password", "60")]
        [InlineData("ForgotPasswordPolicy", "/api/auth/forgot-password", "120")]
        [InlineData("ResolvePublicProfilePolicy", "/api/public/cards/resolve/test", "60")]
        public async Task OnRejected_SetsRetryAfterHeader(
            string policyName, string requestPath, string expectedRetryAfterSeconds)
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            var httpContext = CreateHttpContextWithPolicy(policyName, "5.5.5.5", requestPath, provider);
            var context = new OnRejectedContext { HttpContext = httpContext, Lease = Substitute.For<RateLimitLease>() };

            await rateLimiterOptions.OnRejected!(context, CancellationToken.None);

            Assert.Equal(expectedRetryAfterSeconds, httpContext.Response.Headers["Retry-After"].ToString());
        }

        [Fact]
        public async Task CardActivationPolicy_SetsRetryAfterHeader_InSeconds()
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            var ip = "200.200.200.200";
            for (var i = 0; i < 3; i++)
            {
                var ctx = CreateHttpContextWithPolicy("CardActivationPolicy", ip, "/api/cards/activate", provider);
                var reqCtx = new OnRejectedContext { HttpContext = ctx, Lease = Substitute.For<RateLimitLease>() };
                await rateLimiterOptions.OnRejected!(reqCtx, CancellationToken.None);
            }

            var lockedCtx = CreateHttpContextWithPolicy("CardActivationPolicy", ip, "/api/cards/activate", provider);
            var lockedReqCtx = new OnRejectedContext { HttpContext = lockedCtx, Lease = Substitute.For<RateLimitLease>() };
            await rateLimiterOptions.OnRejected!(lockedReqCtx, CancellationToken.None);

            var retryAfter = lockedCtx.Response.Headers["Retry-After"].ToString();
            Assert.NotEmpty(retryAfter);
            Assert.True(int.Parse(retryAfter) >= 1799, $"Expected Retry-After >= 1799 seconds but got {retryAfter}");
        }

        [Fact]
        public async Task CardActivationPolicy_LockoutTriggersAfterThreeViolations()
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            var ip = "10.0.0.1";
            var path = "/api/cards/activate";

            var ctx1 = CreateHttpContextWithPolicy("CardActivationPolicy", ip, path, provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = ctx1, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            var body1 = await ReadBodyAsync(ctx1);
            Assert.Contains("activation", body1.ToLowerInvariant());
            Assert.DoesNotContain("locked", body1.ToLowerInvariant());

            var ctx2 = CreateHttpContextWithPolicy("CardActivationPolicy", ip, path, provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = ctx2, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);

            var ctx3 = CreateHttpContextWithPolicy("CardActivationPolicy", ip, path, provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = ctx3, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            var body3 = await ReadBodyAsync(ctx3);
            Assert.Contains("locked out", body3.ToLowerInvariant());

            var ctx4 = CreateHttpContextWithPolicy("CardActivationPolicy", ip, path, provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = ctx4, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            var body4 = await ReadBodyAsync(ctx4);
            Assert.Contains("locked out", body4.ToLowerInvariant());
        }

        [Fact]
        public async Task CardActivationPolicy_DifferentIPs_AreIsolated()
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            var ipA = "11.0.0.1";
            var ipB = "11.0.0.2";
            var path = "/api/cards/activate";

            for (var i = 0; i < 3; i++)
            {
                var ctx = CreateHttpContextWithPolicy("CardActivationPolicy", ipA, path, provider);
                await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = ctx, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            }

            var ctxB = CreateHttpContextWithPolicy("CardActivationPolicy", ipB, path, provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = ctxB, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            var bodyB = await ReadBodyAsync(ctxB);
            Assert.DoesNotContain("locked out", bodyB.ToLowerInvariant());
        }

        [Fact]
        public async Task ChangePasswordPolicy_LockoutTriggersAfterThreeViolations()
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            var ip = "10.0.0.2";
            var path = "/api/company/change-password";

            for (var i = 0; i < 3; i++)
            {
                var ctx = CreateHttpContextWithPolicy("ChangePasswordPolicy", ip, path, provider);
                await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = ctx, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            }

            var lockedCtx = CreateHttpContextWithPolicy("ChangePasswordPolicy", ip, path, provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = lockedCtx, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            var body = await ReadBodyAsync(lockedCtx);
            Assert.Contains("locked out", body.ToLowerInvariant());
        }

        [Fact]
        public async Task LoginAndRegisterPolicies_HaveIndependentCounters()
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            var loginCtx = CreateHttpContextWithPolicy("LoginPolicy", "9.9.9.9", "/api/auth/login", provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = loginCtx, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            var loginBody = await ReadBodyAsync(loginCtx);

            var registerCtx = CreateHttpContextWithPolicy("RegisterPolicy", "9.9.9.9", "/api/auth/register", provider);
            await rateLimiterOptions.OnRejected!(new OnRejectedContext { HttpContext = registerCtx, Lease = Substitute.For<RateLimitLease>() }, CancellationToken.None);
            var registerBody = await ReadBodyAsync(registerCtx);

            Assert.Contains("login", loginBody.ToLowerInvariant());
            Assert.Contains("registration", registerBody.ToLowerInvariant());
            Assert.NotEqual(loginBody, registerBody);
        }

        [Fact]
        public async Task OnRejected_ResponseShape_IsCorrect()
        {
            var (provider, rateLimiterOptions) = BuildProviderWithOptions();

            var httpContext = CreateHttpContextWithPolicy("LoginPolicy", "3.3.3.3", "/api/auth/login", provider);
            var context = new OnRejectedContext { HttpContext = httpContext, Lease = Substitute.For<RateLimitLease>() };

            await rateLimiterOptions.OnRejected!(context, CancellationToken.None);

            Assert.Equal(429, httpContext.Response.StatusCode);
            Assert.StartsWith("application/json", httpContext.Response.ContentType);

            var body = await ReadBodyAsync(httpContext);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("isSuccess", out _));
            Assert.True(root.TryGetProperty("statusCode", out _));
            Assert.True(root.TryGetProperty("message", out _));
            Assert.True(root.TryGetProperty("errors", out _));

            Assert.False(root.GetProperty("isSuccess").GetBoolean());
            Assert.Equal(429, root.GetProperty("statusCode").GetInt32());
        }

        private static (IServiceProvider Provider, RateLimiterOptions Options) BuildProviderWithOptions()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddLocalization(o => o.ResourcesPath = string.Empty);
            services.AddDistributedMemoryCache();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddRateLimitingConfig();

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
            return (provider, options);
        }

        private static DefaultHttpContext CreateHttpContextWithPolicy(
            string policyName, string ip, string path, IServiceProvider provider)
        {
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
            httpContext.Request.Path = path;

            var endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new EnableRateLimitingAttribute(policyName)),
                "TestEndpoint");
            httpContext.SetEndpoint(endpoint);

            httpContext.Response.Body = new MemoryStream();
            return httpContext;
        }

        private static async Task<string> ReadBodyAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return await reader.ReadToEndAsync();
        }
    }
}
