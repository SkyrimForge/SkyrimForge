using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.PapyrusScripts;

internal static class PapyrusContestedNames
{
    /// <summary>
    /// A script and an ancestor can declare one function name as different natives, such as Form.GetWeight and ActorBase.GetWeight.
    /// A receiver of the derived type satisfies both extension blocks, so both become statics of their own classes.
    /// </summary>
    internal static Dictionary<string, HashSet<string>> Find(IReadOnlyDictionary<string, PapyrusScript> scripts, IReadOnlyDictionary<string, string?> parents)
    {
        var contestedNames = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (PapyrusScript script in scripts.Values)
        {
            var ownNames = new HashSet<string>(script.Functions.Where(function => !function.IsGlobal).Select(function => function.Name),
                StringComparer.OrdinalIgnoreCase);
            var visitedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { script.Name };
            string? parent = script.Parent;
            while (parent != null && visitedNames.Add(parent))
            {
                if (scripts.TryGetValue(parent, out PapyrusScript? parentScript))
                {
                    foreach (PapyrusFunction inheritedFunction in parentScript.Functions.Where(function =>
                        !function.IsGlobal && ownNames.Contains(function.Name)))
                    {
                        Mark(contestedNames, script.Name, inheritedFunction.Name);
                        Mark(contestedNames, parentScript.Name, inheritedFunction.Name);
                    }
                }
                parent = parents.TryGetValue(parent, out string? grandparent) ? grandparent : null;
            }
        }
        return contestedNames;
    }

    private static void Mark(Dictionary<string, HashSet<string>> contestedNames, string script, string function)
    {
        if (!contestedNames.TryGetValue(script, out HashSet<string>? names))
        {
            names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            contestedNames[script] = names;
        }
        names.Add(function);
    }
}
