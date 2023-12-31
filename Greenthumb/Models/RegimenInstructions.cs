using System.Collections.Immutable;

namespace Greenthumb.Models;

public sealed class RegimenInstructions : Dictionary<Season, Dictionary<string, List<string>>>
{
    public RegimenInstructions()
        => Season.List.ToImmutableList().ForEach(s => Add(s, new Dictionary<string, List<string>>()));

    public List<string> GetOrCreateSeasonalInstructionList(Season season, string name)
    {
        if (this[season].TryGetValue(name, out var instructionList))
            return instructionList;

        instructionList = new List<string>();

        this[season].Add(name, instructionList);

        return instructionList;
    }
}