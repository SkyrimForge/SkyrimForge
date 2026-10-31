// MagicItem::CollectData returns a MagicItemDataCollector by value, with a virtual destructor, which the generator does not bind.
// The projectile effect list is copied, since the collector frees it.
#include <RE/Skyrim.h>

extern "C" __declspec(dllexport) std::int32_t SF_MagicItem_CollectData(RE::MagicItem* self, RE::Effect** costliestEffect, std::int32_t* maxCost,
	RE::Effect** largestAreaEffect, float* highestArea, std::uint32_t* flags, bool* summonsExtraLargeCreature, RE::Effect** buffer, std::int32_t capacity)
{
	RE::MagicItemDataCollector collector = self->CollectData();
	*costliestEffect = collector.costliestEffect;
	*maxCost = collector.maxCost;
	*largestAreaEffect = collector.largestAreaEffect;
	*highestArea = collector.highestArea;
	*flags = collector.flags.underlying();
	*summonsExtraLargeCreature = collector.summonsExtraLargeCreature;
	std::int32_t total = 0;
	for (RE::Effect* entry : collector.projectileEffectList)
	{
		if (total < capacity)
		{
			buffer[total] = entry;
		}
		++total;
	}
	return total;
}
