// A separate translation unit because windows.h macros collide with names in CommonLib.
#define NETHOST_USE_AS_STATIC
#include <nethost.h>
#include <coreclr_delegates.h>
#include <hostfxr.h>
#include <windows.h>
#include <cwchar>
#include <string>

namespace
{
	std::string hostError;

	std::string failure;

	bool runtimeMissing = false;

	// FrameworkMissingFailure
	constexpr int frameworkMissingStatus = static_cast<int>(0x80008096);

	constexpr char downloadPage[] = "https://dotnet.microsoft.com/download";

	// Topmost, since Skyrim VR starts the framework after its window exists and a box behind that window blocks the game.
	int ShowMessage(const std::string& text, UINT type)
	{
		int length = MultiByteToWideChar(CP_UTF8, 0, text.c_str(), -1, nullptr, 0);
		std::wstring wideText(static_cast<size_t>(length), L'\0');
		MultiByteToWideChar(CP_UTF8, 0, text.c_str(), -1, wideText.data(), length);
		return MessageBoxW(nullptr, wideText.c_str(), L"SkyrimForge", type | MB_TOPMOST | MB_SETFOREGROUND);
	}

	void HOSTFXR_CALLTYPE CollectHostError(const char_t* message)
	{
		int length = WideCharToMultiByte(CP_UTF8, 0, message, -1, nullptr, 0, nullptr, nullptr);
		if (length <= 1)
		{
			return;
		}
		std::string line(static_cast<size_t>(length), '\0');
		WideCharToMultiByte(CP_UTF8, 0, message, -1, line.data(), length, nullptr, nullptr);
		line.pop_back();
		hostError += hostError.empty() ? line : "\n" + line;
	}

	const char* Fail(const char* step, int status)
	{
		char statusText[11]{};
		_snprintf_s(statusText, _TRUNCATE, "0x%08X", static_cast<unsigned int>(status));
		failure = std::string(step) + " returned " + statusText;
		if (!hostError.empty())
		{
			failure += ":\n" + hostError;
		}
		return failure.c_str();
	}

	bool ResolveSibling(const wchar_t* relativePath, wchar_t* path, size_t capacity)
	{
		HMODULE self = nullptr;
		if (!GetModuleHandleExW(
			GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
			reinterpret_cast<LPCWSTR>(&ResolveSibling), &self))
		{
			return false;
		}
		DWORD length = GetModuleFileNameW(self, path, static_cast<DWORD>(capacity));
		if (length == 0 || length >= capacity)
		{
			return false;
		}
		wchar_t* lastSlash = wcsrchr(path, L'\\');
		if (lastSlash == nullptr)
		{
			return false;
		}
		*(lastSlash + 1) = L'\0';
		return wcsncat_s(path, capacity, relativePath, _TRUNCATE) == 0;
	}

	void ReportPath(void (*report)(const char*), const char* step, const wchar_t* path)
	{
		char line[1024]{};
		int prefixLength = _snprintf_s(line, _TRUNCATE, "%s: ", step);
		if (prefixLength > 0)
		{
			WideCharToMultiByte(CP_UTF8, 0, path, -1, line + prefixLength, static_cast<int>(sizeof(line)) - prefixLength, nullptr, nullptr);
		}
		report(line);
	}
}

