#include <RE/Skyrim.h>

#include <cstdint>
#include <mutex>
#include <string>
#include <string_view>
#include <unordered_map>

namespace
{
	// Runs on the script's thread. Returns uint8_t because bool's width is not part of the ABI.
	using CallHandler = std::uint8_t (*)(std::int32_t, RE::BSScript::Variable*, const RE::BSScript::StackFrame*, RE::BSScript::Variable*, RE::VMStackID);
	CallHandler g_callHandler = nullptr;

	// An object's raw type is its class pointer, plus one for an array of objects.
	RE::BSScript::TypeInfo::RawType ToRawType(RE::BSScript::TypeInfo::RawType declaredRawType, std::int32_t objectType)
	{
		if (declaredRawType != RE::BSScript::TypeInfo::RawType::kObject && declaredRawType != RE::BSScript::TypeInfo::RawType::kObjectArray)
		{
			return declaredRawType;
		}

		std::size_t rawType = static_cast<std::size_t>(
			RE::BSScript::GetRawTypeFromVMType(static_cast<RE::VMTypeID>(objectType)));

		// A value below kArraysEnd means the class did not resolve, and setting the bit on it produces kObject.
		if (declaredRawType == RE::BSScript::TypeInfo::RawType::kObjectArray && rawType >= static_cast<std::size_t>(RE::BSScript::TypeInfo::RawType::kArraysEnd))
		{
			rawType |= std::size_t{ 1 };
		}
		return static_cast<RE::BSScript::TypeInfo::RawType>(rawType);
	}

	// RegisterFunction is a variadic template and cannot take a signature known only at run time.
	class ManagedFunction : public RE::BSScript::NF_util::NativeFunctionBase
	{
	public:
		ManagedFunction(std::string_view a_function, std::string_view a_class, std::uint16_t a_parameterCount,
			const RE::BSScript::TypeInfo::RawType* a_parameterRawTypes, const std::int32_t* a_parameterObjectTypes,
			RE::BSScript::TypeInfo::RawType a_returnRawType, std::int32_t a_returnObjectType, std::int32_t a_token, bool a_isStatic,
			bool a_isLatent) :
			NativeFunctionBase(a_function, a_class, a_isStatic, a_parameterCount),
			token(a_token)
		{
			_retType = ToRawType(a_returnRawType, a_returnObjectType);
			for (std::uint16_t i = 0; i < a_parameterCount; ++i)
			{
				_descTable.entries[i].second.SetType(ToRawType(a_parameterRawTypes[i], a_parameterObjectTypes[i]));
			}
			_isLatent = a_isLatent;
		}

		bool HasStub() const override
		{
			return true;
		}

		bool MarshallAndDispatch(RE::BSScript::Variable& a_baseValue, RE::BSScript::Internal::VirtualMachine&,
			RE::VMStackID a_stackID, RE::BSScript::Variable& a_resultValue,
			const RE::BSScript::StackFrame& a_frame) const override
		{
			if (g_callHandler == nullptr)
			{
				return false;
			}
			const bool handled = g_callHandler(token, &a_resultValue, &a_frame, &a_baseValue, a_stackID) != 0;
			if (!_isLatent)
			{
				return handled;
			}

			// For a latent call the VM reads the result as whether the work started. The value arrives through ReturnFromLatent.
			a_resultValue.SetBool(handled);
			return true;
		}

	private:
		std::int32_t token;
	};

	// Each entry is a reference, since linking a script that declares the function differently releases the VM's reference.
	std::mutex g_functionsLock;
	std::unordered_map<std::int32_t, RE::BSTSmartPointer<ManagedFunction>> g_functions;

	RE::BSScript::IFunction* FindFunction(RE::BSScript::ObjectTypeInfo& type, const RE::BSFixedString& name)
	{
		for (std::uint32_t i = 0; i < type.GetNumGlobalFuncs(); ++i)
		{
			if (type.GetGlobalFuncIter()[i].func && type.GetGlobalFuncIter()[i].func->GetName() == name)
			{
				return type.GetGlobalFuncIter()[i].func.get();
			}
		}
		for (std::uint32_t i = 0; i < type.GetNumMemberFuncs(); ++i)
		{
			if (type.GetMemberFuncIter()[i].func && type.GetMemberFuncIter()[i].func->GetName() == name)
			{
				return type.GetMemberFuncIter()[i].func.get();
			}
		}
		return nullptr;
	}

	std::string Describe(const RE::BSScript::IFunction& function, bool withParameterNames)
	{
		std::string text;
		const RE::BSScript::TypeInfo returnType = function.GetReturnType();
		if (returnType.GetRawType() != RE::BSScript::TypeInfo::RawType::kNone)
		{
			text = returnType.TypeAsString() + " ";
		}
		text += "Function ";
		text += function.GetName().c_str();
		text += "(";
		for (std::uint32_t i = 0; i < function.GetParamCount(); ++i)
		{
			RE::BSFixedString name;
			RE::BSScript::TypeInfo type;
			function.GetParam(i, name, type);
			text += (i > 0 ? ", " : "") + type.TypeAsString();
			if (withParameterNames)
			{
				text += " ";
				text += name.c_str();
			}
		}
		text += function.GetIsStatic() ? ") global native" : ") native";
		return text;
	}

