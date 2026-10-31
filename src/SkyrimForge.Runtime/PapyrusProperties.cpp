#include "Shared.h"
#include <cstdint>
namespace
{
	// autoVarIndex indexes the object's entire variable array, although Object::GetVariable adds a base class's count.
	const RE::BSScript::PropertyTypeInfo* FindProperty(RE::BSScript::ObjectTypeInfo* objectType, const char* name)
	{
		for (RE::BSScript::ObjectTypeInfo* type = objectType; type != nullptr; type = type->GetParent())
		{
			if (!type->IsLinked())
			{
				continue;
			}
			const RE::BSScript::ObjectTypeInfo::PropertyInfo* properties = type->GetPropertyIter();
			for (std::uint32_t i = 0; properties != nullptr && i < type->GetNumProperties(); ++i)
			{
				if (_stricmp(properties[i].name.c_str(), name) == 0)
				{
					return &properties[i].info;
				}
			}
		}
		return nullptr;
	}
	bool FindBoundObject(RE::BSScript::Internal::VirtualMachine* machine, RE::TESForm* receiver,
		const char* className, RE::BSTSmartPointer<RE::BSScript::Object>& out)
	{
		RE::BSScript::IObjectHandlePolicy* policy = machine->GetObjectHandlePolicy();
		if (policy == nullptr || receiver == nullptr)
		{
			return false;
		}
		RE::VMHandle handle = policy->GetHandleForObject(receiver->GetFormType(), receiver);
		return handle != policy->EmptyHandle() && machine->FindBoundObject(handle, className, out);
	}
}
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_BindScript(RE::TESForm* receiver,
	const char* className)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || className == nullptr)
	{
		return 1;
	}
	RE::BSScript::IObjectHandlePolicy* policy = machine->GetObjectHandlePolicy();
	if (policy == nullptr)
	{
		return 1;
	}
	if (receiver == nullptr)
	{
		return 2;
	}
	RE::VMHandle handle = policy->GetHandleForObject(receiver->GetFormType(), receiver);
	if (handle == policy->EmptyHandle())
	{
		return 2;
	}
	// Resolved first, so FindBoundObject, TypeCanBeBound, and CreateObject never receive an unknown class.
	RE::BSTSmartPointer<RE::BSScript::ObjectTypeInfo> type;
	if (!machine->GetScriptObjectType(className, type) || !type)
	{
		return 3;
	}
	RE::BSTSmartPointer<RE::BSScript::Object> object;
	if (machine->FindBoundObject(handle, className, object) && object)
	{
		return 0;
	}
	if (!machine->TypeCanBeBound(className, handle))
	{
		return 6;
	}
	if (!machine->CreateObject(className, object) || !object)
	{
		return 4;
	}
	// InitObjectProperties with null property data dereferences null and ends the process. The VM raises OnInit without it.
	machine->BindObject(object, handle, false);
	RE::BSTSmartPointer<RE::BSScript::Object> scriptObject;
	return machine->FindBoundObject(handle, className, scriptObject) && scriptObject ? 0 : 5;
}
// Dispatching a member function of a class with no object bound ends the process.
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_HasBoundObject(std::uint64_t receiverHandle,
	const char* className)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || className == nullptr)
	{
		return 1;
	}
	// The class is already resolved by SF_Papyrus_BindArgumentsFor.
	RE::BSTSmartPointer<RE::BSScript::Object> object;
	return machine->FindBoundObject(static_cast<RE::VMHandle>(receiverHandle), className, object) && object ? 0 : 3;
}
// The returned pointer is valid while the object is bound.
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_PropertySlot(RE::TESForm* receiver,
	const char* className, const char* propertyName, RE::BSScript::Variable** variable)
{
	if (variable == nullptr)
	{
		return 1;
	}
	*variable = nullptr;
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || className == nullptr || propertyName == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<RE::BSScript::Object> object;
	if (!FindBoundObject(machine, receiver, className, object) || !object)
	{
		return 2;
	}
	const RE::BSScript::PropertyTypeInfo* property = FindProperty(object->GetTypeInfo(), propertyName);
	if (property == nullptr)
	{
		return 3;
	}
	if (property->autoVarIndex == static_cast<std::uint32_t>(-1))
	{
		return 4;
	}
	// autoVarIndex is read from a .pex that can be older than the code reading it.
	RE::BSScript::ObjectTypeInfo* type = object->GetTypeInfo();
	if (type == nullptr || property->autoVarIndex >= type->GetTotalNumVariables())
	{
		return 5;
	}
	*variable = &object->variables[property->autoVarIndex];
	return 0;
}
// Writes a None array of the declared type, as the VM does for an assignment of None. SetNone would write the plain None type.
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_ClearPropertyArray(RE::TESForm* receiver,
	const char* className, const char* propertyName)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || className == nullptr || propertyName == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<RE::BSScript::Object> object;
	if (!FindBoundObject(machine, receiver, className, object) || !object)
	{
		return 2;
	}
	const RE::BSScript::PropertyTypeInfo* property = FindProperty(object->GetTypeInfo(), propertyName);
	if (property == nullptr)
	{
		return 3;
	}
	if (property->autoVarIndex == static_cast<std::uint32_t>(-1))
	{
		return 4;
	}
	if (!property->type.IsArray())
	{
		return 6;
	}
	RE::BSScript::ObjectTypeInfo* type = object->GetTypeInfo();
	if (type == nullptr || property->autoVarIndex >= type->GetTotalNumVariables())
	{
		return 5;
	}
	object->variables[property->autoVarIndex] = RE::BSScript::Variable(property->type);
	return 0;
}
// The type ID of the class a property declares. The VM does not report a form packed against the wrong class.
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_PropertyClass(const char* className,
	const char* propertyName, std::int32_t* typeId)
{
	if (typeId == nullptr)
	{
		return 1;
	}
	*typeId = 0;
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || className == nullptr || propertyName == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<RE::BSScript::ObjectTypeInfo> type;
	if (!machine->GetScriptObjectType(className, type) || !type)
	{
		return 2;
	}
	const RE::BSScript::PropertyTypeInfo* property = FindProperty(type.get(), propertyName);
	if (property == nullptr)
	{
		return 3;
	}
	if (!property->type.IsObject())
	{
		return 4;
	}
	std::int32_t id = 0;
	bool resolved = false;
	if (!SF_Papyrus_DeclaredClassSafely(machine, property->type, &id, &resolved))
	{
		return 6;
	}
	if (!resolved)
	{
		return 5;
	}
	*typeId = id;
	return 0;
}
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_PropertyArray(const char* className,
	const char* propertyName, RE::BSScript::Variable* variable, std::uint32_t size,
	RE::BSScript::Array** array, std::int32_t* elementType)
{
	if (array == nullptr || elementType == nullptr)
	{
		return 1;
	}
	*array = nullptr;
	*elementType = 0;
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || className == nullptr || propertyName == nullptr || variable == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<RE::BSScript::ObjectTypeInfo> type;
	if (!machine->GetScriptObjectType(className, type) || !type)
	{
		return 2;
	}
	const RE::BSScript::PropertyTypeInfo* property = FindProperty(type.get(), propertyName);
	if (property == nullptr)
	{
		return 3;
	}
	if (!property->type.IsArray())
	{
		return 4;
	}
	std::int32_t id = 0;
	bool resolved = false;
	if (!SF_Papyrus_DeclaredClassSafely(machine, property->type, &id, &resolved))
	{
		return 6;
	}
	if (property->type.IsObjectArray() && !resolved)
	{
		return 5;
	}
	// A Papyrus array stores at most 128 entries.
	RE::BSTSmartPointer<RE::BSScript::Array> allocatedArray;
	if (!machine->CreateArray(property->type, size, allocatedArray) || !allocatedArray || allocatedArray->size() != size)
	{
		return 7;
	}
	// SetArray gives the Variable its own reference, so the returned array outlives this call.
	variable->SetArray(allocatedArray);
	*elementType = id;
	*array = allocatedArray.get();
	return 0;
}
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_GetPropertyIntoCallback(RE::TESForm* receiver,
	const char* className, const char* propertyName, RE::BSScript::IStackCallbackFunctor* callback)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || callback == nullptr || className == nullptr || propertyName == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<RE::BSScript::Object> object;
	if (!FindBoundObject(machine, receiver, className, object) || !object)
	{
		return 2;
	}
	return SF_Repair_BSScript__IVirtualMachine_GetPropertyValue(machine, object.get(), propertyName, callback) ? 0 : 3;
}
// The VM copies the value during this call and releases the callback as it returns, before the setter runs.
extern "C" __declspec(dllexport) std::int32_t SF_Papyrus_SetPropertyIntoCallback(RE::TESForm* receiver,
	const char* className, const char* propertyName, RE::BSScript::Variable* value,
	RE::BSScript::IStackCallbackFunctor* callback)
{
	RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
	if (machine == nullptr || value == nullptr || callback == nullptr || className == nullptr
		|| propertyName == nullptr)
	{
		return 1;
	}
	RE::BSTSmartPointer<RE::BSScript::Object> object;
	if (!FindBoundObject(machine, receiver, className, object) || !object)
	{
		return 2;
	}
	return SF_Repair_BSScript__IVirtualMachine_SetPropertyValue(machine, object.get(), propertyName, value,
		callback)
		? 0
		: 3;
}
