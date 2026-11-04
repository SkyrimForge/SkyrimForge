// GetLoadedMods returns a bare TESFile**, which the generator does not bind.
#include <RE/Skyrim.h>
#include <algorithm>
#include <cstdint>
namespace
{
	std::int32_t CopyFiles(const RE::TESFile* const* files, std::int32_t total, const RE::TESFile** buffer, std::int32_t capacity)
	{
		if (files == nullptr)
		{
			return 0;
		}
		std::copy_n(files, (std::min)(total, capacity), buffer);
		return total;
	}
}
extern "C" __declspec(dllexport) std::int32_t SF_TESDataHandler_GetLoadedMods(
	const RE::TESDataHandler* self, const RE::TESFile** buffer, std::int32_t capacity)
{
	return CopyFiles(self->GetLoadedMods(), self->GetLoadedModCount(), buffer, capacity);
}
// On VR, GetLoadedLightMods returns null if SkyrimVRESL is not installed.
extern "C" __declspec(dllexport) std::int32_t SF_TESDataHandler_GetLoadedLightMods(
	const RE::TESDataHandler* self, const RE::TESFile** buffer, std::int32_t capacity)
{
	return CopyFiles(self->GetLoadedLightMods(), self->GetLoadedLightModCount(), buffer, capacity);
}
