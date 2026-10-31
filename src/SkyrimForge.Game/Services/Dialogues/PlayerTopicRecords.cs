namespace SkyrimForge.Game;

internal static class PlayerTopicRecords
{
    private static readonly FormId<Quest> dialogueGeneric = new(0x00013EB3);
    private static readonly FormId<TopicInfo> offerServicesInfo = new(0x0007F6BC);

    // The priority is set before the quest lists the topic, in case the game orders a quest's branches as they are added.
    internal static bool Create(PlayerTopic topic)
    {
        Quest owner = (topic.QuestId ?? dialogueGeneric).Form;
        TopicInfo record = offerServicesInfo.Form;
        Topic createdTopic = RuntimeForms.Create(FormType.Dialogue).AsTopic()!.Value;
        GlobalVariable createdGlobal = RuntimeConditions.CreateGlobal();
        DIALOGUE_DATA data = createdTopic.Data;
        data.Type = DIALOGUE_TYPES.DIALOGUE_TYPE.kPlayerDialogue;
        data.SetSubtype(DIALOGUE_DATA.Subtype.kCustom);
        createdTopic.PriorityAndJournalIndex = ((uint)topic.Priority << 24) | (createdTopic.PriorityAndJournalIndex & 0x00FFFFFFu);
        topic.Attach(createdTopic, record, createdGlobal);
        return owner.AddPlayerTopic(createdTopic);
    }
}
