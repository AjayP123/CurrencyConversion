using CurrencyConversionApi.Configuration;
using FluentAssertions;
using Xunit;

namespace CurrencyConversionApi.Tests.Configuration;

public class SmartCacheConfigTests
{
    [Fact]
    public void GetCacheKey_Historical_Throws()
    {
        var cfg = new SmartCacheConfig();
        Action act = () => cfg.GetCacheKey("EUR", isHistorical: true, rateDate: DateTime.UtcNow.AddDays(-1));
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetOptimalTTL_HistoricalDate_Throws()
    {
        var cfg = new SmartCacheConfig();
        Action act = () => cfg.GetOptimalTTL(DateTime.UtcNow.AddDays(-2));
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void IsBusinessHours_ReturnsExpected()
    {
        var cfg = new SmartCacheConfig();
        // Force a date at 10:00 CET (assume CET == local offset not critical for test) by constructing a DateTime with that hour
        var dt = new DateTime(2024, 1, 1, cfg.BusinessHoursStart + 1, 0, 0);
        cfg.IsBusinessHours(dt).Should().BeTrue();
        var off = new DateTime(2024, 1, 1, cfg.BusinessHoursEnd + 1, 0, 0);
        cfg.IsBusinessHours(off).Should().BeFalse();
    }

    [Fact]
    public void GetOptimalTTL_ReturnsPositive()
    {
        var cfg = new SmartCacheConfig();
        var ttl = cfg.GetOptimalTTL();
        ttl.Should().BeGreaterThan(TimeSpan.Zero);
        ttl.Should().BeLessOrEqualTo(cfg.OffHoursTTL); // upper bound
    }

    [Fact]
    public void GetOptimalTTL_BusinessHours_CapsAtBusinessTtl()
    {
        var cfg = new SmartCacheConfig();
        // Choose a UTC time that maps to 10:00 CET reliably. We'll pick a naive 10:00 and assume conversion won't shift date.
        var cetTarget = new DateTime(2024, 1, 2, cfg.BusinessHoursStart + 2, 0, 0, DateTimeKind.Unspecified);
        cfg.UtcNowProvider = () => cetTarget.AddHours(-1); // crude but sufficient: ensure still before update hour so nextUpdate > BusinessHoursTTL
        var ttl = cfg.GetOptimalTTL();
        ttl.Should().BeLessOrEqualTo(cfg.BusinessHoursTTL);
        ttl.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetOptimalTTL_OffHours_CapsAtOffHoursTtl()
    {
        var cfg = new SmartCacheConfig();
        // Force a late hour (22:00 CET equivalent)
        var cetLate = new DateTime(2024, 1, 2, cfg.BusinessHoursEnd + 2, 0, 0, DateTimeKind.Unspecified);
        cfg.UtcNowProvider = () => cetLate;
        var ttl = cfg.GetOptimalTTL();
        ttl.Should().BeLessOrEqualTo(cfg.OffHoursTTL);
        ttl.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetOptimalTTL_NearUpdate_UsesTimeUntilUpdateWhenShorter()
    {
        var cfg = new SmartCacheConfig
        {
            FrankfurterUpdateHour = 16,
            UpdateBuffer = TimeSpan.FromMinutes(5),
            BusinessHoursTTL = TimeSpan.FromMinutes(30), // longer than remaining
            OffHoursTTL = TimeSpan.FromHours(3)
        };
        // Build a CET time 3 minutes before update+buffer: 16:05 (update+buffer) minus 3 = 16:02 CET
        var cetPreUpdate = new DateTime(2024, 1, 3, cfg.FrankfurterUpdateHour, 0, 0, DateTimeKind.Unspecified)
            .Add(cfg.UpdateBuffer)
            .AddMinutes(-3);
        // Convert this CET time back to UTC by finding the TimeZone offset
        var tz = TimeZoneInfo.FindSystemTimeZoneById(cfg.CETTimeZone);
        var utcEquivalent = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(cetPreUpdate, DateTimeKind.Unspecified), tz);
        cfg.UtcNowProvider = () => utcEquivalent;
        var ttl = cfg.GetOptimalTTL();
        ttl.Should().BeLessThan(cfg.BusinessHoursTTL); // constrained by update window
        ttl.Should().BeLessThan(TimeSpan.FromMinutes(10));
        ttl.Should().BeGreaterThan(TimeSpan.FromMinutes(1));
    }
}
