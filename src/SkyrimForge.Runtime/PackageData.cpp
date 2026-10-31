// TESCustomPackageData stores the values as an array of IPackageData pointers with a parallel array of IDs, which the generator does not bind.
#include "Shared.h"

namespace
{
	RE::TESCustomPackageData* GetCustomData(RE::TESPackage* package)
	{
		return package && package->data ? skyrim_cast<RE::TESCustomPackageData*>(package->data) : nullptr;
	}

	std::int32_t Copy(const char* text, char* buffer, std::int32_t capacity)
	{
		auto length = static_cast<std::int32_t>(text ? std::strlen(text) : 0);
		if (length > 0 && length <= capacity)
		{
			std::memcpy(buffer, text, static_cast<std::size_t>(length));
		}
		return length;
	}

	const char* GetName(RE::TESCustomPackageData* data, std::int8_t uid)
	{
		for (RE::TESCustomPackageData* source : { data, GetCustomData(data->templateParent) })
		{
			if (!source || !source->nameMap)
			{
				continue;
			}
			for (const auto& entry : source->nameMap->nameMap)
			{
				if (entry.uid == uid)
				{
					return entry.name.c_str();
				}
			}
		}
		return "";
	}

	RE::IPackageData* GetEntry(RE::TESPackage* package, std::int32_t index)
	{
		RE::TESCustomPackageData* data = GetCustomData(package);
		return data && index >= 0 && index < data->data.dataSize && data->data.data ? data->data.data[index] : nullptr;
	}
}

extern "C" __declspec(dllexport) std::int32_t SF_Package_DataCount(RE::TESPackage* package)
{
	RE::TESCustomPackageData* data = GetCustomData(package);
	return data && data->data.data ? data->data.dataSize : 0;
}

extern "C" __declspec(dllexport) std::int32_t SF_Package_DataName(RE::TESPackage* package, std::int32_t index, char* buffer, std::int32_t capacity)
{
	RE::TESCustomPackageData* data = GetCustomData(package);
	if (!GetEntry(package, index) || !data->data.uids)
	{
		return 0;
	}
	return Copy(GetName(data, data->data.uids[index]), buffer, capacity);
}

extern "C" __declspec(dllexport) std::int32_t SF_Package_DataTypeName(RE::TESPackage* package, std::int32_t index, char* buffer, std::int32_t capacity)
{
	RE::IPackageData* entry = GetEntry(package, index);
	return entry ? Copy(entry->GetTypeName().c_str(), buffer, capacity) : 0;
}

extern "C" __declspec(dllexport) std::int32_t SF_Package_DataValue(RE::TESPackage* package, std::int32_t index, char* buffer, std::int32_t capacity)
{
	RE::IPackageData* entry = GetEntry(package, index);
	if (!entry)
	{
		return 0;
	}
	RE::BSString text;
	SF_Repair_IPackageData_GetDataAsString(entry, &text, package);
	return Copy(text.c_str(), buffer, capacity);
}
