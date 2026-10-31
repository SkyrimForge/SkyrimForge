using System;

namespace SkyrimForge.Game;

internal sealed class ConditionGlobal
{
    private readonly GlobalVariable global;
    private readonly Func<bool> condition;

    private bool reported;

    internal ConditionGlobal(Func<bool> condition)
    {
        global = RuntimeConditions.CreateGlobal();
        RuntimeForms.SetTemporary(global, true);
        this.condition = condition;
    }

    internal bool Result => global.Value != 0f;

    internal TESConditionItem CreateConditionItem() => RuntimeConditions.Create(FUNCTION_DATA.FunctionID.kGetGlobalValue, global.Address);

    internal void Write(Action<Exception> report)
    {
        bool result = Evaluate(condition, ex =>
        {
            if (!reported)
            {
                reported = true;
                report(ex);
            }
        });
        GlobalVariable writtenGlobal = global;
        writtenGlobal.Value = result ? 1f : 0f;
    }

    internal static bool Evaluate(Func<bool> condition, Action<Exception> report)
    {
        try
        {
            return condition();
        }
        catch (Exception ex)
        {
            report(ex);
            return false;
        }
    }
}
