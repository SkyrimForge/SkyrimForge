// The generator does not bind a function that fills a float array through a pointer.
#include <RE/Skyrim.h>

extern "C" __declspec(dllexport) bool SF_BGSSoundOutput_DoGetSpeakerBias(
	RE::BGSSoundOutput* self, std::uint32_t inputChannelCount, std::uint32_t inputChannel, std::uint32_t outputChannelCount, float* bias)
{
	return self->DoGetSpeakerBias(inputChannelCount, inputChannel, outputChannelCount, bias);
}
