namespace Greenthumb.Models;

public sealed class TaskLists : Dictionary<string, List<string>>
{
    public List<string> GetOrCreateTaskList(string name)
    {
        if (TryGetValue(name, out var list))
            return list;

        list = new List<string>();

        Add(name, list);

        return list;
    }
}

public sealed class Calendar : Dictionary<string, TaskLists>
{
    #region Proeprties
    public DateTime Epoch
    {
        get;
    }
    #endregion

    public Calendar(int year)
    {
        Epoch = new DateTime(year, 1, 1);

        var currentDate = Epoch;

        while (currentDate.Year == Epoch.Year)
        {
            Add(currentDate.ToString(Formats.CalendarDate), new TaskLists());

            currentDate = currentDate.AddDays(1);
        }
    }

    public TaskLists GetTaskLists(DateTime date)
        => this[date.ToString(Formats.CalendarDate)];
}