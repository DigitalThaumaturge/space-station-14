using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tag;

/// <summary>
/// Stores what tags are added on use, usage verb, usage time, and if the tag adding is resuable.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAddTagOnUseSystem))]
public sealed partial class AddTagOnUseComponent : Component
{
    /// <summary>
    /// Can the item be used? Only relevant if the item is not reusable.
    /// </summary>
    [DataField("usable")]
    public bool Usable = true;

    /// <summary>
    /// Can the item be used after someone has already used it?
    /// </summary>
    [DataField("reusable")]
    public bool Reusable = true;

    /// <summary>
    /// What tags does the item confer on use?
    /// </summary>
    [DataField("tags")]
    public List<ProtoId<TagPrototype>> Tags = new();

    /// <summary>
    /// How long does the tagging doafter take?
    /// </summary>
    [DataField("doAfterLength")]
    public float DoAfterLength = 0.5f;

    /// <summary>
    /// What verb should intiate adding the tags?
    /// </summary>
    [DataField("verbText")]
    public LocId VerbText = "add-tag-verb-default";

    /// <summary>
    /// What message should accompany the verb?
    /// </summary>
    [DataField("verbMessage")]
    public LocId VerbMessage = "add-tag-verb-default-message";

    /// <summary>
    /// What popup appears after adding tags?
    /// </summary>
    [DataField("popupText")]
    public LocId PopupText = "add-tag-verb-default-popup";

    /// <summary>
    /// What appears in the description if the item is unusable?
    /// </summary>
    [DataField("unusableDescription")]
    public LocId UnusableDescription = "add-tag-unusable-default";
}