	void Copy(const std::string& text, char* buffer, std::int32_t bufferSize)
	{
		if (buffer != nullptr && bufferSize > 0)
		{
			strncpy_s(buffer, static_cast<std::size_t>(bufferSize), text.c_str(), _TRUNCATE);
		}
	}
}

extern "C" __declspec(dllexport) void SF_Papyrus_SetCallback(
	std::uint8_t (*handler)(std::int32_t, RE::BSScript::Variable*, const RE::BSScript::StackFrame*, RE::BSScript::Variable*, RE::VMStackID))
{
	g_callHandler = handler;
}

extern "C" __declspec(dllexport) bool SF_Papyrus_ReturnLatent(RE::VMStackID stackId, const RE::BSScript::Variable* value)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || value == nullptr)
	{
		return false;
	}
	machine->ReturnFromLatent(stackId, *value);
	return true;
}

extern "C" __declspec(dllexport) RE::BSScript::Array* SF_Papyrus_SetVariableArray(
	RE::BSScript::Variable* variable, RE::BSScript::TypeInfo::RawType rawType, std::int32_t objectType, std::uint32_t size)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (variable == nullptr || machine == nullptr)
	{
		return nullptr;
	}

	RE::BSTSmartPointer<RE::BSScript::Array> array;
	RE::BSScript::TypeInfo type(ToRawType(rawType, objectType));
	if (!machine->CreateArray(type, size, array) || !array)
	{
		return nullptr;
	}
	// SetArray gives the Variable its own reference, so the returned array outlives this call.
	variable->SetArray(array);
	return array.get();
}

extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_BindFunction(const char* className, const char* functionName,
	const RE::BSScript::TypeInfo::RawType* parameterRawTypes, const std::int32_t* parameterObjectTypes, std::int32_t parameterCount,
	RE::BSScript::TypeInfo::RawType returnRawType, std::int32_t returnObjectType, std::int32_t token, bool isStatic,
	bool isLatent)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr)
	{
		return 1;
	}

	// The game's limit is 11 parameters.
	if (className == nullptr || functionName == nullptr || parameterCount < 0
		|| parameterCount > 11
		|| (parameterCount > 0 && (parameterRawTypes == nullptr || parameterObjectTypes == nullptr)))
	{
		return 2;
	}

	// BindNativeMethod would replace a function already bound to a linked script's declaration.
	RE::BSTSmartPointer<RE::BSScript::ObjectTypeInfo> type;
	if (machine->GetScriptObjectTypeNoLoad1(RE::BSFixedString(className), type) && type && type->IsLinked()
		&& dynamic_cast<ManagedFunction*>(FindFunction(*type, RE::BSFixedString(functionName))) != nullptr)
	{
		return 4;
	}

	// Stored even if the VM rejects the function, so SF_Papyrus_GetBinding can describe it.
	ManagedFunction* managedFunction = new ManagedFunction(functionName, className,
		static_cast<std::uint16_t>(parameterCount), parameterRawTypes, parameterObjectTypes, returnRawType,
		returnObjectType, token, isStatic, isLatent);
	{
		std::scoped_lock lock(g_functionsLock);
		g_functions[token] = RE::BSTSmartPointer<ManagedFunction>(managedFunction);
	}
	return machine->BindNativeMethod(managedFunction) ? 0 : 3;
}

extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_GetBinding(const char* className, const char* functionName, std::int32_t token,
	char* scriptDeclaration, char* registration, std::int32_t bufferSize)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<ManagedFunction> registered;
	{
		std::scoped_lock lock(g_functionsLock);
		if (auto entry = g_functions.find(token); entry != g_functions.end())
		{
			registered = entry->second;
		}
	}

	RE::BSTSmartPointer<RE::BSScript::ObjectTypeInfo> type;
	if (!machine->GetScriptObjectType1(RE::BSFixedString(className), type) || !type)
	{
		return 2;
	}
	// Until the type links, its function table storage contains an UnlinkedNativeFunction pointer, and reading it as a table faults.
	if (!type->IsLinked())
	{
		return 5;
	}
	RE::BSScript::IFunction* declared = FindFunction(*type, RE::BSFixedString(functionName));
	if (declared == nullptr)
	{
		return 3;
	}
	if (declared == registered.get())
	{
		return 0;
	}
	Copy(Describe(*declared, true), scriptDeclaration, bufferSize);
	if (registered)
	{
		Copy(Describe(*registered, false), registration, bufferSize);
	}
	return 4;
}