extern "C" const char* StartClr(void (*report)(const char*), const char* logDirectory)
{
	report("locating hostfxr");
	wchar_t hostfxrPath[1024]{};
	size_t hostfxrPathLength = sizeof(hostfxrPath) / sizeof(wchar_t);
	if (int status = get_hostfxr_path(hostfxrPath, &hostfxrPathLength, nullptr); status != 0)
	{
		runtimeMissing = true;
		Fail("get_hostfxr_path", status);
		failure += ": no 64-bit .NET installation was found.";
		return failure.c_str();
	}

	ReportPath(report, "loading hostfxr", hostfxrPath);
	HMODULE hostfxr = LoadLibraryW(hostfxrPath);
	if (hostfxr == nullptr)
	{
		return "LoadLibrary(hostfxr) failed";
	}

	auto initializeForRuntimeConfig = reinterpret_cast<hostfxr_initialize_for_runtime_config_fn>(
		GetProcAddress(hostfxr, "hostfxr_initialize_for_runtime_config"));
	auto getRuntimeDelegate = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(
		GetProcAddress(hostfxr, "hostfxr_get_runtime_delegate"));
	auto close = reinterpret_cast<hostfxr_close_fn>(GetProcAddress(hostfxr, "hostfxr_close"));
	auto setErrorWriter = reinterpret_cast<hostfxr_set_error_writer_fn>(GetProcAddress(hostfxr, "hostfxr_set_error_writer"));
	if (initializeForRuntimeConfig == nullptr || getRuntimeDelegate == nullptr || close == nullptr || setErrorWriter == nullptr)
	{
		runtimeMissing = true;
		return "hostfxr lacks the hosting functions .NET Core 3.0 added, so the newest installed .NET predates .NET Core 3.0";
	}
	// Otherwise hostfxr writes to standard error, which the game does not display.
	setErrorWriter(CollectHostError);

	wchar_t runtimeConfigPath[MAX_PATH]{};
	wchar_t assemblyPath[MAX_PATH]{};
	if (!ResolveSibling(L"SkyrimForge\\SkyrimForge.PluginHost.runtimeconfig.json", runtimeConfigPath, MAX_PATH) ||
		!ResolveSibling(L"SkyrimForge\\SkyrimForge.PluginHost.dll", assemblyPath, MAX_PATH))
	{
		return "could not resolve the framework paths";
	}

	// Enables Hot Reload in an attached debugger. Takes effect only when set before the runtime starts.
	SetEnvironmentVariableW(L"DOTNET_MODIFIABLE_ASSEMBLIES", L"debug");

	ReportPath(report, "starting the runtime from", runtimeConfigPath);
	hostfxr_handle context = nullptr;
	// One and two are successes that find a runtime already started in this process.
	if (int status = initializeForRuntimeConfig(runtimeConfigPath, nullptr, &context); status < 0 || context == nullptr)
	{
		runtimeMissing = status == frameworkMissingStatus;
		return Fail("hostfxr_initialize_for_runtime_config", status);
	}

	report("resolving the load delegate");
	void* loadAssemblyPointer = nullptr;
	if (int status = getRuntimeDelegate(context, hdt_load_assembly_and_get_function_pointer, &loadAssemblyPointer); status != 0 || loadAssemblyPointer == nullptr)
	{
		close(context);
		return Fail("hostfxr_get_runtime_delegate", status);
	}
	auto loadAssembly = reinterpret_cast<load_assembly_and_get_function_pointer_fn>(loadAssemblyPointer);

	ReportPath(report, "loading the framework assembly", assemblyPath);
	using InitializeFunction = int(CORECLR_DELEGATE_CALLTYPE*)(const char*);
	InitializeFunction initialize = nullptr;
	int loadResult = loadAssembly(
		assemblyPath,
		L"SkyrimForge.PluginHost.Bootstrap, SkyrimForge.PluginHost",
		L"Initialize",
		UNMANAGEDCALLERSONLY_METHOD,
		nullptr,
		reinterpret_cast<void**>(&initialize));
	if (loadResult != 0 || initialize == nullptr)
	{
		close(context);
		return Fail("resolving SkyrimForge.PluginHost.Bootstrap.Initialize", loadResult);
	}

	report("calling the managed entry point");
	int status = initialize(logDirectory);
	report(status == 0 ? "the managed entry point returned" : "the managed entry point reported a failure");

	// The runtime continues running after the context is closed.
	close(context);

	return status == 0 ? nullptr : "the framework reported a startup failure (see SkyrimForge.log)";
}

extern "C" void ShowStartFailure(const char* error, bool logged)
{
	const std::string summary = "SkyrimForge did not start, so the mods built on it will not run.\n\n";
	if (runtimeMissing)
	{
		std::string text = summary + "SkyrimForge needs a version of .NET that is not installed. Install the current .NET Runtime from "
			+ downloadPage + ", and then start the game again.\n\nWould you like to open that page now?";
		if (ShowMessage(text, MB_YESNO | MB_ICONERROR) == IDYES)
		{
			ShellExecuteA(nullptr, "open", downloadPage, nullptr, nullptr, SW_SHOWNORMAL);
			// ExitProcess would run the shutdown code of plugins that are still loading.
			TerminateProcess(GetCurrentProcess(), 1);
		}
		return;
	}
	const std::string logNote = logged ? "\n\nSkyrimForge.Runtime.log (in SKSE's log folder) records each step of SkyrimForge's startup." : "";
	ShowMessage(summary + error + logNote, MB_OK | MB_ICONERROR);
}
