// CommonLib's Setting::SetString frees the previous text with this module's C runtime and allocates the new text on this module's heap, and neither is a setting's allocator.
// The new text goes on the game's heap. The previous text stays allocated, since no source states its allocator.
#include <RE/Skyrim.h>
#include <cstring>

extern "C" __declspec(dllexport) bool SF_Setting_SetString(RE::Setting* setting, const char* value)
{
	const std::size_t length = std::strlen(value) + 1;
	auto* text = static_cast<char*>(RE::malloc(length));
	if (text == nullptr)
	{
		return false;
	}
	std::memcpy(text, value, length);
	setting->data.s = text;
	return true;
}
