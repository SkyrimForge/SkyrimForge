namespace SkyrimForge.Game;

internal sealed class GreetingSilencer(VoiceSilencer voiceSilencer) : IGreetingSilencer
{
    private readonly VoiceSilencer voiceSilencer = voiceSilencer;

    public void SilenceGreetings<TActor>(TActor actor) where TActor : IActor => voiceSilencer.SilenceGreetings(actor.GetFormId<Actor>(), this);

    public void RestoreGreetings<TActor>(TActor actor) where TActor : IActor => voiceSilencer.RestoreGreetings(actor.GetFormId<Actor>(), this);

    public void SilenceRemarks<TActor>(TActor actor) where TActor : IActor => voiceSilencer.SilenceRemarks(actor.GetFormId<Actor>(), this);

    public void RestoreRemarks<TActor>(TActor actor) where TActor : IActor => voiceSilencer.RestoreRemarks(actor.GetFormId<Actor>(), this);
}
