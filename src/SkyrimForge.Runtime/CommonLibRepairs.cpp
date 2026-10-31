#include "Shared.h"

#include <cstddef>
#include <cstdint>
#include <memory>
#include <type_traits>
#include <utility>

// CommonLib declares the third parameter as a Variable&, and it is a BSTSmartPointer<IStackCallbackFunctor>&.
// Called as declared, it reads the Variable's type as the callback pointer.
extern "C" bool SF_Repair_BSScript__IVirtualMachine_GetPropertyValue(
	RE::BSScript::Internal::VirtualMachine* machine, RE::BSScript::Object* object,
	const char* propertyName, RE::BSScript::IStackCallbackFunctor* callback)
{
	if (machine == nullptr || object == nullptr || propertyName == nullptr || callback == nullptr)
	{
		return false;
	}

	// At the ABI a reference to either type is one pointer.
	RE::BSTSmartPointer<RE::BSScript::Object> objectReference(object);
	RE::BSTSmartPointer<RE::BSScript::IStackCallbackFunctor> callbackReference(callback);
	RE::BSScript::Variable& variable = *reinterpret_cast<RE::BSScript::Variable*>(&callbackReference);
	return machine->GetPropertyValue(objectReference, propertyName, variable);
}

// SetPropertyValue takes a fourth parameter, the callback, which CommonLib omits. Without it the write is lost and the call still returns true.
// VR's table has New_1B and New_1D before SetPropertyValue's entry.
namespace
{
	constexpr std::ptrdiff_t setPropertyValueSlot = 0x20;
	constexpr std::ptrdiff_t setPropertyValueVrSlot = 0x22;
}

extern "C" bool SF_Repair_BSScript__IVirtualMachine_SetPropertyValue(
	RE::BSScript::Internal::VirtualMachine* machine, RE::BSScript::Object* object,
	const char* propertyName, RE::BSScript::Variable* value,
	RE::BSScript::IStackCallbackFunctor* callback)
{
	if (machine == nullptr || object == nullptr || propertyName == nullptr || value == nullptr
		|| callback == nullptr)
	{
		return false;
	}

	using SetPropertyValueFunction = bool(RE::BSScript::IVirtualMachine*, RE::BSTSmartPointer<RE::BSScript::Object>&,
		const char*, RE::BSScript::Variable&,
		RE::BSTSmartPointer<RE::BSScript::IStackCallbackFunctor>&);

	RE::BSScript::IVirtualMachine* virtualMachine = static_cast<RE::BSScript::IVirtualMachine*>(machine);

	RE::BSTSmartPointer<RE::BSScript::Object> objectReference(object);
	RE::BSTSmartPointer<RE::BSScript::IStackCallbackFunctor> callbackReference(callback);
	return REL::RelocateVirtual<SetPropertyValueFunction>(setPropertyValueSlot, setPropertyValueVrSlot, virtualMachine,
		objectReference, propertyName, *value, callbackReference);
}

// The game's GetDataAsString takes the TESPackage as a second argument, which CommonLib omits, so the declared call passes whatever the register contains.
// For a location naming an alias, the game's function tests the package for null, so a null package is safe.
namespace
{
	constexpr std::size_t getDataAsStringSlot = 0x0A;
}

extern "C" bool SF_Repair_IPackageData_GetDataAsString(const RE::IPackageData* data, RE::BSString* text, const RE::TESPackage* package)
{
	if (data == nullptr || text == nullptr)
	{
		return false;
	}

	using GetDataAsStringFunction = bool (*)(const RE::IPackageData*, RE::BSString*, const RE::TESPackage*);
	void* slot = (*reinterpret_cast<void* const* const*>(data))[getDataAsStringSlot];
	return reinterpret_cast<GetDataAsStringFunction>(slot)(data, text, package);
}

// The game can leave a scatter table's next free index at its capacity. CommonLib's get_free_entry tests that index before wrapping it, so an insert can write past the entries.
namespace
{
	using BranchTable = RE::BSTHashMap<RE::BGSDialogueBranch*, RE::BSTArray<RE::TESTopic*>*>;

	// The slot counts are protected, so a derived type forms member pointers to them.
	template <class Table>
	struct ScatterTable : Table
	{
		static void WrapNextFree(Table& a_table)
		{
			auto& nextFreeSlot = a_table.*(&ScatterTable::_good);
			const auto capacity = a_table.*(&ScatterTable::_capacity);
			if (capacity > 0 && nextFreeSlot >= capacity)
			{
				spdlog::info("Wrapped a table's next free slot {} to 0, since the table has {} slots.", nextFreeSlot, capacity);
				nextFreeSlot = 0;
			}
		}
	};
}

void SF_Repair_BSTScatterTable_WrapNextFree(BranchTable& table)
{
	ScatterTable<BranchTable>::WrapNextFree(table);
}

void SF_Repair_BSTScatterTable_WrapNextFree(RE::PlayerCharacter::QuestTargetsMap& table)
{
	ScatterTable<RE::PlayerCharacter::QuestTargetsMap>::WrapNextFree(table);
}

// The topic array is allocated on the game's heap and never freed.
extern "C" __declspec(dllexport) bool SF_Repair_TESQuest_BranchedDialogue_Emplace(
	RE::TESQuest* quest, std::int32_t type, RE::BGSDialogueBranch* branch, RE::TESTopic* topic)
{
	if (quest == nullptr || branch == nullptr || topic == nullptr || type < 0 || type >= static_cast<std::int32_t>(RE::DIALOGUE_TYPE::kBranchedTotal))
	{
		return false;
	}
	BranchTable& table = quest->branchedDialogue[type];
	if (table.find(branch) != table.end())
	{
		return true;
	}
	auto* topics = RE::malloc<RE::BSTArray<RE::TESTopic*>>();
	if (topics == nullptr)
	{
		return false;
	}
	std::construct_at(topics);
	topics->push_back(topic);
	SF_Repair_BSTScatterTable_WrapNextFree(table);
	table.emplace(branch, topics);
	return table.find(branch) != table.end();
}
