namespace NFC.Platform.BuildingBlocks.Extensions
{
    public static class RateLimitingServiceExtensions
    {
        private const int LockoutViolationThreshold = 3;
        private const int LockoutDurationMinutes = 30;

        public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("LoginPolicy", httpContext =>
                {
                    var ip = GetClientIp(httpContext);
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"login_{ip}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });

                options.AddPolicy("RegisterPolicy", httpContext =>
                {
                    var ip = GetClientIp(httpContext);
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"register_{ip}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });

                options.AddPolicy("ResetPasswordPolicy", httpContext =>
                {
                    var ip = GetClientIp(httpContext);
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"resetpwd_{ip}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });

                // 3 requests per 2 minutes — stricter to prevent email spam
                options.AddPolicy("ForgotPasswordPolicy", httpContext =>
                {
                    var ip = GetClientIp(httpContext);
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"forgot_{ip}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(2),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });

                options.AddPolicy("ResolvePublicProfilePolicy", httpContext =>
                {
                    var ip = GetClientIp(httpContext);
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"profile_{ip}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });



                // 3 requests per 10 minutes + distributed lockout after 3 violations
                options.AddPolicy("ChangePasswordPolicy", httpContext =>
                {
                    var ip = GetClientIp(httpContext);
                    var cache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();
                    return GetLockoutPartition("ChangePassword", ip, 3, TimeSpan.FromMinutes(10), cache);
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    var messageService = context.HttpContext.RequestServices.GetRequiredService<IMessageService>();
                    var cache = context.HttpContext.RequestServices.GetService<IDistributedCache>();

                    var endpoint = context.HttpContext.GetEndpoint();
                    var policyName = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                    var ip = GetClientIp(context.HttpContext);

                    string messageKey = "TooManyAuthAttempts";
                    int messageParam = 60;
                    int retryAfterSeconds = 60;

                    switch (policyName)
                    {
                        case "LoginPolicy":
                            messageKey = "TooManyLoginAttempts";
                            break;

                        case "RegisterPolicy":
                            messageKey = "TooManyRegisterAttempts";
                            break;

                        case "ResetPasswordPolicy":
                            messageKey = "TooManyResetPasswordAttempts";
                            break;

                        case "ForgotPasswordPolicy":
                            messageKey = "TooManyForgotPasswordRequests";
                            messageParam = 120;
                            retryAfterSeconds = 120;
                            break;

                        case "ResolvePublicProfilePolicy":
                            messageKey = "TooManyProfileRequests";
                            break;



                        case "ChangePasswordPolicy" when cache != null:
                        {
                            var (isLocked, minutesLeft, _) = await GetOrUpdateLockoutStateAsync(
                                cache, "ChangePassword", ip, LockoutViolationThreshold,
                                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(LockoutDurationMinutes));

                            if (isLocked)
                            {
                                messageKey = "ChangePasswordLockedOut";
                                messageParam = minutesLeft;
                                retryAfterSeconds = minutesLeft * 60;
                            }
                            else
                            {
                                messageKey = "TooManyChangePasswordAttempts";
                                messageParam = 600;
                                retryAfterSeconds = 600;
                            }
                            break;
                        }
                    }

                    // RFC 7231 — always in seconds
                    context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

                    var localizedMessage = messageService.Get(messageKey, messageParam);
                    var result = ServiceResult.Fail(localizedMessage, StatusCodes.Status429TooManyRequests);
                    await context.HttpContext.Response.WriteAsJsonAsync(result, token);
                };
            });

            return services;
        }

        private static RateLimitPartition<string> GetLockoutPartition(
            string policyPrefix, string ip, int permitLimit, TimeSpan window, IDistributedCache cache)
        {
            var lockoutKey = $"lockout_{policyPrefix}_{ip}";
            var lockoutBytes = cache.Get(lockoutKey);

            if (lockoutBytes != null)
            {
                var lockedUntil = DateTime.Parse(Encoding.UTF8.GetString(lockoutBytes));
                if (DateTime.UtcNow < lockedUntil)
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"locked_{policyPrefix}_{ip}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 0,
                            Window = TimeSpan.FromSeconds(1),
                            QueueLimit = 0
                        });
                }
            }

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"{policyPrefix}_{ip}",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    SegmentsPerWindow = 4,
                    QueueLimit = 0
                });
        }

        private static async Task<(bool IsLockedOut, int MinutesRemaining, int ViolationCount)>
            GetOrUpdateLockoutStateAsync(
                IDistributedCache cache, string policyPrefix, string ip,
                int threshold, TimeSpan violationWindow, TimeSpan lockoutDuration)
        {
            var lockoutKey = $"lockout_{policyPrefix}_{ip}";
            var violationsKey = $"violations_{policyPrefix}_{ip}";

            var lockoutBytes = await cache.GetAsync(lockoutKey);
            if (lockoutBytes != null)
            {
                var lockedUntil = DateTime.Parse(Encoding.UTF8.GetString(lockoutBytes));
                if (DateTime.UtcNow < lockedUntil)
                {
                    var minutesRemaining = (int)Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalMinutes);
                    return (true, minutesRemaining, 0);
                }
            }

            var countBytes = await cache.GetAsync(violationsKey);
            int count = countBytes != null ? int.Parse(Encoding.UTF8.GetString(countBytes)) : 0;
            count++;

            if (count >= threshold)
            {
                var lockedUntil = DateTime.UtcNow.Add(lockoutDuration);
                await cache.SetAsync(
                    lockoutKey,
                    Encoding.UTF8.GetBytes(lockedUntil.ToString("o")),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lockoutDuration });
                await cache.RemoveAsync(violationsKey);

                return (true, (int)lockoutDuration.TotalMinutes, count);
            }
            else
            {
                await cache.SetAsync(
                    violationsKey,
                    Encoding.UTF8.GetBytes(count.ToString()),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = violationWindow });

                return (false, 0, count);
            }
        }

        private static string GetClientIp(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ipList = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ipList.Length > 0)
                    return ipList[0].Trim();
            }
            var ip = context.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrEmpty(ip) ? "unknown" : ip;
        }
    }
}
