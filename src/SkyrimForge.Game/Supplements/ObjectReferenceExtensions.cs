using SkyrimForge.Papyrus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="IObjectReference"/>.</summary>
public static class ObjectReferenceExtensions
{
    // The flags CommonLib passes for a sound handle.
    private const uint soundHandleFlags = 0x1A;
    private const uint soundPriority = 1;

    // TESObjectREFR::ChangeFlags::kGameOnlyExtra, the change Papyrus's BlockActivation records before it sets the flag.
    private const uint gameOnlyExtraChange = 0x80000000;

    // TESObjectCELL::ChangeFlags::kFlags, the change Papyrus's Lock records on a cell.
    private const uint cellFlagsChange = 2;

    // The game frees no BSResource.ID.
    private static readonly Dictionary<string, BSResource.ID> soundFiles = new(StringComparer.OrdinalIgnoreCase);

    private static (FormId<BGSSoundOutput> SoundOutputId, float Range)[]? fadingOutputs;

    extension<TReference>(TReference reference) where TReference : IObjectReference
    {
        /// <summary>
        /// Moves the reference to <paramref name="position"/>, raised so the center of its bounds lies no lower than that position,
        /// and tilts it to the terrain across <paramref name="span"/>, with the heading <paramref name="yawDegrees"/>.
        /// </summary>
        /// <remarks>
        /// The height reads the render node once it has loaded and the base record's bounds before, so a second call after the node loads corrects it.
        /// The reference lies level on land that is not loaded.
        /// Call on the game's thread.
        /// </remarks>
        public void LayOnGround(TES tes, NiPoint3 position, float yawDegrees, float span)
        {
            float drop = reference.GetCurrent3D() is { } node
                ? node.World.Translate.Z - node.WorldBound.Center.Z
                : reference.GetBaseObject()?.OriginDrop ?? 0f;
            reference.SetPosition(position with { Z = position.Z + Math.Max(drop, 0f) });
            reference.SetAngle(tes.GetLandAngle(position, yawDegrees, span) ?? new NiPoint3(0f, 0f, (float)(yawDegrees * Math.PI / 180.0)));
            reference.Update3DPosition(false);
        }

        /// <summary>Whether the reference is in an interior cell, as Papyrus's IsInInterior returns it.</summary>
        /// <remarks>False for a reference in no cell. Call on the game's thread.</remarks>
        public bool IsInInterior => reference.ParentCell is { } cell && cell.IsInteriorCell();

        /// <summary>The key that opens the reference's lock, as Papyrus's GetKey returns it.</summary>
        /// <remarks>Null if the reference has no lock or its lock takes no key. Call on the game's thread.</remarks>
        public Key? Key => reference.GetLock()?.Key;

        /// <summary>Locks or unlocks the reference, as Papyrus's Lock does.</summary>
        /// <param name="locked">
        /// Whether to lock the reference. Locking closes the reference if its ExtraAction contains kOpen, and adds a lock if it has none.
        /// Unlocking a reference with no lock does nothing.
        /// </param>
        /// <param name="asOwner">
        /// Whether to update the cells on both sides of a door that leads to another cell.
        /// In each cell that has an owner, unlocking sets kHandChanged, which makes the cell public, locking clears that flag, and either clears kWarnToLeave.
        /// </param>
        /// <remarks>Call on the game's thread.</remarks>
        public void Lock(bool locked = true, bool asOwner = false)
        {
            if (locked && reference.ExtraList.GetByType(ExtraDataType.kAction)?.AsExtraAction()?.Action.HasFlag(OBJECT_ACTION.kOpen) == true)
            {
                BGSOpenCloseForm.SetOpenState(reference, false, true);
            }
            if ((reference.GetLock() ?? (locked ? reference.AddLock() : null)) is { } referenceLock)
            {
                referenceLock.SetLocked(locked);
                reference.AddLockChange();
                if (asOwner && reference.ExtraList.GetByType(ExtraDataType.kTeleport)?.AsExtraTeleport()?.TeleportData is { } teleport)
                {
                    SetOwnedCellPublic(teleport.LinkedDoor?.ParentCell, !locked);
                    SetOwnedCellPublic(reference.ParentCell, !locked);
                }
                reference.SetAltered(true);
            }
        }

