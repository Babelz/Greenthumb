using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;

namespace Greenthumb.Models;

public sealed class Fertilization
{
    [JsonConverter(typeof(OccurrenceConverter))]
    public Occurrence Occurrence
    {
        get;
        set;
    }

    public string FertilizerType
    {
        get;
        set;
    }

    [JsonConverter(typeof(SeasonArrayConverter))]
    public Season[]? Seasons
    {
        get;
        set;
    }

    public string? Notes
    {
        get;
        set;
    }

    [JsonIgnore] public bool OverTheYear => Seasons == null;
}

public sealed class SoilChange
{
    public string SoilType
    {
        get;
        set;
    }

    [JsonConverter(typeof(OccurrenceConverter))]
    public Occurrence Every
    {
        get;
        set;
    }

    [JsonConverter(typeof(SmartEnumNameConverter<Season, string>))]
    public Season Season
    {
        get;
        set;
    }
}

public sealed class Watering
{
    [JsonConverter(typeof(SeasonArrayConverter))]
    public Season[]? Seasons
    {
        get;
        set;
    }

    [JsonConverter(typeof(OccurrenceConverter))]
    public Occurrence Occurrence
    {
        get;
        set;
    }

    public string Condition
    {
        get;
        set;
    }

    public string? Notes
    {
        get;
        set;
    }

    [JsonIgnore] public bool OverTheYear => Seasons == null;
}

public sealed class Regimen
{
    public List<Fertilization>? Fertilization
    {
        get;
        set;
    }

    public SoilChange SoilChange
    {
        get;
        set;
    }

    public List<Watering> Watering
    {
        get;
        set;
    }
}

public sealed class Plant
{
    public string Name
    {
        get;
        set;
    }

    [JsonConverter(typeof(CalendarDateConverter))]
    public DateTime SoilChangedAt
    {
        get;
        set;
    }

    [JsonConverter(typeof(CalendarDateConverter))]
    public DateTime SoilFertilizedAt
    {
        get;
        set;
    }

    [JsonConverter(typeof(CalendarDateConverter))]
    public DateTime WateredAt
    {
        get;
        set;
    }

    public Regimen Regimen
    {
        get;
        set;
    }
}