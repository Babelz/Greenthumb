using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ardalis.SmartEnum;
using Greenthumb.Utils;

namespace Greenthumb.Models;

public sealed class OccurrenceWindow : SmartEnum<OccurrenceWindow, string>
{
    #region Members
    public static readonly OccurrenceWindow Week  = new(nameof(Week), "w");
    public static readonly OccurrenceWindow Month = new(nameof(Month), "m");
    public static readonly OccurrenceWindow Year  = new(nameof(Year), "y");
    #endregion

    private OccurrenceWindow(string name, string value)
        : base(name, value)
    {
    }
}

public record Occurrence(int Times, OccurrenceWindow Window)
{
    public ImmutableList<DateTime> DatesOver(DateTime epoch)
    {
        var datesOver = new List<DateTime>();

        Window.When(OccurrenceWindow.Week)
              .Then(() =>
              {
                  DateIterator.Until(epoch, (d) => d.AddDays(7), (d) => d.Year <= epoch.Year)
                              .ToImmutableList()
                              .ForEach(d =>
                              {
                                  for (var i = 1; i <= Times; i++)
                                      datesOver.Add(d.AddDays((i - 1) * (7 / Times) % 7));
                              });
              })
              .When(OccurrenceWindow.Month)
              .Then(() =>
              {
                  DateIterator.Until(epoch, (d) => d.AddMonths(1), (d) => d.Year <= epoch.Year)
                              .ToImmutableList()
                              .ForEach(d =>
                              {
                                  var daysInMonth = DateTime.DaysInMonth(d.Year, d.Month);

                                  for (var i = 1; i <= Times; i++)
                                      datesOver.Add(d.AddDays((i - 1) * (daysInMonth / Times) % daysInMonth));
                              });
              })
              .When(OccurrenceWindow.Year)
              .Then(() =>
              {
                  var daysInYear = DateTime.IsLeapYear(epoch.Year) ? 366 : 365;

                  for (var i = 1; i <= Times; i++)
                      datesOver.Add(epoch.AddDays((i - 1) * (daysInYear / Times) % daysInYear));
              })
              .Default(() => throw new ArgumentOutOfRangeException($"unsupported {nameof(OccurrenceWindow)} value \"{Window.Value}\""));

        return datesOver.Where(d => d.Year == epoch.Year).ToImmutableList();
    }

    public static Occurrence Parse(string text)
    {
        if (!Regex.IsMatch(text, @$"\d+/[{string.Join('|', OccurrenceWindow.List.Select(i => i.Value))}]"))
            throw new ArgumentException($"invalid occurrence string \"{text}\"", nameof(text));

        var tokens = text.Split("/");

        return new Occurrence
        (
            int.Parse(tokens.First().Trim()),
            OccurrenceWindow.FromValue(tokens.Last().Trim())
        );
    }
}

public sealed class OccurrenceConverter : JsonConverter<Occurrence>
{
    public override Occurrence? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();

        return string.IsNullOrEmpty(text) ? null : Occurrence.Parse(text);
    }

    public override void Write(Utf8JsonWriter writer, Occurrence value, JsonSerializerOptions options)
        => writer.WriteStringValue($"{value.Times}/{value.Window.Value}");
}