        /// <summary>Sets the reference's lock level, as Papyrus's SetLockLevel does. A reference with no lock receives an unlocked one.</summary>
        /// <param name="level">The level, of which the game stores the low 8 bits. A level of 255 requires a key.</param>
        /// <remarks>Call on the game's thread.</remarks>
        public void SetLockLevel(int level)
        {
            REFR_LOCK? existingLock = reference.GetLock();
            if ((existingLock ?? reference.AddLock()) is { } referenceLock)
            {
                referenceLock.BaseLevel = unchecked((sbyte)level);
                if (existingLock == null)
                {
                    referenceLock.SetLocked(false);
                }
                reference.AddLockChange();
                reference.SetAltered(true);
            }
        }

        /// <summary>Creates a detection event at the reference for <paramref name="owner"/>, as Papyrus's CreateDetectionEvent does.</summary>
        /// <remarks>An owner without an AI process creates none. Call on the game's thread.</remarks>
        public void CreateDetectionEvent<TOwner>(TOwner owner, int soundLevel = 0) where TOwner : IActor
        {
            if (owner.ActorRuntimeData.CurrentProcess is { } process)
            {
                process.SetActorsDetectionEvent(owner, reference.GetPosition(), soundLevel, reference);
            }
        }

        /// <summary>Blocks or unblocks activation of the reference, as Papyrus's BlockActivation does.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public void BlockActivation(bool block = true)
        {
            reference.AddChange(gameOnlyExtraChange);
            reference.ExtraList.SetExtraFlags(ExtraFlags.Flag.kBlockActivate, block);
        }

        /// <summary>Sends the animation event <paramref name="eventName"/> to the reference's animation graph, as Papyrus's Debug.SendAnimationEvent does.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="eventName"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public void SendAnimationEvent(string eventName)
        {
            ArgumentNullException.ThrowIfNull(eventName);
            reference.AsIAnimationGraphManagerHolder().NotifyAnimationGraph(eventName);
        }

        /// <summary>Sets the bool animation graph variable <paramref name="variableName"/> to <paramref name="value"/>, as Papyrus's SetAnimationVariableBool does.</summary>
        /// <returns>The game's result, which is true even for a name the graph does not declare.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="variableName"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public bool SetAnimationVariableBool(string variableName, bool value)
        {
            ArgumentNullException.ThrowIfNull(variableName);
            return reference.AsIAnimationGraphManagerHolder().SetGraphVariableBool(variableName, value);
        }

        /// <summary>Sets the float animation graph variable <paramref name="variableName"/> to <paramref name="value"/>, as Papyrus's SetAnimationVariableFloat does.</summary>
        /// <returns>The game's result, which is true even for a name the graph does not declare.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="variableName"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public bool SetAnimationVariableFloat(string variableName, float value)
        {
            ArgumentNullException.ThrowIfNull(variableName);
            return reference.AsIAnimationGraphManagerHolder().SetGraphVariableFloat(variableName, value);
        }

        /// <summary>Sets the int animation graph variable <paramref name="variableName"/> to <paramref name="value"/>, as Papyrus's SetAnimationVariableInt does.</summary>
        /// <returns>The game's result, which is true even for a name the graph does not declare.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="variableName"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public bool SetAnimationVariableInt(string variableName, int value)
        {
            ArgumentNullException.ThrowIfNull(variableName);
            return reference.AsIAnimationGraphManagerHolder().SetGraphVariableInt(variableName, value);
        }

