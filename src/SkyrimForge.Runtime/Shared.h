#pragma once

// CommonLib first. SKSE.h includes Windows headers through spdlog, whose MAX_PATH, INVALID_HANDLE_VALUE, and REFIID macros break CommonLib's constants of the same names.
#include <RE/Skyrim.h>
#include <SKSE/SKSE.h>

// Returns null on success and a message otherwise. report must not be null.
extern "C" const char* StartClr(void (*report)(const char*), const char* logDirectory);

// Creates a window, so never call it with the loader lock acquired.
extern "C" void ShowStartFailure(const char* error, bool logged);

extern "C" void SF_Events_Install();

extern "C" bool SF_StartFramework();

extern "C" bool SF_FrameworkRunning();

// 1 for AE, 2 for SE, 4 for VR, and 0 for none, as REL::Module::Runtime defines them.
extern "C" __declspec(dllexport) std::uint8_t SF_Module_Edition();

extern "C" void SF_Faults_Install();

std::string SF_Faults_DescribeAddress(const void* address);

extern "C" void SF_Hook_AllocateTrampoline();

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_FindCall(std::uintptr_t callerAddress, std::uintptr_t calleeAddress, std::uint32_t occurrence, std::uint32_t searchLength);

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_CallAt(std::uintptr_t function, std::uint32_t seOffset, std::uint32_t aeOffset, std::uint32_t vrOffset);

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteCall(std::uintptr_t site, std::uintptr_t replacement, const char* owner);

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteEntry(std::uintptr_t target, std::uintptr_t replacement, const char* owner);

extern "C" __declspec(dllexport) std::uint32_t SF_Code_VerifyWrites();

extern "C" __declspec(dllexport) bool SF_Code_FunctionBounds(std::uintptr_t address, std::uintptr_t* begin, std::uintptr_t* end);

extern "C" __declspec(dllexport) bool SF_Code_IsInstructionStart(std::uintptr_t begin, std::uintptr_t address);

extern "C" bool SF_Code_BranchesInto(std::uintptr_t begin, std::uintptr_t end);

extern "C" bool SF_Code_HasRelativeOperand(std::uintptr_t begin, std::uintptr_t end);

extern "C" bool SF_Code_CallBeforeEnd(std::uintptr_t begin, std::uintptr_t end);

extern "C" bool SF_Code_BranchesWithin(std::uintptr_t begin, std::uintptr_t end);

extern "C" void SF_Saving_Install();

bool SF_PapyrusEvents_SaveRecord(SKSE::SerializationInterface* serialization);

bool SF_PapyrusEvents_LoadRecord(SKSE::SerializationInterface* serialization, std::uint32_t type);

// The game requires this before a load and on a new game.
void SF_PapyrusEvents_RevertAll();

bool SF_Papyrus_DeclaredClassSafely(RE::BSScript::Internal::VirtualMachine* machine, const RE::BSScript::TypeInfo& declaredType, std::int32_t* classId, bool* resolved);

void SF_AttachGeneratedEventSinks(bool dataLoaded);

std::uintptr_t SF_VTable_Lookup(const char* name);

void SF_Lifecycle_Forward(SKSE::MessagingInterface::Message* message);

extern "C" bool SF_Repair_BSScript__IVirtualMachine_GetPropertyValue(
	RE::BSScript::Internal::VirtualMachine* machine, RE::BSScript::Object* object,
	const char* propertyName, RE::BSScript::IStackCallbackFunctor* callback);

extern "C" bool SF_Repair_BSScript__IVirtualMachine_SetPropertyValue(
	RE::BSScript::Internal::VirtualMachine* machine, RE::BSScript::Object* object,
	const char* propertyName, RE::BSScript::Variable* value,
	RE::BSScript::IStackCallbackFunctor* callback);

extern "C" bool SF_Repair_IPackageData_GetDataAsString(const RE::IPackageData* data, RE::BSString* text, const RE::TESPackage* package);

// An insert through CommonLib requires this first.
void SF_Repair_BSTScatterTable_WrapNextFree(RE::BSTHashMap<RE::BGSDialogueBranch*, RE::BSTArray<RE::TESTopic*>*>& table);
void SF_Repair_BSTScatterTable_WrapNextFree(RE::PlayerCharacter::QuestTargetsMap& table);

// Returns 0 if the address library lacks the ID. CommonLib would end the process.
extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_FunctionAddress(std::uint64_t se, std::uint64_t ae, std::uint64_t vr);
