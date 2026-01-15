using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class MouseAccentSystem : EntitySystem
{
    private static readonly Regex RegexLastWord = new(@"(\S+)$");
    private static readonly Regex RegexLastPunctuation = new(@"([.!?]+$)(?!.*[.!?])|(?<![.!?])$");
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MouseAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, MouseAccentComponent component)
    {
        var msg = message;
        // Always suffix the message, like a really annoying animal crossing character

        //Checks if the last word of the sentence is all caps
        //So the suffix can be allcapped
        var lastWordAllCaps = !RegexLastWord.Match(msg).Value.Any(char.IsLower);
        var suffix = "";
        var pick = _random.Next(1, 7);
        suffix = Loc.GetString($"accent-mouse-suffix-{pick}");
        if (lastWordAllCaps)
            suffix = suffix.ToUpper();
        msg = RegexLastPunctuation.Replace(msg, suffix);

        return msg;
    }

    private void OnAccentGet(Entity<MouseAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, ent.Comp);
    }
}