        /// <summary>Plays <paramref name="impactEffect"/> at the reference, as Papyrus's PlayImpactEffect does.</summary>
        /// <param name="impactManager">The game's impact manager.</param>
        /// <param name="impactEffect">The impact data set to play.</param>
        /// <param name="nodeName">The node in the reference's 3D at which the effect plays.</param>
        /// <param name="pickDirectionX">The X component of the direction of the ray the game casts to find a surface.</param>
        /// <param name="pickDirectionY">The Y component of the direction of the ray the game casts to find a surface.</param>
        /// <param name="pickDirectionZ">The Z component of the direction of the ray the game casts to find a surface.</param>
        /// <param name="pickLength">The length of that ray.</param>
        /// <param name="applyNodeRotation">Whether the effect takes the node's rotation.</param>
        /// <param name="useNodeLocalRotation">Whether that rotation is the node's local rotation rather than its world rotation.</param>
        /// <returns>Whether the game played the effect. False if the reference is in no cell or its cell has no physics world.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nodeName"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public bool PlayImpactEffect(BGSImpactManager impactManager, ImpactDataSet impactEffect, string nodeName = "", float pickDirectionX = 0f, float pickDirectionY = 0f,
            float pickDirectionZ = -1f, float pickLength = 512f, bool applyNodeRotation = false, bool useNodeLocalRotation = false)
        {
            ArgumentNullException.ThrowIfNull(nodeName);
            if (reference.ParentCell?.GetbhkWorld() == null)
            {
                return false;
            }
            NiPoint3 pickDirection = new(pickDirectionX, pickDirectionY, pickDirectionZ);
            return impactManager.PlayImpactEffect(reference, impactEffect, nodeName, ref pickDirection, pickLength, applyNodeRotation, useNodeLocalRotation);
        }

        /// <summary>Removes every item from the reference's inventory, as Papyrus's RemoveAllItems does.</summary>
        /// <param name="transferTo">The container the items move to, or <c>null</c> for none.</param>
        /// <param name="keepOwnership">Whether moved items retain the reference as their owner.</param>
        /// <exception cref="InvalidOperationException">The reference is the player, for whom Papyrus also runs a game function no binding reaches.</exception>
        /// <remarks>Papyrus's abRemoveQuestItems has no counterpart, since the game function it reaches has no such parameter. Call on the game's thread.</remarks>
        public void RemoveAllItems(IObjectReference? transferTo = null, bool keepOwnership = false)
        {
            if (reference.IsPlayerRef())
            {
                throw new InvalidOperationException("Removing all of the player's items runs a game function no binding reaches. Call Papyrus's RemoveAllItems instead.");
            }
            reference.GetInventoryChanges(true)?.RemoveAllItems(reference, transferTo?.AsObjectReference(), false, keepOwnership, false);
        }

        /// <summary>Activates the reference as <paramref name="activator"/>, as Papyrus's Activate does without the Papyrus VM.</summary>
        /// <param name="activator">The reference that activates this one.</param>
        /// <param name="defaultProcessingOnly">Whether the activation bypasses a block and sends no OnActivate event.</param>
        /// <returns>Whether the game's default processing ran and succeeded. It is false if that processing is blocked.</returns>
        /// <remarks>Call on the game's thread.</remarks>
        public bool Activate<TActivator>(TActivator activator, bool defaultProcessingOnly = false) where TActivator : IObjectReference =>
            reference.ActivateRef(activator, 0, null, 1, defaultProcessingOnly);

        /// <summary>Disables the reference with <c>Disable()</c>, at once and without a fade, and marks it deleted with <c>SetDelete(true)</c>.</summary>
        /// <remarks>
        /// The form ID resolves until the game unloads the reference. A reference in an inventory stays in the inventory until removed from it.
        /// Call on the game's thread.
        /// </remarks>
        public void DisableAndDelete()
        {
            reference.Disable();
            reference.SetDelete(true);
        }

