// CommonLib declares the root field under #ifndef SKYRIM_CROSS_VR, so only the accessors reach it.
// Each getter fills a caller's GFxValue, since a returned object-typed GFxValue takes a reference nothing managed releases.
#include <RE/Skyrim.h>

extern "C" __declspec(dllexport) void SF_TutorialMenu_GetRoot(const RE::TutorialMenu* self, RE::GFxValue* destination)
{
	if (self != nullptr && destination != nullptr)
	{
		*destination = self->GetRoot();
	}
}

extern "C" __declspec(dllexport) void SF_TutorialMenu_SetRoot(RE::TutorialMenu* self, const RE::GFxValue* root)
{
	if (self != nullptr && root != nullptr)
	{
		self->SetRoot(*root);
	}
}

extern "C" __declspec(dllexport) void SF_KinectMenu_GetRoot(const RE::KinectMenu* self, RE::GFxValue* destination)
{
	if (self != nullptr && destination != nullptr)
	{
		*destination = self->GetRoot();
	}
}

extern "C" __declspec(dllexport) void SF_KinectMenu_SetRoot(RE::KinectMenu* self, const RE::GFxValue* root)
{
	if (self != nullptr && root != nullptr)
	{
		self->SetRoot(*root);
	}
}
