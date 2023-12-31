using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CommandLine;
using CommandLine.Text;
using Greenthumb.Models;
using Greenthumb.Utils;

using Calendar = Greenthumb.Models.Calendar;

namespace Greenthumb;

public sealed class Options
{
    #region Properties
    [Option('y', "year", Required = true, HelpText = "epoch year for which the calendar will be generated")]
    public int Year
    {
        get;
        set;
    }

    [Option('o', "output", Required = true, HelpText = "output folder where the generated calendar files will be created")]
    public string OutDirectory
    {
        get;
        set;
    }
    #endregion
}

public static class Program
{
    private static ImmutableList<Plant> LoadPlants()
    {
        try
        {
            return JsonSerializer.Deserialize<Plant[]>(File.ReadAllBytes("Files/plants.json"))?.ToImmutableList() ?? ImmutableList<Plant>.Empty;
        }
        catch (Exception e)
        {
            Log.Error($"error occurred while loading plant details from \"plants.json\", message: {e.Message}");
        }

        return ImmutableList<Plant>.Empty;
    }

    private static bool InitializeOutDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            return true;
        }
        catch (Exception e)
        {
            Log.Error($"error occurred while creating out directory, message: {e.Message}");
        }

        return false;
    }

    private static void GenerateSoilChangeTasks(Plant plant, Calendar calendar)
    {
        var window          = plant.Regimen.SoilChange.Every.Window;
        var occurrence      = plant.Regimen.SoilChange.Every.Times;
        var soilChangeDates = ImmutableList<DateTime>.Empty;

        window.When(OccurrenceWindow.Week)
              .Then(() =>
              {
                  soilChangeDates = DateIterator.Until(plant.SoilChangedAt, (d) => d.AddDays(7 * occurrence), (d) => d.Year <= calendar.Epoch.Year)
                                                .ToImmutableList();
              }).When(OccurrenceWindow.Month)
              .Then(() =>
              {
                  soilChangeDates = DateIterator.Until(plant.SoilChangedAt, (d) => d.AddMonths(1 * occurrence), (d) => d.Year <= calendar.Epoch.Year)
                                                .ToImmutableList();
              }).When(OccurrenceWindow.Year)
              .Then(() =>
              {
                  soilChangeDates = new[] { plant.Regimen.SoilChange.Season.FirstDayOf(plant.SoilChangedAt.AddYears(occurrence)) }.ToImmutableList();
              }).Default(() => throw new ArgumentOutOfRangeException($"unsupported {nameof(OccurrenceWindow)} value \"{window.Value}\""));

        soilChangeDates.Where(d => Season.IsInSeason(d, plant.Regimen.SoilChange.Season) && d.Year == calendar.Epoch.Year)
                       .ToImmutableList()
                       .ForEach(d =>
                       {
                           var taskList = calendar.GetTaskLists(d).GetOrCreateTaskList("Mullanvaihto");

                           taskList.Add(taskList.Any() ? plant.Name.ToLower() : plant.Name);
                       });
    }

    private static void GenerateFertilizationTasks(Plant plant, Calendar calendar)
    {
        if (plant.Regimen.Fertilization == null)
            return;

        foreach (var fertilizationTask in plant.Regimen.Fertilization)
        {
            var fertilizationDates = fertilizationTask.Occurrence.DatesOver(calendar.Epoch);

            if (!fertilizationTask.OverTheYear)
                fertilizationDates = fertilizationDates.Where(d => fertilizationTask.Seasons!.Contains(Season.Of(d))).ToImmutableList();

            fertilizationDates.ForEach(d =>
            {
                var taskList = calendar.GetTaskLists(d).GetOrCreateTaskList("Lannoitus");

                taskList.Add(taskList.Any() ? plant.Name.ToLower() : plant.Name);
            });
        }
    }

    private static void GenerateWateringTasks(Plant plant, Calendar calendar)
    {
        foreach (var wateringTask in plant.Regimen.Watering)
        {
            var wateringDates = wateringTask.Occurrence.DatesOver(calendar.Epoch);

            if (!wateringTask.OverTheYear)
                wateringDates = wateringDates.Where(d => wateringTask.Seasons!.Contains(Season.Of(d))).ToImmutableList();

            wateringDates.ForEach(d =>
            {
                var taskList = calendar.GetTaskLists(d).GetOrCreateTaskList("Kastelu");

                taskList.Add(taskList.Any() ? plant.Name.ToLower() : plant.Name);
            });
        }
    }

    private static void GenerateSoilChangeInstructions(Plant plant, RegimenInstructions instructions)
    {
        var taskList = instructions.GetOrCreateSeasonalInstructionList(plant.Regimen.SoilChange.Season, "Mullanvaihto");

        taskList.Add($"{plant.Name}: {plant.Regimen.SoilChange.SoilType}");
    }

    private static void GenerateFertilizationInstructions(Plant plant, RegimenInstructions instructions)
    {
        if (plant.Regimen.Fertilization == null)
            return;

        foreach (var fertilizationTask in plant.Regimen.Fertilization)
        {
            var seasons = fertilizationTask.OverTheYear ? Season.List : fertilizationTask.Seasons!;

            foreach (var season in seasons)
            {
                var taskList = instructions.GetOrCreateSeasonalInstructionList(season, "Lannoitus");

                taskList.Add($"{plant.Name}: {fertilizationTask.FertilizerType}");
            }
        }
    }

    private static void GenerateWateringInstructions(Plant plant, RegimenInstructions instructions)
    {
        foreach (var wateringTask in plant.Regimen.Watering)
        {
            var seasons = wateringTask.OverTheYear ? Season.List : wateringTask.Seasons!;

            foreach (var season in seasons)
            {
                var taskList = instructions.GetOrCreateSeasonalInstructionList(season, "Kastelu");

                taskList.Add($"{plant.Name}: {wateringTask.Condition}{(!string.IsNullOrEmpty(wateringTask.Notes) ? $", {wateringTask.Notes}" : string.Empty)}");
            }
        }
    }

    private static void GenerateCalendarHtml(Calendar calendar, string directory)
    {
        var html = new StringBuilder();

        void H(string t) => html.Append(t);

        var months = calendar.Select(t => new
        {
            Date      = t.Key,
            TaskLists = t.Value,
            Month     = DateTime.ParseExact(t.Key, Formats.CalendarDate, CultureInfo.InvariantCulture, DateTimeStyles.None).Month - 1,
        }).GroupBy(v => v.Month);

        var finnishMonthNameLookup = new[]
        {
            "Tammikuu",
            "Helmikuu",
            "Maaliskuu",
            "Huhtikuu",
            "Toukokuu",
            "Kesäkuu",
            "Heinäkuu",
            "Elokuu",
            "Syyskuu",
            "Lokakuu",
            "Marraskuu",
            "Joulukuu",
        };

        foreach (var month in months)
        {
            H("<div>");
            H($"<h2 class=\"centered\">{finnishMonthNameLookup[month.Key]}</h2>");
            H("<table>");
            H("<thead>");
            H("<tr>");
            H("<th class=\"date_column\">Päivä</th>");
            H("<th class=\"task_list_column\">Tehtävälista</th>");
            H("<th class=\"completed_column\">Hoidettu</th>");
            H("<th class=\"notes_column\">Huomiot</th>");
            H("</tr>");
            H("</thead>");

            foreach (var taskLists in month)
            {
                var taskDate = DateTime.ParseExact(taskLists.Date, Formats.CalendarDate, CultureInfo.InvariantCulture, DateTimeStyles.None);

                H("<tr>");
                H($"<td class=\"date_column\">{taskDate.Day}.{taskDate.Month}</td>");
                H("<td class=\"task_list_column\">");

                foreach (var taskList in taskLists.TaskLists)
                {
                    H($"<h3>{taskList.Key}</h3>");
                    H("<ul>");
                    H("<li>");

                    H(string.Join(',', taskList.Value).Replace(",", ", "));

                    H("</li>");
                    H("</ul>");
                }

                H("<td class=\"completed_column\"></td>");
                H("<td class=\"notes_column\"></td>");
                H("</td>");
                H("</tr>");
            }

            H("</table>");
            H("</div>");

            File.WriteAllText(
                $"{directory}/{finnishMonthNameLookup[month.Key].ToLower()}-{calendar.Epoch.Year}.html",
                File.ReadAllText("Files/template.html").Replace("GENERATED_HTML", html.ToString())
            );

            html.Clear();
        }
    }

    private static void GenerateInstructionsHtml(RegimenInstructions instructions, string directory)
    {
        var html = new StringBuilder();

        void H(string t) => html.Append(t);

        var finnishSeasonNamesLookup = new Dictionary<Season, string>()
        {
            { Season.Spring, "Kevät" },
            { Season.Summer, "Kesä" },
            { Season.Fall, "Syksy" },
            { Season.Winter, "Talvi" },
        };

        foreach (var seasonalTaskLists in instructions)
        {
            H("<div>");
            H($"<h1 class=\"centered\">Hoito-ohjeet {finnishSeasonNamesLookup[seasonalTaskLists.Key]}</h2>");

            foreach (var taskList in seasonalTaskLists.Value)
            {
                H($"<h2>{taskList.Key}</h2>");
                H($"<ul>");

                foreach (var task in taskList.Value)
                    H($"<li class=\"spaced-li\">{task}</li>");

                H($"</ul>");
            }

            H("</table>");
            H("</div>");

            File.WriteAllText(
                $"{directory}/hoito-ohjeet-{finnishSeasonNamesLookup[seasonalTaskLists.Key].ToLower()}.html",
                File.ReadAllText("Files/template.html").Replace("GENERATED_HTML", html.ToString())
            );

            html.Clear();
        }
    }

    public static void Main(string[] args)
    {
        var results = Parser.Default.ParseArguments<Options>(args);

        results.WithParsed(options =>
        {
            if (!InitializeOutDirectory(options.OutDirectory))
            {
                Log.Error("could not initialize out directory, ensure the format is correct path");

                return;
            }

            Log.Info($"generating calendar for year {options.Year}...");

            var calendar     = new Calendar(options.Year);
            var instructions = new RegimenInstructions();
            var plants       = LoadPlants();

            foreach (var plant in plants)
            {
                Log.Info($"generating plant regime calendar and instructions for plant \"{plant.Name}\"");

                GenerateSoilChangeTasks(plant, calendar);
                GenerateFertilizationTasks(plant, calendar);
                GenerateWateringTasks(plant, calendar);

                GenerateSoilChangeInstructions(plant, instructions);
                GenerateFertilizationInstructions(plant, instructions);
                GenerateWateringInstructions(plant, instructions);
            }

            GenerateCalendarHtml(calendar, options.OutDirectory);
            GenerateInstructionsHtml(instructions, options.OutDirectory);
        }).WithNotParsed(errors =>
        {
            if (errors.Any(e => e.Tag != ErrorType.HelpRequestedError))
                Log.Error("failed to parse command line arguments, run the application with --help to see list of parameters");
        });
    }
}