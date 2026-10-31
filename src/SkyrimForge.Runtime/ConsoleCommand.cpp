// The command table is a fixed array, so a command replaces a disabled entry, one whose name begins with a space.
#include <RE/Skyrim.h>

#include <cstdint>
#include <cstring>
#include <string>
#include <string_view>
#include <unordered_map>
#include <vector>

namespace
{
	using CommandHandler = void (*)(std::int64_t, RE::TESObjectREFR*, std::int32_t, const char* const*);
	CommandHandler g_commandHandler = nullptr;

	// Written and read on the game's thread or in queued work, which never overlap.
	std::unordered_map<const RE::SCRIPT_PARAMETER*, std::uint16_t> g_declaredCounts;

	// A console line is at most 512 characters, so a longer argument is a misread.
	constexpr std::uint16_t maxArgumentLength = 512;

	// Never freed, since the table points to the copies until the process exits.
	const char* Copy(const char* text)
	{
		return _strdup(text != nullptr ? text : "");
	}

	bool Matches(const char* existingName, std::string_view name)
	{
		return existingName != nullptr && name.size() == std::strlen(existingName) && _strnicmp(existingName, name.data(), name.size()) == 0;
	}

	bool IsNameInUse(std::string_view name)
	{
		if (name.empty())
		{
			return false;
		}
		const RE::SCRIPT_FUNCTION* consoleCommands = RE::SCRIPT_FUNCTION::GetFirstConsoleCommand();
		for (std::uint16_t i = 0; i < RE::SCRIPT_FUNCTION::Commands::kConsoleCommandsEnd; ++i)
		{
			if (Matches(consoleCommands[i].functionName, name) || Matches(consoleCommands[i].shortName, name))
			{
				return true;
			}
		}
		const RE::SCRIPT_FUNCTION* scriptCommands = RE::SCRIPT_FUNCTION::GetFirstScriptCommand();
		for (std::uint16_t i = 0; i < RE::SCRIPT_FUNCTION::Commands::kScriptCommandsEnd; ++i)
		{
			if (Matches(scriptCommands[i].functionName, name) || Matches(scriptCommands[i].shortName, name))
			{
				return true;
			}
		}
		return false;
	}

	bool Execute(const RE::SCRIPT_PARAMETER* paramInfo, RE::SCRIPT_FUNCTION::ScriptData* scriptData, RE::TESObjectREFR* thisObj, RE::TESObjectREFR*,
		RE::Script*, RE::ScriptLocals*, double& result, std::uint32_t& opcodeOffsetPtr)
	{
		result = 0.0;
		if (g_commandHandler == nullptr || scriptData == nullptr)
		{
			return true;
		}

		const auto declaredCount = g_declaredCounts.find(paramInfo);
		if (declaredCount == g_declaredCounts.end())
		{
			return true;
		}

		const char*   cursor = reinterpret_cast<const char*>(scriptData) + opcodeOffsetPtr;
		std::uint16_t count = 0;
		std::memcpy(&count, cursor, sizeof(count));
		cursor += sizeof(count);
		if (count > declaredCount->second)
		{
			count = declaredCount->second;
		}

		std::vector<std::string> arguments;
		for (std::uint16_t i = 0; i < count; ++i)
		{
			std::uint16_t length = 0;
			std::memcpy(&length, cursor, sizeof(length));
			cursor += sizeof(length);
			if (length > maxArgumentLength)
			{
				break;
			}
			arguments.emplace_back(cursor, length);
			cursor += length;
		}

		std::vector<const char*> argumentPointers;
		for (const std::string& argument : arguments)
		{
			argumentPointers.push_back(argument.c_str());
		}
		g_commandHandler(reinterpret_cast<std::int64_t>(paramInfo), thisObj, static_cast<std::int32_t>(argumentPointers.size()), argumentPointers.data());
		return true;
	}
}

extern "C" __declspec(dllexport) void SF_ConsoleCommand_SetHandler(CommandHandler handler)
{
	g_commandHandler = handler;
}

extern "C" __declspec(dllexport) std::int64_t SF_ConsoleCommand_Add(const char* name, const char* shortName, const char* help,
	const char* const* parameterNames, std::int32_t count)
{
	if (name == nullptr || name[0] == '\0' || name[0] == ' ' || count < 0 || count > 0xFFFF || (count > 0 && parameterNames == nullptr))
	{
		return -2;
	}
	if (IsNameInUse(name) || (shortName != nullptr && IsNameInUse(shortName)))
	{
		return -1;
	}

	RE::SCRIPT_FUNCTION* commands = RE::SCRIPT_FUNCTION::GetFirstConsoleCommand();
	for (std::uint16_t i = 0; i < RE::SCRIPT_FUNCTION::Commands::kConsoleCommandsEnd; ++i)
	{
		RE::SCRIPT_FUNCTION& entry = commands[i];
		if (entry.functionName == nullptr || entry.functionName[0] != ' ')
		{
			continue;
		}

		// At least one element, so a command without parameters still has a unique identifier.
		auto* parameters = new RE::SCRIPT_PARAMETER[count > 0 ? count : 1]{};
		for (std::int32_t j = 0; j < count; ++j)
		{
			parameters[j].paramName = Copy(parameterNames[j]);
			parameters[j].paramType = RE::SCRIPT_PARAM_TYPE::kChar;
			parameters[j].optional = true;
		}

		// The name is written last, so the console cannot match the entry before the rest is in place.
		g_declaredCounts[parameters] = static_cast<std::uint16_t>(count);
		entry.params = parameters;
		entry.numParams = static_cast<std::uint16_t>(count);
		entry.executeFunction = &Execute;
		entry.conditionFunction = nullptr;
		entry.referenceFunction = false;
		entry.helpString = Copy(help);
		entry.shortName = Copy(shortName);
		entry.functionName = Copy(name);
		return reinterpret_cast<std::int64_t>(parameters);
	}
	return 0;
}
