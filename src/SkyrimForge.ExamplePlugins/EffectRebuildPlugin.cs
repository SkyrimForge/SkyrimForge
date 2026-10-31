using SkyrimForge.Game;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>F3 reads the effects of Flames, clears the list, and adds each effect back with the same values.</summary>
/// <remarks>A faulty rebuild leaves the spell unusable until the game restarts.</remarks>
public sealed class EffectRebuildPlugin(IGameEvents events, IUtility utility, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF3, ModifierKeys.None).Subscribe(_ => RebuildAsync());
        log.Write("F3 rebuilds the effects of Flames.");
    }

    private async Task RebuildAsync()
    {
        await utility.WaitForGameThreadAsync();
        Spell spell = ExampleForms.Flames.Form;

        List<EffectValues> originalValues = [.. spell.Effects.Get().Select(EffectValues.FromEffect)];
        log.Write($"Flames has {originalValues.Count} effects: {string.Join(", ", originalValues)}.");

        spell.Effects.Clear();
        int countAfterClear = spell.Effects.Count;

        foreach (EffectValues entry in originalValues)
        {
            entry.Add(spell);
        }

        List<EffectValues> rebuiltValues = [.. spell.Effects.Get().Select(EffectValues.FromEffect)];
        log.Write($"After clearing, Flames had {countAfterClear} effects. After rebuilding, it has {rebuiltValues.Count}: {string.Join(", ", rebuiltValues)}.");
    }

    private sealed record EffectValues(MagicEffect? BaseEffect, float Magnitude, uint Area, uint Duration, float Cost)
    {
        public static EffectValues FromEffect(Effect effect)
        {
            Effect.EffectItem item = effect.GetEffectItem();
            return new EffectValues(effect.BaseEffect, item.Magnitude, item.Area, item.Duration, effect.Cost);
        }

        public void Add(Spell spell)
        {
            Effect effect = Effect.New();
            effect.BaseEffect = BaseEffect;
            Effect.EffectItem item = effect.GetEffectItem();
            item.Magnitude = Magnitude;
            item.Area = Area;
            item.Duration = Duration;
            effect.Cost = Cost;
            spell.Effects.Add(effect);
        }

        public override string ToString()
        {
            string baseEffect = BaseEffect is { } effect ? $"0x{effect.FormId}" : "none";
            return $"[base {baseEffect} magnitude {Magnitude} area {Area} duration {Duration} cost {Cost}]";
        }
    }
}
