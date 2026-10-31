// CommonLib constructs IFunctionArguments only through a variadic template.
// A form is bound once the function is known, since its class depends on the declared parameter.
#include "Shared.h"
#include <cstdint>
#include <cstring>
#include <deque>
#include <vector>
namespace
{
	enum class PendingBinding
	{
		Nothing,
		Form,
		FormArray
	};
	struct Argument
	{
		RE::BSScript::Variable value;
		PendingBinding pendingBinding = PendingBinding::Nothing;
		std::vector<RE::TESForm*> forms;
	};
	// A deque, since managed code receives the address of each Variable.
	class SuppliedArguments : public RE::BSScript::IFunctionArguments
	{
	public:
		bool operator()(RE::BSScrapArray<RE::BSScript::Variable>& a_dst) const override
		{
			a_dst.resize(static_cast<std::uint32_t>(arguments.size()));
			for (std::size_t i = 0; i < arguments.size(); ++i)
			{
				a_dst[i] = arguments[i].value;
			}
			return true;
		}
		std::deque<Argument> arguments;
	};
	SuppliedArguments* AsArgumentList(RE::BSScript::IFunctionArguments* args)
	{
		return static_cast<SuppliedArguments*>(args);
	}
	// Zero is the type ID of Form, so whether the class resolved is a separate result.
	// Separate from its __try callers, since GetName returns a BSFixedString, which needs destruction.
	void ReadDeclaredClass(RE::BSScript::Internal::VirtualMachine* machine,
		const RE::BSScript::TypeInfo& declaredType, std::int32_t* classId, bool* resolved)
	{
		if (!declaredType.IsObject() && !declaredType.IsObjectArray())
		{
			return;
		}
		// An object's raw type is its class pointer, plus one for an array. Raw values below kArraysEnd are the plain types.
		std::size_t rawType = static_cast<std::size_t>(declaredType.GetRawType());
		if (rawType < static_cast<std::size_t>(RE::BSScript::TypeInfo::RawType::kArraysEnd))
		{
			return;
		}
		RE::BSScript::ObjectTypeInfo* scriptClass =
			reinterpret_cast<RE::BSScript::ObjectTypeInfo*>(rawType & ~static_cast<std::size_t>(1));
		RE::VMTypeID typeId = 0;
		if (scriptClass != nullptr && machine->GetTypeIDForScriptObject(scriptClass->GetName(), typeId))
		{
			*classId = static_cast<std::int32_t>(typeId);
			*resolved = true;
		}
	}
	// Separate from TryReadReturnedClass, since GetReturnType returns a TypeInfo by value. A class that does not resolve leaves zero, which is Form.
	void ReadReturnedClass(RE::BSScript::Internal::VirtualMachine* machine,
		RE::BSScript::IFunction* function, std::int32_t* classId)
	{
		bool resolved = false;
		ReadDeclaredClass(machine, function->GetReturnType(), classId, &resolved);
	}
	// The read treats a type ID as a pointer, so it runs under __try. 1 is EXCEPTION_EXECUTE_HANDLER.
	bool TryReadReturnedClass(RE::BSScript::Internal::VirtualMachine* machine,
		RE::BSScript::IFunction* function, std::int32_t* classId)
	{
		__try
		{
			ReadReturnedClass(machine, function, classId);
			return true;
		}
		__except (1)
		{
			return false;
		}
	}
	// An unlinked type is skipped, since its function tables are not yet valid.
	RE::BSScript::IFunction* FindFunction(RE::BSScript::ObjectTypeInfo* objectType, const char* name)
	{
		for (RE::BSScript::ObjectTypeInfo* type = objectType; type != nullptr; type = type->GetParent())
		{
			if (!type->IsLinked())
			{
				continue;
			}
			const RE::BSScript::ObjectTypeInfo::GlobalFuncInfo* globals = type->GetGlobalFuncIter();
			for (std::uint32_t i = 0; i < type->GetNumGlobalFuncs(); ++i)
			{
				RE::BSScript::IFunction* function = globals[i].func.get();
				if (function != nullptr && _stricmp(function->GetName().c_str(), name) == 0)
				{
					return function;
				}
			}
			const RE::BSScript::ObjectTypeInfo::MemberFuncInfo* members = type->GetMemberFuncIter();
			for (std::uint32_t i = 0; i < type->GetNumMemberFuncs(); ++i)
			{
				RE::BSScript::IFunction* function = members[i].func.get();
				if (function != nullptr && _stricmp(function->GetName().c_str(), name) == 0)
				{
					return function;
				}
			}
		}
		return nullptr;
	}
}
bool SF_Papyrus_DeclaredClassSafely(RE::BSScript::Internal::VirtualMachine* machine,
	const RE::BSScript::TypeInfo& declaredType, std::int32_t* classId, bool* resolved)
{
	__try
	{
		ReadDeclaredClass(machine, declaredType, classId, resolved);
		return true;
	}
	__except (1)
	{
		return false;
	}
}
// The dispatch is asynchronous and the VM does not delete the list, so ManagedCallback's destructor deletes it.
extern "C" __declspec(dllexport) RE::BSScript::IFunctionArguments* SF_Papyrus_BeginArguments()
{
	return new SuppliedArguments();
}
extern "C" __declspec(dllexport) void SF_Papyrus_DeleteArguments(RE::BSScript::IFunctionArguments* args)
{
	delete args;
}
extern "C" __declspec(dllexport) RE::BSScript::Variable* SF_Papyrus_AppendArgument(
	RE::BSScript::IFunctionArguments* args)
{
	if (args == nullptr)
	{
		return nullptr;
	}
	SuppliedArguments* argumentList = AsArgumentList(args);
	argumentList->arguments.emplace_back();
	return &argumentList->arguments.back().value;
}
extern "C" __declspec(dllexport) RE::BSScript::Array* SF_Papyrus_AppendArray(
	RE::BSScript::IFunctionArguments* args, RE::BSScript::TypeInfo::RawType rawType, std::uint32_t size)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (args == nullptr || machine == nullptr)
	{
		return nullptr;
	}
	RE::BSTSmartPointer<RE::BSScript::Array> array;
	RE::BSScript::TypeInfo type(rawType);
	if (!machine->CreateArray(type, size, array) || !array)
	{
		return nullptr;
	}
	SuppliedArguments* argumentList = AsArgumentList(args);
	argumentList->arguments.emplace_back();
	// SetArray gives the Variable its own reference, so the returned array outlives this call.
	argumentList->arguments.back().value.SetArray(array);
	return array.get();
}
// The Variable stays None until SF_Papyrus_BindArgumentsFor binds the form to the declared class.
extern "C" __declspec(dllexport) void SF_Papyrus_AppendForm(RE::BSScript::IFunctionArguments* args,
	RE::TESForm* form)
{
	if (args != nullptr)
	{
		SuppliedArguments* argumentList = AsArgumentList(args);
		argumentList->arguments.emplace_back();
		argumentList->arguments.back().pendingBinding = PendingBinding::Form;
		argumentList->arguments.back().forms.push_back(form);
	}
}
// SF_Papyrus_BindArgumentsFor allocates the array, since CreateArray requires the declared element class.
extern "C" __declspec(dllexport) void SF_Papyrus_AppendFormArray(RE::BSScript::IFunctionArguments* args,
	RE::TESForm* const* forms, std::int32_t count)
{
	if (args == nullptr || count < 0)
	{
		return;
	}
	SuppliedArguments* argumentList = AsArgumentList(args);
	argumentList->arguments.emplace_back();
	argumentList->arguments.back().pendingBinding = PendingBinding::FormArray;
	if (forms != nullptr && count > 0)
	{
		argumentList->arguments.back().forms.assign(forms, forms + count);
	}
}
// An object bound to any other class makes the VM accept the dispatch, skip the function, and return None.
// The function is looked up first, since dispatching an unresolvable name crashes the VM.
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_BindArgumentsFor(
	RE::BSScript::IFunctionArguments* args, const char* className, const char* functionName,
	std::int32_t* declaredCount, std::int32_t* returnObjectType)
{
	if (declaredCount != nullptr)
	{
		*declaredCount = 0;
	}
	if (returnObjectType != nullptr)
	{
		*returnObjectType = 0;
	}
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || args == nullptr || className == nullptr || functionName == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<RE::BSScript::ObjectTypeInfo> type;
	if (!machine->GetScriptObjectType(className, type) || !type)
	{
		return 2;
	}
	RE::BSScript::IFunction* function = FindFunction(type.get(), functionName);
	if (function == nullptr)
	{
		return 3;
	}
	SuppliedArguments* argumentList = AsArgumentList(args);
	if (declaredCount != nullptr)
	{
		*declaredCount = static_cast<std::int32_t>(function->GetParamCount());
	}
	if (returnObjectType != nullptr && !TryReadReturnedClass(machine, function, returnObjectType))
	{
		*returnObjectType = -1;
	}
	if (function->GetParamCount() != argumentList->arguments.size())
	{
		return 4;
	}
	for (std::uint32_t i = 0; i < function->GetParamCount(); ++i)
	{
		RE::BSFixedString parameterName;
		RE::BSScript::TypeInfo declaredType;
		function->GetParam(i, parameterName, declaredType);
		Argument& argument = argumentList->arguments[i];
		if (argument.pendingBinding == PendingBinding::Nothing)
		{
			// The VM accepts a scalar for an object parameter and skips the function. Number types are not checked, since the VM may coerce between them.
			if (declaredType.IsObject()
				&& (argument.value.IsInt() || argument.value.IsFloat() || argument.value.IsBool()
					|| argument.value.IsString()))
			{
				return 7;
			}
			continue;
		}
		std::int32_t id = 0;
		bool resolved = false;
		if (!SF_Papyrus_DeclaredClassSafely(machine, declaredType, &id, &resolved))
		{
			return 8;
		}
		if (argument.pendingBinding == PendingBinding::Form)
		{
			if (!declaredType.IsObject())
			{
				return 6;
			}
			if (!resolved)
			{
				return 5;
			}
			RE::BSScript::PackHandle(&argument.value, argument.forms.front(), static_cast<RE::VMTypeID>(id));
			continue;
		}
		if (!declaredType.IsObjectArray())
		{
			return 9;
		}
		if (!resolved)
		{
			return 5;
		}
		RE::BSTSmartPointer<RE::BSScript::Array> array;
		std::uint32_t formCount = static_cast<std::uint32_t>(argument.forms.size());
		if (!machine->CreateArray(declaredType, formCount, array) || !array || array->size() != formCount)
		{
			return 10;
		}
		for (std::uint32_t i = 0; i < formCount; ++i)
		{
			RE::BSScript::PackHandle(&(*array)[i], argument.forms[i], static_cast<RE::VMTypeID>(id));
		}
		argument.value.SetArray(array);
	}
	return 0;
}
extern "C" __declspec(dllexport) void SF_Papyrus_SetVariableForm(RE::BSScript::Variable* variable,
	RE::TESForm* form, std::int32_t typeId)
{
	if (variable != nullptr)
	{
		RE::BSScript::PackHandle(variable, form, static_cast<RE::VMTypeID>(typeId));
	}
}
