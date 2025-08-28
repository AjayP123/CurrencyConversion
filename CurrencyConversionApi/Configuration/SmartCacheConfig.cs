namespace CurrencyConversionApi.Configuration;

/// <summary>
/// Business-hours aware cache configuration - latest rates only
/// </summary>
public class SmartCacheConfig
{
    /// <summary>
    /// Optional override for current UTC time (test seam). If null, DateTime.UtcNow is used.
    /// </summary>
    public Func<DateTime>? UtcNowProvider { get; set; }
    /// <summary>
    /// CET timezone identifier (cross-platform compatible)
    /// </summary>
    public string CETTimeZone { get; set; } = GetCETTimeZoneId();

    /// <summary>
    /// Hour when Frankfurter API updates (16:00 CET)
    /// </summary>
    public int FrankfurterUpdateHour { get; set; } = 16;

    /// <summary>
    /// Buffer time after Frankfurter update to ensure data is published
    /// </summary>
    public TimeSpan UpdateBuffer { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// TTL during business hours (8:00-20:00 CET) - active trading
    /// </summary>
    public TimeSpan BusinessHoursTTL { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Off-hours TTL (20:00 - 8:00 CET)
    /// </summary>
    public TimeSpan OffHoursTTL { get; set; } = TimeSpan.FromHours(2);

    // Historical rates are NOT cached due to date range complexity and memory concerns

    /// <summary>
    /// Business hours start (24-hour format)
    /// </summary>
    public int BusinessHoursStart { get; set; } = 8;

    /// <summary>
    /// Business hours end (24-hour format)  
    /// </summary>
    public int BusinessHoursEnd { get; set; } = 20;

    /// <summary>
    /// Generate cache key for latest rates only
    /// </summary>
    public string GetCacheKey(string baseCurrency, bool isHistorical = false, DateTime? rateDate = null)
    {
        // Historical caching is disabled
        if (isHistorical)
        {
            throw new NotSupportedException("Historical rate caching is disabled due to date range complexity and memory concerns");
        }
        
        return $"latest_rates_{baseCurrency}";
    }

    /// <summary>
    /// Calculate optimal TTL with business hours awareness (latest rates only)
    /// </summary>
    public TimeSpan GetOptimalTTL(DateTime? rateDate = null)
    {
        var now = UtcNowProvider?.Invoke() ?? DateTime.UtcNow;
        var cetTimeZone = GetCETTimeZoneInfo();
        var cetNow = TimeZoneInfo.ConvertTime(now, cetTimeZone);

        // Historical data caching is disabled
    if (rateDate.HasValue && rateDate.Value.Date < (UtcNowProvider?.Invoke() ?? DateTime.UtcNow).Date)
        {
            throw new NotSupportedException("Historical rate caching is disabled - serve directly from API");
        }

        // Get time until next Frankfurter update
        var nextUpdateTime = GetNextFrankfurterUpdate(cetNow);
        var timeUntilUpdate = nextUpdateTime - cetNow;

        // Business hours logic for latest rates only
        if (IsBusinessHours(cetNow))
        {
            // During business hours: 15 minutes or until next update (whichever is shorter)
            return TimeSpan.FromTicks(Math.Min(timeUntilUpdate.Ticks, BusinessHoursTTL.Ticks));
        }
        else
        {
            // Off hours: 2 hours or until next update (whichever is shorter)
            return TimeSpan.FromTicks(Math.Min(timeUntilUpdate.Ticks, OffHoursTTL.Ticks));
        }
    }

    /// <summary>
    /// Check if current time is within business hours
    /// </summary>
    public bool IsBusinessHours(DateTime cetTime)
    {
        var hour = cetTime.Hour;
        return hour >= BusinessHoursStart && hour < BusinessHoursEnd;
    }

    /// <summary>
    /// Get next Frankfurter update time with buffer
    /// </summary>
    private DateTime GetNextFrankfurterUpdate(DateTime cetNow)
    {
        var updateTimeToday = cetNow.Date.AddHours(FrankfurterUpdateHour).Add(UpdateBuffer);
        
        // If today's update time has passed, return tomorrow's update time
        return cetNow >= updateTimeToday ? updateTimeToday.AddDays(1) : updateTimeToday;
    }

    /// <summary>
    /// Get cross-platform compatible CET timezone identifier
    /// </summary>
    private static string GetCETTimeZoneId()
    {
        // Try Windows timezone ID first
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            return "Central European Standard Time";
        }
        catch (TimeZoneNotFoundException)
        {
            // Try IANA timezone ID (Linux/macOS)
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
                return "Europe/Berlin";
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback to UTC+1
                return "W. Europe Standard Time";
            }
        }
    }

    /// <summary>
    /// Get CET TimeZoneInfo with cross-platform support
    /// </summary>
    private TimeZoneInfo GetCETTimeZoneInfo()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(CETTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback: try common CET timezone identifiers
            var commonCETIds = new[]
            {
                "Central European Standard Time", // Windows
                "Europe/Berlin",                  // IANA
                "Europe/Paris",                   // IANA alternative
                "W. Europe Standard Time"         // Windows fallback
            };

            foreach (var id in commonCETIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                    continue;
                }
            }

            // Ultimate fallback: create custom CET timezone
            return TimeZoneInfo.CreateCustomTimeZone("CET", TimeSpan.FromHours(1), "Central European Time", "CET");
        }
    }
}
