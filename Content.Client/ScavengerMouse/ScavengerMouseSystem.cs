using Content.Shared.Cargo.Components;
using Content.Shared.Interaction;
using Content.Shared.ScavengerMouse;

namespace Content.Client.ScavengerMouse;

/// <inheritdoc/>
public sealed class ScavengerMouseSystem : SharedScavengerMouseSystem
{
    protected override void OnATMInteract(Entity<ScavengerMouseComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (TryComp<CargoOrderConsoleComponent>(args.Target, out var atm))
        {
            args.Handled = true;
        }
    }
}
