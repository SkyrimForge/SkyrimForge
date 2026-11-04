namespace SkyrimForge.Game;

internal static class RuntimeConditions
{
    internal static GlobalVariable CreateGlobal()
    {
        GlobalVariable global = RuntimeForms.Create(FormType.Global).AsGlobalVariable()!.Value;
        global.GlobalVariableType = GlobalVariable.Type.kFloat;
        global.Value = 0f;
        return global;
    }

    internal static TESConditionItem Create(FUNCTION_DATA.FunctionID function, nint parameter, float value = 1f)
    {
        TESConditionItem item = TESConditionItem.New();
        item.Next = null;
        CONDITION_ITEM_DATA data = item.Data;
        FUNCTION_DATA functionData = data.FunctionData;
        functionData.Function = function;
        functionData.Params.Set(0, parameter);
        CONDITION_ITEM_DATA.GlobalOrFloat comparison = data.ComparisonValue;
        comparison.F = value;
        CONDITION_ITEM_DATA.Flags flags = data.GetFlags();
        flags.OpCode = CONDITION_ITEM_DATA.OpCode.kEqualTo;
        data.Object = CONDITIONITEMOBJECT.kSelf;
        return item;
    }

    // The condition is joined to the rest with "and," which the game evaluates after every "or," so it applies to the info as a whole.
    internal static TESConditionItem PrependIsReference(TopicInfo info, nint reference, float value)
    {
        TESConditionItem condition = Create(FUNCTION_DATA.FunctionID.kGetIsReference, reference, value);
        TESCondition conditions = info.ObjConditions;
        condition.Next = conditions.Head;
        conditions.Head = condition;
        return condition;
    }
}
