#include "Shared.h"

#include <cstdint>
#include <memory>

extern "C" __declspec(dllexport) void SF_Player_SetQuestTargets(RE::TESQuest* quest, RE::TESQuestTarget** targets, std::int32_t count)
{
	RE::PlayerCharacter* player = RE::PlayerCharacter::GetSingleton();
	if (player == nullptr || quest == nullptr || count < 0 || (count > 0 && targets == nullptr))
	{
		return;
	}
	{
		RE::BSSpinLockGuard lock{ player->GetQuestTargetsLock() };
		RE::PlayerCharacter::QuestTargetsMap& table = player->GetQuestTargets();
		RE::BSTArray<RE::TESQuestTarget*>* cachedTargets = nullptr;
		if (const auto entry = table.find(quest); entry != table.end())
		{
			cachedTargets = entry->second;
		}
		else if (count > 0)
		{
			cachedTargets = RE::malloc<RE::BSTArray<RE::TESQuestTarget*>>();
			if (cachedTargets == nullptr)
			{
				return;
			}
			std::construct_at(cachedTargets);
			SF_Repair_BSTScatterTable_WrapNextFree(table);
			table.emplace(quest, cachedTargets);
		}
		if (cachedTargets != nullptr)
		{
			cachedTargets->clear();
			for (std::int32_t i = 0; i < count; ++i)
			{
				cachedTargets->push_back(targets[i]);
			}
		}
	}
	auto& flags = REL::Module::IsVR() ? player->GetVRPlayerRuntimeData()->playerFlags : player->GetPlayerRuntimeData().playerFlags;
	flags.forceQuestTargetRepath = true;
}

extern "C" __declspec(dllexport) void SF_Quest_WriteJournalText(RE::BSString* text, const char* description)
{
	if (text != nullptr && description != nullptr)
	{
		*text = description;
	}
}

namespace
{
	void SetInstancePackage(RE::BGSRefAliasInstanceData& instance, RE::TESPackage* package)
	{
		auto* packages = const_cast<RE::BSTArray<RE::TESPackage*>*>(instance.instancedPackages);
		if (packages == nullptr)
		{
			if (package == nullptr)
			{
				return;
			}
			packages = RE::malloc<RE::BSTArray<RE::TESPackage*>>();
			if (packages == nullptr)
			{
				return;
			}
			std::construct_at(packages);
			instance.instancedPackages = packages;
		}
		packages->clear();
		if (package != nullptr)
		{
			packages->push_back(package);
		}
	}

	RE::ExtraAliasInstanceArray* GetOrAddInstances(RE::TESObjectREFR& reference)
	{
		auto* instances = reference.extraList.GetByType<RE::ExtraAliasInstanceArray>();
		if (instances == nullptr)
		{
			instances = new RE::ExtraAliasInstanceArray();
			reference.extraList.Add(instances);
		}
		return instances;
	}

	RE::BGSRefAliasInstanceData* AddInstance(RE::ExtraAliasInstanceArray& instances, RE::TESQuest* quest, const RE::BGSBaseAlias* alias)
	{
		auto* instance = RE::malloc<RE::BGSRefAliasInstanceData>();
		if (instance == nullptr)
		{
			return nullptr;
		}
		instance->quest = quest;
		instance->alias = alias;
		instance->instancedPackages = nullptr;
		instances.aliases.push_back(instance);
		return instance;
	}
}

// A reference has a second instance of the alias once the alias's quest has started, so the package is set on the first only.
// A load of a save written before the alias was forced leaves the alias filled and the reference without an instance, so one is created.
extern "C" __declspec(dllexport) void SF_AliasInstance_SetPackage(RE::TESObjectREFR* reference, RE::TESQuest* quest, const RE::BGSBaseAlias* alias, RE::TESPackage* package)
{
	if (reference == nullptr)
	{
		return;
	}
	if (package == nullptr && reference->extraList.GetByType<RE::ExtraAliasInstanceArray>() == nullptr)
	{
		return;
	}
	RE::ExtraAliasInstanceArray* instances = GetOrAddInstances(*reference);
	RE::BSWriteLockGuard locker{ instances->lock };
	bool                 found = false;
	for (RE::BGSRefAliasInstanceData* instance : instances->aliases)
	{
		if (instance != nullptr && instance->quest == quest && instance->alias == alias)
		{
			SetInstancePackage(*instance, found ? nullptr : package);
			found = true;
		}
	}
	if (!found && package != nullptr)
	{
		if (RE::BGSRefAliasInstanceData* instance = AddInstance(*instances, quest, alias))
		{
			SetInstancePackage(*instance, package);
		}
	}
}

// ForceRefTo adds no instance to the reference the alias object is already forced to.
extern "C" __declspec(dllexport) void SF_AliasInstance_Add(RE::TESObjectREFR* reference, RE::TESQuest* quest, const RE::BGSBaseAlias* alias)
{
	if (reference == nullptr)
	{
		return;
	}
	RE::ExtraAliasInstanceArray* instances = GetOrAddInstances(*reference);
	RE::BSWriteLockGuard locker{ instances->lock };
	for (RE::BGSRefAliasInstanceData* instance : instances->aliases)
	{
		if (instance != nullptr && instance->quest == quest && instance->alias == alias)
		{
			return;
		}
	}
	AddInstance(*instances, quest, alias);
}

extern "C" __declspec(dllexport) void SF_AliasInstance_Remove(RE::TESObjectREFR* reference, const RE::TESQuest* quest, const RE::BGSBaseAlias* alias)
{
	if (reference == nullptr)
	{
		return;
	}
	auto* instances = reference->extraList.GetByType<RE::ExtraAliasInstanceArray>();
	if (instances == nullptr)
	{
		return;
	}
	RE::BSWriteLockGuard locker{ instances->lock };
	for (std::uint32_t i = instances->aliases.size(); i-- > 0;)
	{
		RE::BGSRefAliasInstanceData* instance = instances->aliases[i];
		if (instance == nullptr || instance->quest != quest || instance->alias != alias)
		{
			continue;
		}
		instances->aliases.erase(instances->aliases.begin() + i);
		if (auto* packages = const_cast<RE::BSTArray<RE::TESPackage*>*>(instance->instancedPackages))
		{
			std::destroy_at(packages);
			RE::free(packages);
		}
		RE::free(instance);
	}
}
