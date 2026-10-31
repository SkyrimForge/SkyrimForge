using SkyrimForge.Game;
using System.Threading;

namespace SkyrimForge;

/// <summary>
/// Arguments for <see cref="DialogueTopic.BeginCallback"/> and <see cref="DialogueTopic.EndCallback"/>.
/// <see cref="NewGameOrSaveLoading"/> is the <see cref="IGameSession.NewGameOrSaveLoading"/> of the game the topic was chosen in.
/// </summary>
public readonly record struct DialogueTopicContext(DialogueTopic Topic, Actor Speaker, CancellationToken NewGameOrSaveLoading);
