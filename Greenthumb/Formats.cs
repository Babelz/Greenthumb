using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenthumb;

public static class Formats
{
    public const string CalendarDate = "yyyy-M-d";
}

public sealed class CalendarDateConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();

        return string.IsNullOrEmpty(text) ? default : DateTime.ParseExact(text, Formats.CalendarDate, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Formats.CalendarDate));
}