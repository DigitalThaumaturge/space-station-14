using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tag;

/// <summary>
/// System to allow adding of tags via a verb-intiated doafter, i.e. learning new crafting recipes from blueprints
/// by meeting the whitelist
/// </summary>
public sealed class SharedAddTagOnUseSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddTagOnUseComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AddTagOnUseComponent, GetVerbsEvent<ExamineVerb>>(OnTagAdderGetVerbs);
        SubscribeLocalEvent<TagComponent, AddTagOnUseDoAfterEvent>(OnAddTagDoAfter);
    }

    private void OnExamined(Entity<AddTagOnUseComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.Usable)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.UnusableDescription));
    }

    private void OnTagAdderGetVerbs(Entity<AddTagOnUseComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !ent.Comp.Usable)
            return;

        var user = args.User;

        if (!TryComp<TagComponent>(user, out var tagged))
            return;

        args.Verbs.Add(new()
        {
            Text = Loc.GetString(ent.Comp.VerbText),
            Message = Loc.GetString(ent.Comp.VerbMessage),
            Act = () => TryAddTags(ent, (user, tagged), ent.Comp.Tags),
            CloseMenu = true,
        });
    }

    public void TryAddTags(Entity<AddTagOnUseComponent> tagger, Entity<TagComponent> taggee, List<ProtoId<TagPrototype>> tags)
    {

        var doAfterArgs = new DoAfterArgs(EntityManager, taggee.Owner, tagger.Comp.DoAfterLength, new AddTagOnUseDoAfterEvent(), taggee.Owner, taggee.Owner, tagger.Owner)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            BlockDuplicate = true,
        };
        _doafter.TryStartDoAfter(doAfterArgs);
    }

    private void OnAddTagDoAfter(Entity<TagComponent> ent, ref AddTagOnUseDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<AddTagOnUseComponent>(args.Used, out var tagOnUse))
            return;

        tagOnUse.Usable = tagOnUse.Reusable;
        Dirty(args.Used.Value, tagOnUse);
        foreach (var tag in tagOnUse.Tags)
        {
            _tags.AddTag(ent.Owner, tag);
        }
        _popup.PopupClient(Loc.GetString(tagOnUse.PopupText), ent.Owner, ent.Owner);

        args.Handled = true;
    }
}
