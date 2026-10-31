#include <RE/Skyrim.h>
#include <SKSE/SKSE.h>

#include <cstdint>

namespace
{
	using CallHandler = void (*)(std::int32_t, RE::GFxFunctionHandler::Params*);
	CallHandler g_callHandler = nullptr;

	using MovieHandler = void (*)(RE::GFxMovieView*, RE::GFxValue*);
	MovieHandler g_movieHandler = nullptr;

	class ManagedFunction : public RE::GFxFunctionHandler
	{
	public:
		explicit ManagedFunction(std::int32_t a_token) :
			token(a_token)
		{}

		void Call(Params& a_params) override
		{
			if (g_callHandler != nullptr)
			{
				g_callHandler(token, &a_params);
			}
		}

	private:
		std::int32_t token;
	};

	bool OnMovieCreated(RE::GFxMovieView* view, RE::GFxValue* root)
	{
		if (g_movieHandler != nullptr)
		{
			g_movieHandler(view, root);
		}
		return true;
	}
}

// The handler runs on the thread that advances the movie with the game stopped, so it may call the game directly.
extern "C" __declspec(dllexport) void SF_Scaleform_SetCallHandler(void (*handler)(std::int32_t, RE::GFxFunctionHandler::Params*))
{
	g_callHandler = handler;
}

// Scaleform reference-counts handlers and would delete a static one.
// Never released, since GRefCountImpl::Release is a relocated call this build does not otherwise make.
extern "C" __declspec(dllexport) RE::GFxFunctionHandler* SF_Scaleform_CreateFunctionHandler(std::int32_t token)
{
	return new ManagedFunction(token);
}

extern "C" __declspec(dllexport) bool SF_Scaleform_SetMovieHandler(void (*handler)(RE::GFxMovieView*, RE::GFxValue*))
{
	g_movieHandler = handler;

	// Registered once, since SKSE runs the callback once per registration.
	static const bool registered = []
	{
		const SKSE::ScaleformInterface* scaleform = SKSE::GetScaleformInterface();
		return scaleform != nullptr && scaleform->Register(OnMovieCreated, "SkyrimForge");
	}();
	return registered;
}
