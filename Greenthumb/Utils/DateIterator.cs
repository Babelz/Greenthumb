using System.Runtime.CompilerServices;

namespace Greenthumb.Utils;

public static class DateIterator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<DateTime> Until(DateTime from, Func<DateTime, DateTime> next, Func<DateTime, bool> until)
    {
        var current = from;

        do
        {
            yield return current;

            current = next(current);
        } while (until(current));
    }
}