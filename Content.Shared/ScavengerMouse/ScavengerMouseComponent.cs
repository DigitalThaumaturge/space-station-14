using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ScavengerMouse;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedScavengerMouseSystem))]
[AutoGenerateComponentState]
public sealed partial class ScavengerMouseComponent : Component
{
    /// <summary>
    /// Tracks if the scavenger mouse has evolved or not.
    /// </summary>
    [ViewVariables]
    public bool Evolved = false;

    /// <summary>
    /// The container that the scavenger mouse uses to store stolen items
    /// </summary>
    [ViewVariables]
    public Container ItemContainer = default!;

    /// <summary>
    /// Name of the container used to hold the scavenger mouse's stolen goods.
    /// </summary>
    [DataField]
    public string ContainerName = "scavenger_storage";

    /// <summary>
    /// A whitelist governing what items can be stolen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The maximum amount of items the scavenger mouse can steal. Should be large enough to always allow the scavenger mouse to evolve.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Capacity = 500;

    /// <summary>
    /// Current amount of evolution points.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EvolutionPoints = 0.0f;

    /// <summary>
    /// The amount of evolution points required to evolve.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EvolutionLimit = 100f;

    /// <summary>
    /// The amount of evolution points yielded by stealing one material. Is multiplied by stack amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaterialValue = 0.33333333333f;

    /// <summary>
    /// The amount of evolution points yielded by stealing one speso. Is multiplied by stack amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MoneyValue = 0.01f;

    /// <summary>
    /// The amount of evolution points yielded by stealing one food item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FoodValue = 0.2f;

    /// <summary>
    /// The maximum amount of spesos the scavenger mouse can pull from a request computer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxMoneySteal = 500;

    /// <summary>
    /// Outfit ID used to gear up the scavenger when they evolve
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype> OutfitId = "ScavengerMouseGear";

    [DataField("actionDomain", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionDomain = "ActionScavengerMouseDomain";

    /// <summary>
    /// The action for the Domain ability
    /// </summary>
    [DataField("actionDomainEntity")]
    public EntityUid? ActionDomainEntity;

    /// <summary>
    /// The amount of hunger one use of Domain consumes
    /// </summary>
    [DataField("hungerPerDomainUse", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float HungerPerDomainUse = 50f;

    /// <summary>
    /// How many moles of ammonia are released after one use of Domain
    /// </summary>
    [DataField("molesAmmoniaPerDomain"), ViewVariables(VVAccess.ReadWrite)]
    public float MolesAmmoniaPerDomain = 200f;
}
