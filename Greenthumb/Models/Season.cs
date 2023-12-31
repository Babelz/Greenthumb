using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.SmartEnum;

namespace Greenthumb.Models;

public sealed class Season : SmartEnum<Season, string>
{
    #region Members
    public static readonly Season Spring = new(nameof(Spring), nameof(Spring));
    public static readonly Season Summer = new(nameof(Summer), nameof(Summer));
    public static readonly Season Fall   = new(nameof(Fall), nameof(Fall));
    public static readonly Season Winter = new(nameof(Winter), nameof(Winter));
    #endregion

    private Season(string name, string value)
        : base(name, value)
    {
    }

    public DateTime FirstDayOf(DateTime epoch)
    {
        var firstDayOfMonth = default(DateTime);

        When(Spring).Then(() => firstDayOfMonth = new DateTime(epoch.Year, 3, 1));
        When(Summer).Then(() => firstDayOfMonth = new DateTime(epoch.Year, 6, 1));
        When(Fall).Then(() => firstDayOfMonth   = new DateTime(epoch.Year, 9, 1));
        When(Winter).Then(() => firstDayOfMonth = new DateTime(epoch.Year, 12, 1));

        return firstDayOfMonth;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Season Of(DateTime dateTime)
        => dateTime.Month switch
        {
            var m when ((stackalloc int[] { 3, 4, 5 }).Contains(m)) => Spring,
            var m when ((stackalloc int[] { 6, 7, 8 }).Contains(m)) => Summer,
            var m when ((stackalloc int[] { 9, 10, 11 }).Contains(m)) => Fall,
            var m when ((stackalloc int[] { 12, 1, 2 }).Contains(m)) => Winter,
            _ => throw new ArgumentOutOfRangeException($"could not determine season of date time {dateTime.ToString(Formats.CalendarDate)}")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInSeason(DateTime dateTime, Season season)
        => Of(dateTime) == season;
}

public class SeasonArrayConverter : JsonConverter<Season[]>
{
    public override Season[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();

        var results = new List<Season>();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
                results.Add(Season.FromName(reader.GetString()));
            else
                throw new JsonException($"unexpected token type: {reader.TokenType}");

            reader.Read();
        }

        return results.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, Season[]? value, JsonSerializerOptions options)
    {
        if (value == null)
            return;

        writer.WriteStartArray();

        foreach (var item in value)
            writer.WriteStringValue(item.Name);

        writer.WriteEndArray();
    }
}