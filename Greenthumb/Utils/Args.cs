using System.Data.SqlTypes;
using System.Runtime.CompilerServices;

namespace Greenthumb.Utils;

public static class Args
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseYear(string[] args, out int year)
    {
        year = 0;

        return args.Length > 0 && int.TryParse(args[0], out year);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseOutDir(string[] args, out string directory)
    {
        directory = string.Empty;

        if (args.Length <= 1)
            return false;

        directory = args[1];

        return true;
    }
}