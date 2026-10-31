// SKSE::RegistrationSet is a template, so its non-template base is subclassed and SendEvent written here.
// A set is never destroyed, so declaring a name again returns the set the last save restored.
#include "Shared.h"

#include <SKSE/RegistrationSet.h>

#include <cstring>
#include <map>
#include <memory>
#include <mutex>
#include <string>

namespace
{
	// Must not change, since saves store it.
	constexpr std::uint32_t registrationsRecord = 0x53465247;  // "SFRG"

	constexpr std::uint32_t registrationsVersion = 1;

	class ManagedRegistrationSet : public SKSE::Impl::RegistrationSetBase
	{
	public:
		explicit ManagedRegistrationSet(const std::string_view& a_eventName) :
			RegistrationSetBase(a_eventName)
		{}

		// Enumerates under the base's lock, since a script may register from another thread. One argument list serves every handle, since the VM copies from it.
		bool Send(RE::BSScript::IFunctionArguments* a_arguments)
		{
			RE::BSScript::Internal::VirtualMachine* machine = RE::BSScript::Internal::VirtualMachine::GetSingleton();
			if (machine == nullptr)
			{
				return false;
			}
			RE::BSFixedString name(_eventName);
			Locker lock(_lock);
			for (const RE::VMHandle& handle : _handles)
			{
				machine->SendEvent(handle, name, a_arguments);
			}
			return true;
		}

		std::size_t Count()
		{
			Locker lock(_lock);
			return _handles.size();
		}

		const std::string& Name() const
		{
			return _eventName;
		}
	};

	// Papyrus compares names case-insensitively.
	struct NameLess
	{
		bool operator()(const std::string& a_left, const std::string& a_right) const
		{
			return _stricmp(a_left.c_str(), a_right.c_str()) < 0;
		}
	};

	// A plugin may declare from any thread.
	std::map<std::string, std::unique_ptr<ManagedRegistrationSet>, NameLess> g_sets;
	std::mutex g_setsMutex;

	ManagedRegistrationSet* FindOrCreateSet(const char* eventName)
	{
		if (eventName == nullptr)
		{
			return nullptr;
		}
		std::string name(eventName);
		std::scoped_lock lock(g_setsMutex);
		std::unique_ptr<ManagedRegistrationSet>& set = g_sets[name];
		if (!set)
		{
			set = std::make_unique<ManagedRegistrationSet>(name);
		}
		return set.get();
	}

	ManagedRegistrationSet* AsSet(void* set)
	{
		return static_cast<ManagedRegistrationSet*>(set);
	}

	bool WriteText(SKSE::SerializationInterface* serialization, const std::string& text)
	{
		const std::uint32_t length = static_cast<std::uint32_t>(text.size());
		return serialization->WriteRecordData(&length, sizeof(length)) &&
		       (length == 0 || serialization->WriteRecordData(text.data(), length));
	}

	bool ReadText(SKSE::SerializationInterface* serialization, std::string& text)
	{
		std::uint32_t length = 0;
		if (serialization->ReadRecordData(&length, sizeof(length)) != sizeof(length))
		{
			return false;
		}
		text.assign(length, '\0');
		return length == 0 || serialization->ReadRecordData(text.data(), length) == length;
	}
}

extern "C" __declspec(dllexport) void* SF_PapyrusEvents_Declare(const char* eventName)
{
	return FindOrCreateSet(eventName);
}

extern "C" __declspec(dllexport) bool SF_PapyrusEvents_AddForm(void* set, RE::TESForm* form)
{
	return set != nullptr && form != nullptr && AsSet(set)->Register(form);
}

extern "C" __declspec(dllexport) bool SF_PapyrusEvents_AddAlias(void* set, RE::BGSBaseAlias* alias)
{
	return set != nullptr && alias != nullptr && AsSet(set)->Register(alias);
}

extern "C" __declspec(dllexport) bool SF_PapyrusEvents_AddActiveEffect(void* set, RE::ActiveEffect* effect)
{
	return set != nullptr && effect != nullptr && AsSet(set)->Register(effect);
}

extern "C" __declspec(dllexport) bool SF_PapyrusEvents_RemoveForm(void* set, RE::TESForm* form)
{
	return set != nullptr && form != nullptr && AsSet(set)->Unregister(form);
}

extern "C" __declspec(dllexport) bool SF_PapyrusEvents_RemoveAlias(void* set, RE::BGSBaseAlias* alias)
{
	return set != nullptr && alias != nullptr && AsSet(set)->Unregister(alias);
}

extern "C" __declspec(dllexport) bool SF_PapyrusEvents_RemoveActiveEffect(void* set, RE::ActiveEffect* effect)
{
	return set != nullptr && effect != nullptr && AsSet(set)->Unregister(effect);
}

extern "C" __declspec(dllexport) void SF_PapyrusEvents_Clear(void* set)
{
	if (set != nullptr)
	{
		AsSet(set)->Clear();
	}
}

extern "C" __declspec(dllexport) std::int32_t SF_PapyrusEvents_Count(void* set)
{
	return set == nullptr ? 0 : static_cast<std::int32_t>(AsSet(set)->Count());
}

extern "C" __declspec(dllexport) bool SF_PapyrusEvents_Send(void* set, RE::BSScript::IFunctionArguments* arguments)
{
	return set != nullptr && arguments != nullptr && AsSet(set)->Send(arguments);
}

// One record for every set, since a record type is fixed at build time and event names are not.
bool SF_PapyrusEvents_SaveRecord(SKSE::SerializationInterface* serialization)
{
	std::scoped_lock lock(g_setsMutex);
	if (!serialization->OpenRecord(registrationsRecord, registrationsVersion))
	{
		return false;
	}
	const std::uint32_t count = static_cast<std::uint32_t>(g_sets.size());
	if (!serialization->WriteRecordData(&count, sizeof(count)))
	{
		return false;
	}
	for (const std::pair<const std::string, std::unique_ptr<ManagedRegistrationSet>>& entry : g_sets)
	{
		if (!WriteText(serialization, entry.first) || !entry.second->Save(serialization))
		{
			return false;
		}
	}
	return true;
}

// Creates a set for a name no plugin has declared yet, so registrations are restored regardless of when a plugin starts.
bool SF_PapyrusEvents_LoadRecord(SKSE::SerializationInterface* serialization, std::uint32_t type)
{
	if (type != registrationsRecord)
	{
		return false;
	}
	std::uint32_t count = 0;
	if (serialization->ReadRecordData(&count, sizeof(count)) != sizeof(count))
	{
		return true;
	}
	for (std::uint32_t i = 0; i < count; ++i)
	{
		std::string name;
		if (!ReadText(serialization, name))
		{
			return true;
		}
		ManagedRegistrationSet* set = FindOrCreateSet(name.c_str());
		if (set == nullptr || !set->Load(serialization))
		{
			return true;
		}
	}
	return true;
}

void SF_PapyrusEvents_RevertAll()
{
	std::scoped_lock lock(g_setsMutex);
	for (const std::pair<const std::string, std::unique_ptr<ManagedRegistrationSet>>& entry : g_sets)
	{
		entry.second->Clear();
	}
}