        /// <summary>Plays the sound file at <paramref name="path"/> from the reference, with a direction and fading with distance.</summary>
        /// <param name="dataHandler">The game's data handler.</param>
        /// <param name="audioManager">The game's audio manager.</param>
        /// <param name="path">The file's path under the game's Data folder, such as <c>Sound\FX\MyMod\Chime.wav</c>.</param>
        /// <param name="range">The distance in game units past which the sound is silent. The game's sound output model whose range is nearest is used.</param>
        /// <returns>The playback, or <c>null</c> if the game did not start the sound.</returns>
        /// <remarks>A missing file is silent. Mono wave and xWMA files play. Call on the game's thread.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The reference has no 3D, or the game declares no sound output model that fades with distance.</exception>
        public SoundInstance? PlaySoundFile(TESDataHandler dataHandler, BSAudioManager audioManager, string path, float range = 2000f)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (reference.GetCurrent3D() is not { } node)
            {
                throw new InvalidOperationException("The reference has no loaded 3D.");
            }
            if (fadingOutputs == null)
            {
                (FormId<BGSSoundOutput> SoundOutputId, float Range)[] outputs =
                [
                    .. dataHandler.GetFormArray<BGSSoundOutput>()
                        .Where(output => output.DoGetAttenuatesWithDistance() && output.DoGetAttenuation() != null)
                        .Select(output => (output.FormId, output.DoGetAttenuation()!.Value.GetMaxDistance()))
                ];
                if (outputs.Length == 0)
                {
                    throw new InvalidOperationException("The game declares no sound output model that fades with distance.");
                }
                fadingOutputs = outputs;
            }
            if (!soundFiles.TryGetValue(path, out BSResource.ID file))
            {
                file = BSResource.ID.New();
                file.GenerateFromPath(path);
                soundFiles.Add(path, file);
            }
            BSSoundHandle handle = BSSoundHandle.New();
            try
            {
                audioManager.GetSoundHandleByFile(handle, file, soundHandleFlags, soundPriority);
                handle.SetOutputModel(fadingOutputs.MinBy(output => Math.Abs(output.Range - range)).SoundOutputId.Form.AsBSISoundOutputModel());
                handle.SetObjectToFollow(node);
                return handle.Play() ? new SoundInstance(handle.SoundID) : null;
            }
            finally
            {
                // The sound continues to play once the handle is deleted.
                handle.Delete();
            }
        }

        /// <summary>
        /// Creates a persistent reference of the base object with <paramref name="baseObjectId"/>, rotated <paramref name="yawDegrees"/> degrees about Z,
        /// as the overload receiving the base object does.
        /// </summary>
        /// <exception cref="ArgumentException">The base object is a leveled list, which Papyrus's PlaceAtMe resolves first.</exception>
        /// <exception cref="InvalidOperationException">
        /// No form has <paramref name="baseObjectId"/>, the form is not a <typeparamref name="TBase"/>, or the game created no reference.
        /// </exception>
        /// <remarks>Call on the game's thread.</remarks>
        public ObjectReference Place<TBase>(TESDataHandler dataHandler, FormId<TBase> baseObjectId, NiPoint3 position, float yawDegrees = 0f) where TBase : struct, ITESBoundObject =>
            reference.Place(dataHandler, baseObjectId.Form, position, yawDegrees);

        /// <summary>
        /// Creates a persistent reference of <paramref name="baseObject"/> at <paramref name="position"/>, rotated <paramref name="yawDegrees"/> degrees about Z.
        /// </summary>
        /// <returns>The reference, with ObjectReference's script bound. Its 3D loads afterwards if its cell is loaded, and <c>WaitFor3DLoadAsync</c> waits for it.</returns>
        /// <exception cref="ArgumentException"><paramref name="baseObject"/> is a leveled list, which Papyrus's PlaceAtMe resolves first.</exception>
        /// <exception cref="InvalidOperationException">The game created no reference.</exception>
        /// <remarks><paramref name="position"/> is in this reference's worldspace or, in an interior, its cell. Call on the game's thread.</remarks>
        public ObjectReference Place<TBase>(TESDataHandler dataHandler, TBase baseObject, NiPoint3 position, float yawDegrees = 0f) where TBase : ITESBoundObject
        {
            ThrowIfLeveledList(baseObject);
            // The last five arguments are the ones Papyrus's PlaceAtMe passes.
            // In an exterior the game puts the reference in the cell that contains the position, not in this reference's cell.
            ObjectReference placedReference = dataHandler.CreateReferenceAtLocation(baseObject, position, new NiPoint3(0f, 0f, yawDegrees * MathF.PI / 180f), reference.ParentCell,
                reference.GetWorldspace(), null, null, null, true, true) ?? throw new InvalidOperationException($"The game created no reference of {baseObject.FormID:X8}.");
            // A reference Papyrus's PlaceAtMe returns has ObjectReference's script bound, which a Papyrus call on it requires.
            ObjectReferenceScript.Dispatcher.Bind(placedReference);
            return placedReference;
        }

        /// <summary>
        /// Creates a persistent reference of the base object with <paramref name="baseObjectId"/> and lays it on the ground at <paramref name="position"/>,
        /// with the heading <paramref name="yawDegrees"/>, as <c>LayOnGround</c> does.
        /// </summary>
        /// <exception cref="ArgumentException">The base object is a leveled list, which Papyrus's PlaceAtMe resolves first.</exception>
        /// <exception cref="InvalidOperationException">
        /// No form has <paramref name="baseObjectId"/>, the form is not a <typeparamref name="TBase"/>, or the game created no reference.
        /// </exception>
        /// <remarks>Call on the game's thread.</remarks>
        public ObjectReference PlaceOnGround<TBase>(TES tes, TESDataHandler dataHandler, FormId<TBase> baseObjectId, NiPoint3 position, float yawDegrees, float span) where TBase : struct, ITESBoundObject
        {
            float drop = Math.Max(baseObjectId.Form.AsTESBoundObject().OriginDrop, 0f);
            ObjectReference placedReference = reference.Place(dataHandler, baseObjectId, position with { Z = position.Z + drop }, yawDegrees);
            placedReference.LayOnGround(tes, position, yawDegrees, span);
            return placedReference;
        }

        /// <summary>Creates an actor of the base with <paramref name="actorBaseId"/> at <paramref name="position"/>, as Papyrus's PlaceActorAtMe does at a marker there.</summary>
        /// <param name="dataHandler">The game's data handler.</param>
        /// <param name="actorBaseId">The actor base, which may be built on a template.</param>
        /// <param name="position">The position, in this reference's worldspace or, in an interior, its cell.</param>
        /// <param name="persistent">Whether the actor is persistent. The one Papyrus's PlaceActorAtMe creates is not.</param>
        /// <returns>The actor, with Actor's script bound. Its 3D loads afterwards if its cell is loaded, and <c>WaitFor3DLoadAsync</c> waits for it.</returns>
        /// <exception cref="InvalidOperationException">
        /// No form has <paramref name="actorBaseId"/>, the form is not an actor base, or the game created no marker or no actor.
        /// </exception>
        /// <remarks>
        /// Call on the game's thread.
        /// Before the 3D loads, <c>SetOutfit</c> takes effect but <c>EquipItem</c> does not, so call <c>EquipItem</c> after <c>WaitFor3DLoadAsync</c>.
        /// </remarks>
        public Actor PlaceActor(TESDataHandler dataHandler, FormId<ActorBase> actorBaseId, NiPoint3 position, bool persistent = false)
        {
            ObjectReference marker = reference.Place(dataHandler, KnownForms.XMarker, position);
            try
            {
                Actor actor = marker.PlaceObjectAtMe(actorBaseId.Form, persistent)?.AsActor() ?? throw new InvalidOperationException($"The game created no actor of {actorBaseId}.");
                // A reference Papyrus's PlaceActorAtMe returns has Actor's script bound, which a Papyrus call on it requires.
                ActorScript.Dispatcher.Bind(actor);
                return actor;
            }
            finally
            {
                marker.DisableAndDelete();
            }
        }

        /// <summary>Waits until the reference's 3D loads, testing <see cref="ObjectReferenceBindings.Is3DLoaded{T}(T)"/> once a frame.</summary>
        /// <param name="utility">The service that waits one frame between tests of the 3D.</param>
        /// <param name="timeout">
        /// The span of <see cref="FrameEventArgs.UnpausedTime"/> after which the task faults if no 3D has loaded, or <see cref="Timeout.InfiniteTimeSpan"/> for no limit.
        /// </param>
        /// <param name="cancellationToken">Cancels the wait on the next frame. A new game or a save load also cancels the wait.</param>
        /// <exception cref="ArgumentNullException"><paramref name="utility"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative and not <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
        /// <exception cref="TimeoutException">The 3D did not load within <paramref name="timeout"/>. The exception is stored in the returned task.</exception>
        /// <remarks>
        /// The game loads a model on another thread, so the wait depends on the disk and on the models queued before it, not on the frame rate.
        /// A model can take over a second when many others are queued before it.
        /// Call on the game's thread.
        /// </remarks>
        public Task WaitFor3DLoadAsync(IUtility utility, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utility);
            Task<bool> loading = utility.WaitUntilAsync(() => reference.Is3DLoaded(), timeout, cancellationToken);
            return Core();

            async Task Core()
            {
                if (!await loading)
                {
                    throw new TimeoutException($"The 3D of {reference.FormID:X8} did not load within {timeout.TotalSeconds:0.##} seconds.");
                }
            }
        }

        /// <summary>
        /// Disables the vegetation within <see cref="PackIn.DisabledVegetationRadius"/> of <paramref name="origin"/>,
        /// then lays each part of <paramref name="packIn"/> on the ground at its offset from <paramref name="origin"/>.
        /// </summary>
        /// <param name="tes">The game's TES, through which the ground and the vegetation are read.</param>
        /// <param name="dataHandler">The game's data handler.</param>
        /// <param name="utility">The service through which the task waits one second.</param>
        /// <param name="packIn">The parts to place.</param>
        /// <param name="origin">The position the parts' offsets are measured from, in this reference's worldspace or, in an interior, its cell.</param>
        /// <param name="cancellationToken">
        /// Cancels the one-second wait. The parts remain placed, and the task ends canceled without returning them. A new game or a save load also cancels the wait.
        /// </param>
        /// <returns>
        /// The reference placed for each part, which excludes a part that <see cref="PackInPart.MaximumTilt"/> leaves out.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="utility"/> or <paramref name="packIn"/> is <c>null</c>.</exception>
        /// <remarks>
        /// A part whose ground is not loaded is placed at the origin's height. The task waits a second for the parts' 3D to load, lays them on the ground again, and completes.
        /// <see cref="PackIn.FitsAt"/> tests whether every part is placed at an origin, and <see cref="PackIn.FindSite"/> searches for such an origin.
        /// Call on the game's thread.
        /// </remarks>
        public Task<IReadOnlyDictionary<PackInPart, ObjectReference>> PlacePackInAsync(TES tes, TESDataHandler dataHandler, IUtility utility, PackIn packIn, NiPoint3 origin,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utility);
            ArgumentNullException.ThrowIfNull(packIn);
            return Core();

            async Task<IReadOnlyDictionary<PackInPart, ObjectReference>> Core()
            {
                cancellationToken.ThrowIfCancellationRequested();
                tes.DisableVegetation(packIn, origin);
                Dictionary<PackInPart, ObjectReference> placedParts = [];
                List<GroundPlacement> laidParts = [];
                foreach (PackInPart part in packIn.Parts)
                {
                    NiPoint3 groundPosition = packIn.GetGroundPosition(tes, origin, part);
                    if (!packIn.IsGroundLevelEnough(tes, groundPosition, part))
                    {
                        continue;
                    }
                    float yaw = part.Yaw ?? Random.Shared.NextSingle() * 360f;
                    ObjectReference partReference = reference.PlaceOnGround(tes, dataHandler, part.BaseObjectId, groundPosition, yaw, part.Span);
                    placedParts.Add(part, partReference);
                    laidParts.Add(new(partReference, groundPosition, yaw, part.Span));
                }
                await tes.LayOnGroundAfterDelayAsync(laidParts, utility, cancellationToken);
                return placedParts;
            }
        }

        /// <summary>Creates a reference of the base object with <paramref name="baseObjectId"/>, as the overload receiving the base object does.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The base object is a leveled list, which Papyrus's PlaceAtMe resolves first.</exception>
        /// <exception cref="InvalidOperationException">
        /// No form has <paramref name="baseObjectId"/>, the form is not a <typeparamref name="TBase"/>, or the game created no reference or did not name it.
        /// </exception>
        /// <remarks>Call on the game's thread.</remarks>
        public ObjectReference PlaceNamed<TBase>(FormId<TBase> baseObjectId, string name) where TBase : struct, ITESBoundObject => reference.PlaceNamed(baseObjectId.Form, name);

        /// <summary>Creates a reference of <paramref name="baseObject"/> at this reference and names it <paramref name="name"/>, as Papyrus's PlaceAtMe creates one.</summary>
        /// <returns>The reference, with ObjectReference's script bound. It is not persistent, whereas the one <c>Place</c> creates is.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="baseObject"/> is a leveled list, which Papyrus's PlaceAtMe resolves first.</exception>
        /// <exception cref="InvalidOperationException">The game created no reference or did not name it.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public ObjectReference PlaceNamed<TBase>(TBase baseObject, string name) where TBase : ITESBoundObject
        {
            ArgumentNullException.ThrowIfNull(name);
            ThrowIfLeveledList(baseObject);
            ObjectReference placedReference = reference.PlaceObjectAtMe(baseObject, false) ?? throw new InvalidOperationException($"The game created no reference of {baseObject.FormID:X8}.");
            try
            {
                if (!placedReference.SetDisplayName(name, true))
                {
                    throw new InvalidOperationException($"The game did not name the reference of {baseObject.FormID:X8}.");
                }
                // A reference Papyrus's PlaceAtMe returns has ObjectReference's script bound, which a Papyrus call on it requires.
                ObjectReferenceScript.Dispatcher.Bind(placedReference);
                return placedReference;
            }
            catch
            {
                placedReference.DisableAndDelete();
                throw;
            }
        }
    }

    // Papyrus's PlaceAtMe resolves a leveled list to one of its entries before it calls the game function PlaceNamed calls.
    private static void ThrowIfLeveledList<TBase>(TBase baseObject) where TBase : ITESBoundObject
    {
        if (baseObject.GetFormType() is FormType.LeveledNPC or FormType.LeveledItem)
        {
            throw new ArgumentException("The base object is a leveled list, which this placement does not resolve. Pass one of its entries.", nameof(baseObject));
        }
    }

    /// <summary>Sets kHandChanged to <paramref name="isPublic"/> and clears kWarnToLeave on a cell that has an owner, as Papyrus's Lock does on each side of a door.</summary>
    private static void SetOwnedCellPublic(Cell? cell, bool isPublic)
    {
        if (cell is { } ownedCell && ownedCell.GetOwner() != null)
        {
            Cell.Flag flags = ownedCell.CellFlags & ~Cell.Flag.kWarnToLeave;
            ownedCell.CellFlags = isPublic ? flags | Cell.Flag.kHandChanged : flags & ~Cell.Flag.kHandChanged;
            ownedCell.AddChange(cellFlagsChange);
        }
    }
}
