// Each stream is a local, since the game's virtual table makes delete call the game's deleting destructor on memory the game did not allocate.
#include <RE/Skyrim.h>

#include <cstdint>
#include <memory>
#include <vector>

extern "C" __declspec(dllexport) bool SF_GameFileSystem_Exists(const char* path)
{
	const RE::BSResourceNiBinaryStream file{ path };
	return file.good();
}

// ErrorCode has no end-of-file value, so a read returning kNone and no bytes is taken as the end of the file.
extern "C" __declspec(dllexport) RE::BSResource::ErrorCode SF_GameFileSystem_ReadAll(
	const char* path, std::vector<std::uint8_t>** buffer, const std::uint8_t** data, std::uint64_t* length)
{
	*buffer = nullptr;
	*data = nullptr;
	*length = 0;
	const RE::BSResourceNiBinaryStream file{ path };
	if (!file.good())
	{
		return RE::BSResource::ErrorCode::kNotExist;
	}

	constexpr std::uint64_t chunkLength = 0x10000;
	auto bytes = std::make_unique<std::vector<std::uint8_t>>();
	std::uint64_t read = 0;
	do
	{
		const std::size_t start = bytes->size();
		bytes->resize(start + chunkLength);
		read = 0;
		const RE::BSResource::ErrorCode error = file.stream->DoRead(bytes->data() + start, chunkLength, read);
		if (error != RE::BSResource::ErrorCode::kNone)
		{
			return error;
		}
		bytes->resize(start + read);
	} while (read != 0);

	*data = bytes->data();
	*length = bytes->size();
	*buffer = bytes.release();
	return RE::BSResource::ErrorCode::kNone;
}

extern "C" __declspec(dllexport) void SF_GameFileSystem_Release(std::vector<std::uint8_t>* buffer)
{
	delete buffer;
}
