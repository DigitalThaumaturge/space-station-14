using Content.Server.Atmos.EntitySystems;
using Content.Server.Cargo.Systems;
using Content.Server.Clothing.Systems;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.Cargo.Components;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.ScavengerMouse;
using System.Linq;

namespace Content.Server.ScavengerMouse
{
    /// <inheritdoc/>
    public sealed class ScavengerMouseSystem : SharedScavengerMouseSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
        [Dependency] private readonly OutfitSystem _outfitSystem = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly CargoSystem _cargoSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ScavengerMouseComponent, ScavengerMouseDomainActionEvent>(OnDomain);
        }

        /// <summary>
        /// Uses hunger to release a specific amount of ammonia into the air. This heals the scavenger mouse through a specific metabolism
        /// Copied from the Rat King's domain
        /// </summary>
        private void OnDomain(Entity<ScavengerMouseComponent> ent, ref ScavengerMouseDomainActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HungerComponent>(ent.Owner, out var hunger))
                return;

            //make sure the hunger doesn't go into the negatives
            if (_hunger.GetHunger(hunger) < ent.Comp.HungerPerDomainUse)
            {
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), ent.Owner, ent.Owner);
                return;
            }
            args.Handled = true;
            _hunger.ModifyHunger(ent.Owner, -ent.Comp.HungerPerDomainUse, hunger);

            _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), ent.Owner);
            var tileMix = _atmos.GetTileMixture(ent.Owner, excite: true);
            tileMix?.AdjustMoles(Gas.Ammonia, ent.Comp.MolesAmmoniaPerDomain);
        }

        protected override void Evolve(Entity<ScavengerMouseComponent> ent)
        {
            base.Evolve(ent);
            var neo = _polymorphSystem.PolymorphEntity(ent.Owner, "ScavengerMouseEvolution");
            if (neo != null)
            {
                _outfitSystem.SetOutfit(neo.Value, ent.Comp.OutfitId, unremovable: true);
            }
        }

        protected override void OnDestruction(Entity<ScavengerMouseComponent> ent, ref DestructionEventArgs args)
        {
            // Kept deleting the scavenger's stolen goods post-evolution. This is the only solution of many that actually worked
            if (TryComp<PolymorphedEntityComponent>(ent.Owner, out var neo) && TryComp<ScavengerMouseComponent>(neo.Parent, out var scav))
            {
                var uidXform = Transform(neo.Parent.Value);
                var containedArr = scav.ItemContainer.ContainedEntities.ToArray();
                foreach (var contained in containedArr)
                {
                    Remove((neo.Parent.Value, scav), contained, uidXform);
                    Insert(ent.AsNullable(), contained);
                }
                Dirty(ent.Owner, ent.Comp);
                Dirty(neo.Parent.Value, scav);
            }
            base.OnDestruction(ent, ref args);
        }

        protected override void OnATMInteract(Entity<ScavengerMouseComponent> ent, ref BeforeInteractHandEvent args)
        {
            if (TryComp<CargoOrderConsoleComponent>(args.Target, out var atm))
            {
                _cargoSystem.StealFunds((args.Target, atm), ent.Comp.MaxMoneySteal);
                args.Handled = true;
            }
        }
    }
}
