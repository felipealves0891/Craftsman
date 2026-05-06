namespace Craftsman.Domain.Production.Services;

public sealed class ProductionScheduleOptions
{
    public const string SectionName = "Production";
    public const int DefaultWorkingHoursPerDay = 6;
    public const int DefaultWorkdayStartHour = 8;

    public int? WorkingHoursPerDay { get; set; }

    public int WorkdayStartHour { get; set; } = DefaultWorkdayStartHour;

    public int EffectiveWorkingHoursPerDay => WorkingHoursPerDay.GetValueOrDefault(DefaultWorkingHoursPerDay) > 0
        ? WorkingHoursPerDay.GetValueOrDefault(DefaultWorkingHoursPerDay)
        : DefaultWorkingHoursPerDay;

    public int EffectiveWorkdayStartHour => WorkdayStartHour is >= 0 and <= 23
        ? WorkdayStartHour
        : DefaultWorkdayStartHour;
}
