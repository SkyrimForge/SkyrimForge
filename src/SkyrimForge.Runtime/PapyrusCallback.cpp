// The destructor also notifies the managed side, since a dispatch that returned false never calls the functor.
#include <RE/Skyrim.h>

#include <cstdint>

namespace
{
	// Runs on the thread that ran the stack. A null Variable means the functor is being destroyed.
	using ReturnOrDestructionHandler = void (*)(std::int32_t, const RE::BSScript::Variable*);
	ReturnOrDestructionHandler g_returnOrDestructionHandler = nullptr;

	class ManagedCallback : public RE::BSScript::IStackCallbackFunctor
	{
	public:
		ManagedCallback(std::int32_t a_token, RE::BSScript::IFunctionArguments* a_arguments) :
			token(a_token),
			arguments(a_arguments)
		{}

		// The VM reads the argument list until it releases the stack, which destroys this callback.
		~ManagedCallback() override
		{
			delete arguments;
			if (g_returnOrDestructionHandler != nullptr)
			{
				g_returnOrDestructionHandler(token, nullptr);
			}
		}

		// The Variable is valid only during the handler.
		void operator()(RE::BSScript::Variable a_returnValue) override
		{
			if (g_returnOrDestructionHandler != nullptr)
			{
				g_returnOrDestructionHandler(token, &a_returnValue);
			}
		}

		void SetObject(const RE::BSTSmartPointer<RE::BSScript::Object>&) override
		{}

	private:
		std::int32_t token;
		RE::BSScript::IFunctionArguments* arguments;
	};
}

extern "C" __declspec(dllexport) void SF_Papyrus_SetReturnOrDestructionCallback(
	void (*handler)(std::int32_t, const RE::BSScript::Variable*))
{
	g_returnOrDestructionHandler = handler;
}

// Refcounted from zero. The dispatch takes the first reference, and the last release deletes the callback and its argument list.
extern "C" __declspec(dllexport) RE::BSScript::IStackCallbackFunctor* SF_Papyrus_BeginCallback(
	std::int32_t token, RE::BSScript::IFunctionArguments* arguments)
{
	return new ManagedCallback(token, arguments);
}

extern "C" __declspec(dllexport) void SF_Papyrus_DeleteCallback(
	RE::BSScript::IStackCallbackFunctor* callback)
{
	delete callback;
}
