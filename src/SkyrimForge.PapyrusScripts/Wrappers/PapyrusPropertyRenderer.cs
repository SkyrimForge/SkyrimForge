using System;
using System.Collections.Generic;

namespace SkyrimForge.PapyrusScripts;

internal sealed class PapyrusPropertyRenderer(string scriptName, PapyrusTypes types, string? receiver, PapyrusAnnotations annotations, ICollection<string> methods,
    ICollection<OmittedMember> omittedMembers, IEnumerable<string> generatedNames)
{
    // GlobalVariable declares a property Value, whose setter would be SetValue, and a function SetValue.
    private readonly HashSet<string> claimedNames = new(generatedNames, StringComparer.OrdinalIgnoreCase);

    internal void Add(PapyrusProperty property)
    {
        if (receiver == null)
        {
            omittedMembers.Add(new OmittedMember(scriptName, property.Name, $"neither {scriptName} nor a script it extends is a form type with a C# type, so the property has no object to be read from", property.Line));
        }
        else if (types.GetValueType(property.Type) == null)
        {
            omittedMembers.Add(new OmittedMember(scriptName, property.Name, $"no C# type corresponds to the Papyrus type {property.Type}", property.Line));
        }
        else if (property.IsAuto)
        {
            AddAutoProperty(property);
        }
        else
        {
            AddFullProperty(property);
        }
    }

    private void AddAutoProperty(PapyrusProperty property)
    {
        string surfaceType = types.GetDispatchResultType(property.Type);
        string readExpression = $"Dispatcher.GetProperty<{surfaceType}>(self.Receiver, \"{property.Name}\"){types.GetNullForgivingOperator(property.Type)}";
        string head = CSharpFormatting.RenderSummary(property.Comment, "        ") + annotations.RenderPropertyObsolete(scriptName, property, types, receiver);

        // Only a scalar has one C# property for both directions. A form's setter accepts the interface while the getter returns the view.
        string writeCall = $"Dispatcher.SetProperty(self.Receiver, \"{property.Name}\", value)";
        if (property.IsWritable && PapyrusTypes.IsScalar(property.Type))
        {
            methods.Add($"{head}        public {surfaceType} {property.Name}\n        {{\n            get => {readExpression};\n            set => {writeCall};\n        }}");
            return;
        }

        methods.Add($"{head}        public {surfaceType} {property.Name} => {readExpression};");
        if (!property.IsWritable)
        {
            return;
        }
        AddFormOrArraySetter(property, writeCall);
    }

    private void AddFullProperty(PapyrusProperty property)
    {
        string surfaceType = types.GetDispatchResultType(property.Type);
        bool claimed = claimedNames.Contains(property.Name);
        if (property.IsReadable && !claimed)
        {
            claimedNames.Add(property.Name);
            methods.Add($"{CSharpFormatting.RenderSummary(property.Comment, "        ")}{annotations.RenderPropertyObsolete(scriptName, property, types, receiver)}"
                + $"        public Task<{surfaceType}> {property.Name} => Dispatcher.CallGetterAsync<{surfaceType}>(self.Receiver, \"{property.Name}\"){types.GetNullForgivingOperator(property.Type)};");
        }
        else if (property.IsReadable)
        {
            omittedMembers.Add(new OmittedMember(scriptName, property.Name, $"the script also declares a function named {property.Name}, which is generated instead", property.Line));
        }

        if (!property.IsWritable)
        {
            return;
        }
        string writeCall = $"Dispatcher.CallSetter(self.Receiver, \"{property.Name}\", value)";
        if (PapyrusTypes.IsScalar(property.Type))
        {
            AddSetterMethod(property, $"{surfaceType} value", writeCall);
            return;
        }
        AddFormOrArraySetter(property, writeCall);
    }

    private void AddFormOrArraySetter(PapyrusProperty property, string writeCall)
    {
        if (types.IsForm(property.Type))
        {
            AddSetterMethod(property, $"{types.GetReceiverType(property.Type)}? value", writeCall);
            return;
        }

        string element = PapyrusTypes.GetElementTypeName(property.Type);
        if (PapyrusTypes.IsScalar(element))
        {
            AddSetterMethod(property, $"IReadOnlyCollection<{PapyrusTypes.GetElementType(types.GetValueType(element)!)}>? value", writeCall);
            return;
        }

        string? constraint = types.GetInterfaceType(element);
        string entry = constraint == null ? types.GetValueType(element)! : "TForm";
        AddSetterMethod(property, $"IReadOnlyCollection<{entry}>? value", writeCall, constraint == null ? "" : "<TForm>",
            constraint == null ? "" : $" where TForm : {constraint}");
    }

    private void AddSetterMethod(PapyrusProperty property, string parameter, string writeCall, string typeParameters = "", string constraints = "")
    {
        string setterName = "Set" + property.Name;
        if (claimedNames.Contains(setterName))
        {
            omittedMembers.Add(new OmittedMember(scriptName, setterName, $"the script also declares a function named {setterName}, which is generated instead", property.Line));
            return;
        }
        claimedNames.Add(setterName);
        string timingRemark = property.IsAuto ? "The value is in the object's variable when this returns." : "This returns once the Papyrus virtual machine has accepted the write, which runs a frame or two later and reports nothing.";
        methods.Add(CSharpFormatting.RenderDocElement("summary", $"Sets {scriptName}.{property.Name}. {timingRemark}", "        /// ") + "\n"
            + $"        public void {setterName}{typeParameters}({parameter}){constraints} => {writeCall};");
    }
}
