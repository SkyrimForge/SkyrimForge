#include <RE/Skyrim.h>

#include <cstddef>
#include <cstdint>

// The generator does not bind ConsoleLog::Print because it is variadic. The fixed "%s" prevents a % in the message from being read as a format.
extern "C" __declspec(dllexport) void SF_ConsoleLog_Print(const char* message)
{
	if (RE::ConsoleLog* console = RE::ConsoleLog::GetSingleton())
	{
		console->Print("%s", message);
	}
}

// The game has two command tables, the console's and the scripting language's, and the console prompt runs entries of either, so both are searched.
namespace
{
	struct LocatedCommand
	{
		RE::SCRIPT_FUNCTION* function;
		std::uint16_t        opcode;
	};

	LocatedCommand Locate(const char* name)
	{
		if (RE::SCRIPT_FUNCTION* consoleCommand = RE::SCRIPT_FUNCTION::LocateConsoleCommand(name))
		{
			const std::ptrdiff_t index = consoleCommand - RE::SCRIPT_FUNCTION::GetFirstConsoleCommand();
			return { consoleCommand, static_cast<std::uint16_t>(RE::SCRIPT_FUNCTION::Commands::kConsoleOpBase + index) };
		}
		if (RE::SCRIPT_FUNCTION* scriptCommand = RE::SCRIPT_FUNCTION::LocateScriptCommand(name))
		{
			const std::ptrdiff_t index = scriptCommand - RE::SCRIPT_FUNCTION::GetFirstScriptCommand();
			return { scriptCommand, static_cast<std::uint16_t>(RE::SCRIPT_FUNCTION::Commands::kScriptOpBase + index) };
		}
		return { nullptr, 0 };
	}

	std::uint16_t CountRequiredParameters(const RE::SCRIPT_FUNCTION* function)
	{
		if (function->params == nullptr)
		{
			return function->numParams;
		}

		std::uint16_t requiredCount = 0;
		for (std::uint16_t i = 0; i < function->numParams; ++i)
		{
			if (!function->params[i].optional)
			{
				++requiredCount;
			}
		}
		return requiredCount;
	}
}

extern "C" __declspec(dllexport) std::int64_t SF_SCRIPT_FUNCTION_LocateConsoleCommand(const char* name)
{
	if (name == nullptr)
	{
		return 0;
	}
	return reinterpret_cast<std::int64_t>(Locate(name).function);
}

// An empty parameter block is passed, since a command whose parameters are all optional dereferences it.
extern "C" __declspec(dllexport) std::int32_t SF_SCRIPT_FUNCTION_ExecuteFunction(const char* name, RE::TESObjectREFR* reference, double* result)
{
	if (result != nullptr)
	{
		*result = 0.0;
	}
	if (name == nullptr)
	{
		return -1;
	}

	const LocatedCommand locatedCommand = Locate(name);
	if (locatedCommand.function == nullptr || locatedCommand.function->executeFunction == nullptr)
	{
		return -1;
	}
	if (CountRequiredParameters(locatedCommand.function) != 0)
	{
		return -2;
	}
	if (reference == nullptr && locatedCommand.function->referenceFunction)
	{
		return -3;
	}

	using ScriptData = RE::SCRIPT_FUNCTION::ScriptData;
	ScriptData data{};
	data.opcode = locatedCommand.opcode;
	data.chunkSize = static_cast<std::uint16_t>(sizeof(ScriptData) - offsetof(ScriptData, numParams));
	data.numParams = 0;

	double returnedValue = 0.0;
	std::uint32_t offset = static_cast<std::uint32_t>(offsetof(ScriptData, numParams));
	const bool succeeded = locatedCommand.function->executeFunction(
		locatedCommand.function->params, &data, reference, nullptr, nullptr, nullptr, returnedValue, offset);
	if (result != nullptr)
	{
		*result = returnedValue;
	}
	return succeeded ? 0 : 1;
}

extern "C" __declspec(dllexport) std::int32_t SF_SCRIPT_FUNCTION_NumParams(const char* name)
{
	if (name == nullptr)
	{
		return -1;
	}
	RE::SCRIPT_FUNCTION* command = Locate(name).function;
	return command != nullptr ? static_cast<std::int32_t>(command->numParams) : -1;
}

extern "C" __declspec(dllexport) std::int32_t SF_SCRIPT_FUNCTION_RequiredParams(const char* name)
{
	if (name == nullptr)
	{
		return -1;
	}
	RE::SCRIPT_FUNCTION* command = Locate(name).function;
	return command != nullptr ? static_cast<std::int32_t>(CountRequiredParameters(command)) : -1;
}

// The generator cannot bind the template form factory.
extern "C" __declspec(dllexport) bool SF_Console_RunCommandLine(const char* commandLine, RE::TESObjectREFR* reference)
{
	if (commandLine == nullptr)
	{
		return false;
	}
	RE::ConcreteFormFactory<RE::Script, RE::FormType::Script>* factory = RE::IFormFactory::GetConcreteFormFactoryByType<RE::Script>();
	if (factory == nullptr)
	{
		return false;
	}
	RE::Script* script = factory->Create();
	if (script == nullptr)
	{
		return false;
	}
	script->SetCommand(commandLine);
	script->CompileAndRun(reference);

	delete script;
	return true;
}
