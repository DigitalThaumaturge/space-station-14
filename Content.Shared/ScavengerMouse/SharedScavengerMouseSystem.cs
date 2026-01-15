using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Stacks;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Shared.ScavengerMouse;

public abstract class SharedScavengerMouseSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _admin = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public Vector2 NoOffset = new(0, 0);

    private static readonly ProtoId<TagPrototype> MaterialTag = "ConstructionMaterial";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScavengerMouseComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ScavengerMouseComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ScavengerMouseComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ScavengerMouseComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<ScavengerMouseComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<ScavengerMouseComponent, BeforeInteractHandEvent>(OnATMInteract);
    }

    protected virtual void OnComponentInit(Entity<ScavengerMouseComponent> ent, ref ComponentInit args)
    {
        ent.Comp.ItemContainer = Container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerName);
        ent.Comp.ItemContainer.ShowContents = false;
        ent.Comp.ItemContainer.OccludesLight = true;
    }


    private void OnStartup(Entity<ScavengerMouseComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent.Owner, out ActionsComponent? comp))
            return;

        _action.AddAction(ent.Owner, ref ent.Comp.ActionDomainEntity, ent.Comp.ActionDomain, component: comp);
    }

    private void OnShutdown(Entity<ScavengerMouseComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent.Owner, out ActionsComponent? comp))
            return;

        var actions = new Entity<ActionsComponent?>(ent.Owner, comp);
        _action.RemoveAction(actions, ent.Comp.ActionDomainEntity);
    }

    protected virtual void OnDestruction(Entity<ScavengerMouseComponent> ent, ref DestructionEventArgs args)
    {
        Dirty(ent.Owner, ent.Comp);
        var uidXform = Transform(ent.Owner);
        var containedArr = ent.Comp.ItemContainer.ContainedEntities.ToArray();
        foreach (var contained in containedArr)
        {
            Remove(ent.AsNullable(), contained, uidXform);
        }
    }

    private bool CanInsert(Entity<ScavengerMouseComponent?> ent, EntityUid toInsert)
    {
        // Inserted items cannot be mobs, ever
        var itemNotMob = !HasComp<MobStateComponent>(toInsert) && HasComp<ItemComponent>(toInsert);

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (EntityManager.IsQueuedForDeletion(ent.Owner))
            return false;

        if (ent.Comp.ItemContainer.ContainedEntities.Count >= ent.Comp.Capacity)
            return false;

        // Scavenger mice won't steal "food" they can't eat. (why the hell are welding masks edible)
        if (TryComp<EdibleComponent>(toInsert, out var edible) && edible.RequiresSpecialDigestion)
            return false;

        if (itemNotMob && ent.Comp.Whitelist != null)
        {
            var haha = _whitelistSystem.IsValid(ent.Comp.Whitelist, toInsert);
            return _whitelistSystem.IsValid(ent.Comp.Whitelist, toInsert);
        }

        return itemNotMob;
    }

    protected bool Insert(Entity<ScavengerMouseComponent?> ent, EntityUid toInsert)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // Holdover from EntityStorageSystem, which inspired this code
        // TODO: This should be done automatically for all containers
        _joints.RecursiveClearJoints(toInsert);
        if (!Container.Insert(toInsert, ent.Comp.ItemContainer))
            return false;

        Dirty(ent.Owner, ent.Comp);
        return true;
    }

    protected bool Remove(Entity<ScavengerMouseComponent?> ent, EntityUid toRemove, TransformComponent? xform = null)
    {
        if (!Resolve(ent.Owner, ref xform, false))
            return false;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        Container.Remove(toRemove, ent.Comp.ItemContainer, true, true);

        if (Container.IsEntityInContainer(ent.Owner)
            && Container.TryGetOuterContainer(ent.Owner, Transform(ent.Owner), out var outerContainer))
        {
            Container.Insert(toRemove, outerContainer);
            return true;
        }

        var pos = TransformSystem.GetWorldPosition(xform) + TransformSystem.GetWorldRotation(xform).RotateVec(NoOffset);
        TransformSystem.SetWorldPosition(toRemove, pos);
        Dirty(ent.Owner, ent.Comp);
        return true;
    }

    protected virtual void Evolve(Entity<ScavengerMouseComponent> ent)
    {
        _admin.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player} evolved.");

    }

    protected virtual void OnInteract(Entity<ScavengerMouseComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (ent.Comp.Evolved || args.Handled)
            return;

        if (CanInsert(ent.AsNullable(), args.Target))
        {
            if (_tagSystem.HasTag(args.Target, MaterialTag) && TryComp<StackComponent>(args.Target, out var stack1))
                ent.Comp.EvolutionPoints += ent.Comp.MaterialValue * stack1.Count;
            else if (HasComp<CashComponent>(args.Target) && TryComp<StackComponent>(args.Target, out var stack2))
                ent.Comp.EvolutionPoints += ent.Comp.MoneyValue * stack2.Count;
            // If it doesn't fit the above categories, just consider it to be food
            else
                ent.Comp.EvolutionPoints += ent.Comp.FoodValue;

            var xform = Transform(ent.Owner);
            var coordinateEntity = xform.ParentUid.IsValid() ? xform.ParentUid : ent.Owner;
            var itemXform = Transform(args.Target);
            var itemPos = TransformSystem.GetMapCoordinates(args.Target, xform: itemXform);

            if (itemPos.MapId == xform.MapID
                && (itemPos.Position - TransformSystem.GetMapCoordinates(ent.Owner, xform: xform).Position).Length() <= SharedHandsSystem.MaxAnimationRange)
            {
                var initialPosition = TransformSystem.ToCoordinates(coordinateEntity, itemPos);
                _storage.PlayPickupAnimation(args.Target, initialPosition, xform.Coordinates, itemXform.LocalRotation, ent.Owner);
            }

            Insert(ent.AsNullable(), args.Target);
            _admin.Add(LogType.Pickup, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} put {ToPrettyString(args.Target)} into scavenger mouse storage.");

            if (ent.Comp.EvolutionPoints >= ent.Comp.EvolutionLimit)
                Evolve(ent);
            args.Handled = true;
        }
    }

    protected abstract void OnATMInteract(Entity<ScavengerMouseComponent> ent, ref BeforeInteractHandEvent args);
}
