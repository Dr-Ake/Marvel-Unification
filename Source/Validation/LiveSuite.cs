using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using CyclopsGene;
using DeadpoolsHealingFactor;
using Drake.Hulk;
using GambitXGene;
using GambitXGene.Comps;
using GambitXGene.Systems;
using MagnetoXGene;
using MultipleManXGene;
using NightcrawlerTeleportation;
using Rimworld_Storm;

namespace MarvelUnification.Validation
{
    [StaticConstructorOnStartup]
    public static class SuiteStartup
    {
        internal static string Root;
        internal static string Profile;
        internal static string Selection;
        static SuiteStartup()
        {
            var args = Environment.GetCommandLineArgs();
            var profile = args.FirstOrDefault(a => a.StartsWith("-validation-profile="));
            if (profile == null) return;
            Profile = profile.Substring("-validation-profile=".Length);
            Selection = args.FirstOrDefault(a => a.StartsWith("-validation-selection="))?.Substring("-validation-selection=".Length) ?? "full";
            Root = Path.Combine(LoadedModManager.GetMod<CyclopsGene.CyclopsGeneMod>().Content.RootDir, "Source", "obj", "Validation-" + Profile + (Selection=="duplication"?"-duplication":""));
            if (!args.Any(a => a.Equals("-savedatafolder=" + Path.Combine(Root, "game-data"), StringComparison.OrdinalIgnoreCase))) return;
            UnityEngine.Object.DontDestroyOnLoad(new GameObject("Marvel validation").AddComponent<LiveSuite>());
        }
    }

    public sealed class LiveSuite : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        readonly Dictionary<string, Pawn> characters = new Dictionary<string, Pawn>();
        Map map;
        IntVec3 center;
        IntVec3 arena;
        Mod uiMod;
        string uiName;
        bool finished;
        float deadline;
        void Start() { Application.runInBackground = true; deadline = Time.realtimeSinceStartup + 420; StartCoroutine(Guarded()); }
        void Update() { if (!finished && Time.realtimeSinceStartup > deadline) Finish("FAIL Session exceeded seven-minute bound"); }
        void Write() { File.WriteAllLines(Path.Combine(SuiteStartup.Root, "results.txt"), results); }
        void Check(bool condition, string message) { results.Add((condition ? "PASS " : "FAIL ") + message); Write(); if (!condition) throw new Exception(message); }
        void Test(string name, Action action) { try { action(); } catch (Exception ex) { results.Add("FAIL " + name + ": " + ex); Write(); } }
        void Finish(string message) { if (finished) return; results.Add(message); Write(); finished = true; Application.Quit(); }
        IEnumerator Guarded()
        {
            var run = Run();
            while (!finished)
            {
                bool more; object value;
                try { more = run.MoveNext(); value = more ? run.Current : null; }
                catch (Exception ex) { Finish("FAIL Suite exception " + ex); yield break; }
                if (!more) yield break;
                yield return value;
            }
        }
        Pawn Make(string name, IntVec3 cell)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, fixedBiologicalAge: 30, fixedChronologicalAge: 30));
            if (pawn.genes != null) foreach (var gene in pawn.genes.GenesListForReading.ToList()) pawn.genes.RemoveGene(gene);
            pawn.Name = new NameTriple("Test", name, "Carrier");
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(cell, map, 3), map);
            pawn.drafter.Drafted = true;
            pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_Combat), JobCondition.InterruptForced);
            characters[name] = pawn;
            return pawn;
        }
        Hediff Add(Pawn pawn, string name) { return pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed(name)); }
        object Call(object value, string method, params object[] args) { return AccessTools.Method(value.GetType(), method).Invoke(value,args); }
        void Ticks(int count)
        {
            var method = AccessTools.Method(typeof(TickManager), "DoSingleTick");
            for (int i=0;i<count;i++) method.Invoke(Find.TickManager, null);
        }
        void Move(Pawn pawn, IntVec3 cell)
        {
            pawn.pather.StopDead(); pawn.jobs.StopAll(); pawn.Position = cell;
            pawn.Notify_Teleported(); pawn.drafter.Drafted=true;
            pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_Combat),JobCondition.InterruptForced);
        }
        void ClearArena()
        {
            foreach(var cell in GenRadial.RadialCellsAround(arena,28,true).Where(c=>c.InBounds(map)))
            {
                foreach(var thing in cell.GetThingList(map).ToList())
                    if(thing.def.destroyable && (thing is Plant || thing is Building || thing is Fire || thing is Tornado || thing is Projectile || thing.def.category==ThingCategory.Filth))thing.Destroy();
                map.roofGrid.SetRoof(cell,null);map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.fogGrid.Unfog(cell);
            }
        }
        Thing Wall(IntVec3 cell)
        {
            var prior=cell.GetEdifice(map); if(prior!=null) prior.Destroy();
            return GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall,ThingDefOf.Steel),cell,map);
        }
        Ability Power(Pawn pawn,string name)
        {
            var def=DefDatabase<AbilityDef>.GetNamed(name);
            if(pawn.abilities.GetAbility(def)==null) pawn.abilities.GainAbility(def);
            return pawn.abilities.GetAbility(def);
        }
        void Portrait(Pawn pawn,string name)
        {
            var rt=PortraitsCache.Get(pawn,new Vector2(256,256),Rot4.South,Vector3.zero,1,true,false,false,true);
            var previous=RenderTexture.active; var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
            try { RenderTexture.active=rt; tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); tex.Apply(); File.WriteAllBytes(Path.Combine(SuiteStartup.Root,name+".png"),tex.EncodeToPNG()); }
            finally { RenderTexture.active=previous; UnityEngine.Object.Destroy(tex); }
        }
        void InjectorChecks()
        {
            string[] names={"Cyclops_Injector","DP_HealingInjector","Gambit_Injector","Drake_HulkSerumInjector","JG_PhoenixSerum","Magneto_MagnetoInjector","MM_MultipleManInjector","Nightcrawler_TeleportationInjector","Storm_GeneInjector"};
            string[] powers={"Cyclops_Gene","DP_HealingFactor","Gambit_XGene_Hediff","Drake_HulkIdentity","JG_PhoenixForce","Magneto_Magnetism","MM_MultipleManGeneHediff","Nightcrawler_Teleportation","Storm_Gene"};
            for(int i=0;i<names.Length;i++)
            {
                int index=i;
                Test("Injector "+names[i],()=>{
                    var pawn=Make("Recipient"+index,center+new IntVec3(32,0,index*2));
                    var item=(ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(names[index]));
                    var comp=item.AllComps.OfType<CompUseEffect>().FirstOrDefault(c=>c is not CompUseEffect_DestroySelf);
                    if(comp!=null)
                    {
                        Check(comp.CanBeUsedBy(pawn).Accepted,names[index]+" accepts clean recipient");
                        Call(item.GetComp<CompUsable>(),"UsedBy",pawn);
                        Check(item.Destroyed,names[index]+" consumed once through native use");
                    }
                    else { item.Ingested(pawn,0); Check(item.Destroyed,names[index]+" consumed through native ingestion"); }
                    bool granted=index==6 && DupeSettingsManager.UseGeneMode ? pawn.genes.HasActiveGene(MMDefOf.MM_MultipleManGene) : pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed(powers[index]));
                    Check(granted,names[index]+" grants intended power");
                    if(index==3) Check(!HulkSerumUtility.CanReceiveGammaSerum(pawn,out _),"Hulk serum rejects second bonding");
                    else if(comp is OrganicHediffUseEffect || index==6) {var second=(ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(names[index]));var secondComp=second.AllComps.OfType<CompUseEffect>().First(c=>c is not CompUseEffect_DestroySelf);Check(!secondComp.CanBeUsedBy(pawn).Accepted,names[index]+" rejects duplicate power");}
                });
            }
            Test("Recipe definitions",()=>{
                var recipes=DefDatabase<RecipeDef>.AllDefsListForReading.Where(r=>r.modContentPack?.PackageIdPlayerFacing.Equals("drake.marvelunification",StringComparison.OrdinalIgnoreCase)==true || r.products?.Any(p=>p.thingDef.modContentPack?.PackageIdPlayerFacing.Equals("drake.marvelunification",StringComparison.OrdinalIgnoreCase)==true)==true).ToList();
                Check(recipes.Count>=15,"All crafting and administration recipes loaded: "+recipes.Count);
                foreach(var recipe in recipes) { Check(recipe.Worker!=null,recipe.defName+" recipe worker resolves"); foreach(var product in recipe.products) Check(ThingMaker.MakeThing(product.thingDef)!=null,recipe.defName+" product constructible"); }
            });
            string[] surgeries={"Administer_CyclopsInjector","DP_AdministerHealingInjector","Gambit_AdministerInjector","Magneto_AdministerInjector","MM_AdministerMultipleManInjector","Nightcrawler_AdministerTeleportationInjector"};
            string[] markers={"Cyclops_Gene","DP_HealingFactor","Gambit_XGene_Hediff","Magneto_Magnetism","MM_MultipleManGeneHediff","Nightcrawler_Teleportation"};
            for(int i=0;i<surgeries.Length;i++){int index=i;Test("Surgery "+surgeries[i],()=>{
                var patient=Make("RecipientSurgery"+index,center+new IntVec3(38,0,index*2));var worker=DefDatabase<RecipeDef>.GetNamed(surgeries[index]).Worker;
                worker.ApplyOnPawn(patient,patient.RaceProps.body.corePart,null,new List<Thing>(),null);
                Check(patient.health.hediffSet.HasHediff(HediffDef.Named(markers[index]))||(index==4&&patient.genes.HasActiveGene(MMDefOf.MM_MultipleManGene)),surgeries[index]+" successful surgery grants intended power");
            });}
            Test("Runtime graphics",()=>{
                var defs=DefDatabase<ThingDef>.AllDefsListForReading.Where(d=>d.modContentPack?.PackageIdPlayerFacing.Equals("drake.marvelunification",StringComparison.OrdinalIgnoreCase)==true&&d.graphicData!=null).ToList();
                foreach(var def in defs)Check(def.graphicData.Graphic!=null,"Native graphic resolves: "+def.defName);
            });
        }
        void CyclopsChecks()
        {
            ClearArena();
            var p=characters["Cyclops"]; var home=p.Position; Move(p,arena); CyclopsUtility.EnsureControlVisor(p);
            var controller=CyclopsUtility.GetController(p);
            foreach(var name in new[]{"Cyclops_OpticBlast","Cyclops_OpticVolley","Cyclops_FocusedBeam","Cyclops_FullApertureBlast"})
                Test(name,()=>{
                    AccessTools.Field(typeof(HediffComp_OpticController),"strain").SetValue(controller,0f);
                    var target=Wall(arena+new IntVec3(6,0,0)); int hp=target.HitPoints;
                    var power=Power(p,name); Check(power.Activate(target,target),name+" activates");
                    Check(controller.Strain>0,name+" consumes shared strain"); Ticks(180);
                    Check(target.Destroyed || target.HitPoints<hp,name+" applies concussive damage");
                });
            Test("Cyclops control and leadership",()=>{
                AccessTools.Field(typeof(HediffComp_OpticController),"strain").SetValue(controller,100f);
                var comp=Power(p,"Cyclops_OpticBlast").comps.OfType<CompAbilityEffect_OpticAttack>().Single();
                Check(comp.GizmoDisabled(out _),"Cyclops exhausted strain blocks firing");
                Ticks(60); Check(controller.Strain<100,"Cyclops strain recovers");
                var ally=Make("LeadershipAlly",arena+new IntVec3(0,0,3)); Call(controller,"RefreshLeadershipAura");
                Check(ally.health.hediffSet.HasHediff(CyclopsDefOf.Cyclops_CommandPresence),"Cyclops leadership aura reaches allies");
                ally.Destroy();
            });
            Move(p,home);
        }
        void GambitChecks()
        {
            ClearArena();
            var p=characters["Gambit"]; var home=p.Position; Move(p,arena);
            var deck=p.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff)?.TryGetComp<HediffComp_GambitDeck>();
            Check(deck!=null,"Gambit graft creates live deck");
            Test("Gambit throw",()=>{
                var target=Wall(arena+new IntVec3(8,0,0));int hp=target.HitPoints;int cards=deck.Cards;
                Check(Power(p,"Gambit_ThrowCard").Activate(target,target),"Gambit throw activates");
                Check(deck.Cards==cards-1,"Gambit throw consumes exactly one card"); Ticks(60);
                Check(target.Destroyed||target.HitPoints<hp,"Gambit card explosion damages target");
            });
            Test("Gambit pickup",()=>{
                var target=Wall(arena+new IntVec3(9,0,0));int hp=target.HitPoints;
                Check(Power(p,"Gambit_52CardPickup").Activate(target,target),"Gambit pickup activates"); Ticks(600);
                Check(deck.Cards<5,"Gambit volley drains deck");Check(target.Destroyed||target.HitPoints<hp,"Gambit volley damages target");
                GambitCardVolleyManager.Instance.CancelAllFor(p);
                Ticks(650);Check(deck.Cards>0,"Gambit deck recharges after volley");
            });
            Test("Gambit touch and kinetic",()=>{
                var target=Wall(arena+new IntVec3(1,0,0));int hp=target.HitPoints;
                Power(p,"Gambit_TouchCharge").Activate(target,target); Ticks(30);
                Check(target.Destroyed||target.HitPoints<hp,"Gambit touch charge damages adjacent target");
                bool prior=deck.KineticMeleeEnabled;Power(p,"Gambit_KineticToggle").Activate(p,p);
                Check(deck.KineticMeleeEnabled!=prior,"Gambit kinetic toggle changes live state");
                target=Wall(arena+new IntVec3(1,0,0));hp=target.HitPoints;
                GambitXGene.Utilities.GambitExplosionUtility.TryDoKineticExplosion(p,target,target.Position,deck);Ticks(30);
                Check(target.Destroyed||target.HitPoints<hp,"Gambit kinetic detonation damages target");
                float injury=p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity);
                p.TakeDamage(new DamageInfo(DamageDefOf.Bomb,20,instigator:p));
                Check(p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity)==injury,"Gambit immune to own explosive damage");
            }); Move(p,home);
        }
        void HulkChecks()
        {
            ClearArena();
            var p=characters["Hulk"];var home=p.Position;Move(p,arena);
            Test("Hulk forms and appearance",()=>{
                var body=p.story.bodyType;var skin=p.story.SkinColor;
                HulkUtility.Transform(p);Check(HulkUtility.IsTransformed(p),"Hulk transformation grants form");
                Check(HulkUtility.GetGizmoLabels(p).Count()>=4,"Hulk transformed controls present");
                HulkUtility.SetCustomHulkSkinColor(p,new Color(.2f,.4f,.6f));Check(HulkUtility.HasCustomHulkSkinColor(p),"Hulk per-pawn color saved");
                Portrait(p,"Hulk-Transformed");HulkUtility.Revert(p);
                Check(!HulkUtility.IsTransformed(p)&&p.story.bodyType==body,"Hulk reverts original body");
                HulkUtility.ClearCustomHulkSkinColor(p);Check(!HulkUtility.HasCustomHulkSkinColor(p),"Hulk color resets");HulkUtility.Transform(p);
            });
            Test("Hulk clap",()=>{var target=Wall(arena+new IntVec3(3,0,0));int hp=target.HitPoints;Check(HulkUtility.PerformClap(p),"Hulk clap executes");Check(target.Destroyed||target.HitPoints<hp,"Hulk clap destroys nearby structure");});
            Test("Hulk boulder",()=>{var target=Wall(arena+new IntVec3(12,0,0));int hp=target.HitPoints;Check(HulkUtility.TryThrowBoulderAt(p,target),"Hulk boulder launches");Ticks(180);Check(target.Destroyed||target.HitPoints<hp,"Hulk boulder impacts structure");});
            Test("Hulk leap",()=>{var destination=arena+new IntVec3(0,0,12);Check(HulkUtility.TryLeapTo(p,destination),"Hulk gamma leap launches flyer");Ticks(300);Check(p.Spawned&&p.Position.DistanceTo(destination)<3,"Hulk gamma leap lands at target");Move(p,arena);});
            Test("Hulk healing",()=>{var arm=p.RaceProps.body.AllParts.First(b=>b.def==BodyPartDefOf.Arm);var cut=HediffMaker.MakeHediff(HediffDefOf.Cut,p,arm);cut.Severity=8;p.health.AddHediff(cut);HulkUtility.ApplyRegenerationPulse(p,4);Check(cut.Severity<8,"Hulk regeneration repairs real wound");p.health.AddHediff(HediffDefOf.MissingBodyPart,arm);Check(HulkUtility.TryRegrowMissingPart(p)&&!p.health.hediffSet.PartIsMissing(arm),"Hulk restores missing arm");});
            HulkUtility.Revert(p);Move(p,home);
        }
        void MagnetoChecks()
        {
            ClearArena();
            var p=characters["Magneto"];var home=p.Position;Move(p,arena);var c=p.health.hediffSet.GetFirstHediffOfDef(MagnetoDefOf.Magneto_Magnetism).TryGetComp<HediffComp_Magnetism>();
            Test("Magneto grab/drop",()=>{
                var steel=ThingMaker.MakeThing(ThingDefOf.Steel);steel.stackCount=5;GenSpawn.Spawn(steel,arena+new IntVec3(2,0,0),map);
                Check((bool)Call(c,"TryGrabThing",steel),"Magneto grabs metallic stack");Check(c.GetDirectlyHeldThings().Count==1&&steel.stackCount==4,"Magneto orbits one item from stack");
                Check(c.CompGetGizmos().OfType<Command>().Count()>=3,"Magneto grab, launch and drop controls available");
                Call(c,"DropAllOrbiting");Check(c.GetDirectlyHeldThings().Count==0,"Magneto drop restores physical item");
                var wood=ThingMaker.MakeThing(ThingDefOf.WoodLog);GenSpawn.Spawn(wood,arena+new IntVec3(2,0,2),map);Check(!(bool)Call(c,"TryGrabThing",wood),"Magneto rejects nonmetallic item");
            });
            Test("Magneto launch",()=>{
                var steel=ThingMaker.MakeThing(ThingDefOf.Steel);GenSpawn.Spawn(steel,arena+new IntVec3(2,0,0),map);Call(c,"TryGrabThing",steel);
                var target=Wall(arena+new IntVec3(8,0,0));int hp=target.HitPoints;
                Check((bool)Call(c,"LaunchAt",new LocalTargetInfo(target)),"Magneto payload launches");Ticks(180);
                Check(target.Destroyed||target.HitPoints<hp,"Magneto payload damages target");Check(c.GetDirectlyHeldThings().Count==0,"Magneto launch releases held payload");
            });
            Test("Magneto disarm",()=>{
                var enemy=Make("DisarmTarget",arena+new IntVec3(3,0,3));enemy.SetFaction(Faction.OfPirates);
                var weapon=(ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Gun_Autopistol"));enemy.equipment.AddEquipment(weapon);
                Check((bool)Call(c,"TryGrabFromPawn",enemy)&&enemy.equipment.Primary==null,"Magneto removes target's metallic weapon");Call(c,"DropAllOrbiting");enemy.Destroy();
            });Move(p,home);
        }
        void MultipleManChecks()
        {
            var p=characters["MultipleMan"];
            if(DupeSettingsManager.UseGeneMode&&!p.genes.HasActiveGene(MMDefOf.MM_MultipleManGene))p.genes.AddGene(MMDefOf.MM_MultipleManGene,true);
            Test("Multiple Man duplication",()=>{
                var settings=DupesMod.Instance.Settings;float old=settings.SummonCooldownHours;settings.SummonCooldownHours=0;
                p.equipment.DestroyAllEquipment();
                var weapon=(ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Gun_Autopistol"));p.equipment.AddEquipment(weapon);
                p.skills.GetSkill(SkillDefOf.Melee).Level=13;
                DupeUtility.TrySummonDupe(p);Check(DupeManager.Current.HasActiveDupe(p),"Multiple Man creates registered dupe");
                var dupe=map.mapPawns.AllPawnsSpawned.First(q=>DupeManager.Current.IsRegisteredDupe(q));
                Check(dupe.skills.GetSkill(SkillDefOf.Melee).Level==p.skills.GetSkill(SkillDefOf.Melee).Level&&dupe.equipment.Primary?.def==weapon.def,"Multiple Man copies skill and equipment");
                Check(!DupeManager.Current.CanSummon(dupe,out _),"Duplicate cannot duplicate itself");
                Check(DupeManager.Current.TryDismissOldestDupe(p)&&!DupeManager.Current.HasActiveDupe(p),"Multiple Man dismisses clone");
                DupeUtility.TrySummonDupe(p);dupe=map.mapPawns.AllPawnsSpawned.First(q=>DupeManager.Current.IsRegisteredDupe(q));
                DupeUtility.BuildSelfDismissGizmo(dupe).action();Check(dupe.Destroyed,"Multiple Man dupe self-dismiss works");settings.SummonCooldownHours=old;
            });
        }
        IEnumerator NightcrawlerChecks()
        {
            ClearArena();
            var p=characters["Nightcrawler"];var home=p.Position;Move(p,arena);
            HediffComp_TeleportToggle c=null;var target=arena+new IntVec3(0,0,8);
            Test("Nightcrawler toggle",()=>{
                Check(TeleportUtility.TryGetTeleportComp(p,out c),"Nightcrawler teleport comp resolves");c.SetTeleportEnabled(true);
                Check(TeleportUtility.TryGetActiveTeleportComp(p,out _),"Nightcrawler toggle enables movement override");
                Check(TeleportUtility.CanTeleportTo(p,target,c),"Nightcrawler valid destination accepted");
                p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Goto,target));
            });
            // Native path requests complete between frames, rather than inside a tight tick loop.
            for(int i=0;i<120&&p.Position!=target;i++){Ticks(1);yield return null;}
            Test("Nightcrawler native pathing",()=>{
                Check(p.Position==target,"Nightcrawler native goto pathing teleports to destination");
                c.ToggleTeleport();Check(!TeleportUtility.TryGetActiveTeleportComp(p,out _),"Nightcrawler toggle restores normal pathing");
                Check(!TeleportUtility.CanTeleportTo(p,IntVec3.Invalid,c),"Nightcrawler rejects invalid destination");c.SetTeleportEnabled(true);
            });Test("Nightcrawler cleanup",()=>Move(p,home));
        }
        void StormChecks()
        {
            ClearArena();
            var p=characters["Storm"];var home=p.Position;Move(p,arena);
            Test("Storm weather choices",()=>{
                var component=map.GetComponent<StormGodComponent>();Check(component!=null,"Storm map component created");
                foreach(var weather in new[]{"Clear","Rain","RainyThunderstorm","FoggyRain","SnowGentle","SnowHard"}){component.SummonStorm(60000,WeatherDef.Named(weather));Ticks(1);Check(component.targetWeather.defName==weather&&map.weatherManager.curWeather.defName==weather,"Storm weather choice: "+weather);}
                Power(p,"Storm_SummonStorm").Activate(p,p);Check(component.targetWeather.defName=="RainyThunderstorm","Storm summon ability invokes weather");
                component.stormTicksLeft=1;Ticks(1);Check(!component.isStormActive&&component.targetWeather==null,"Storm duration expires cleanly");
            });
            Test("Storm lightning/tornado",()=>{
                var lightning=Power(p,"Storm_CallLightning");var cell=arena+new IntVec3(8,0,0);var wall=Wall(cell);int hp=wall.HitPoints;lightning.Activate(cell,cell);Ticks(180);
                Check(wall.Destroyed||wall.HitPoints<hp,"Storm called lightning damages target cell");
                Power(p,"Storm_SummonTornado").Activate(cell,cell);Check(map.listerThings.ThingsOfDef(ThingDef.Named("Storm_Tornado")).Count>0,"Storm summons real tornado");
                float injuries=p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity);p.TakeDamage(new DamageInfo(DamageDefOf.TornadoScratch,30));
                Check(p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity)==injuries,"Storm immune to tornado scratch");
                foreach(var t in map.listerThings.ThingsOfDef(ThingDef.Named("Storm_Tornado")).ToList())t.Destroy();
            });Move(p,home);
        }
        void JeanGreyChecks()
        {
            var p=characters["JeanGrey"];var home=p.Position;Move(p,arena);
            Test("Jean Grey powers",()=>{
                var wall=Wall(arena+new IntVec3(4,0,0));int hp=wall.HitPoints;JeanGreyMod.PsionicMechanics.DoTelekineticPush(p,wall);Check(wall.Destroyed||wall.HitPoints<hp,"Jean Grey telekinetic push damages target");
                wall=Wall(arena+new IntVec3(5,0,0));hp=wall.HitPoints;JeanGreyMod.PsionicMechanics.DoMindBlast(p,wall);Check(wall.Destroyed||wall.HitPoints<hp,"Jean Grey molecular deconstruction damages target");
                Rand.PushState(5);try{float injury=p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity);p.TakeDamage(new DamageInfo(DamageDefOf.Bullet,20,weapon:ThingDef.Named("Gun_Autopistol")));Check(p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity)==injury,"Jean Grey psionic shield blocks projectile");}finally{Rand.PopState();}
            });Move(p,home);
        }
        void DeadpoolChecks()
        {
            var p=characters["Deadpool"];var settings=DeadpoolsHealingFactorMod.settings;
            int interval=settings.ticksBetweenHeals;float heal=settings.baseHealAmount;float regrow=settings.regrowSpeed;
            settings.ticksBetweenHeals=1;settings.baseHealAmount=5;settings.regrowSpeed=1;
            Test("Deadpool healing",()=>{
                var arm=p.RaceProps.body.AllParts.First(b=>b.def==BodyPartDefOf.Arm);var cut=HediffMaker.MakeHediff(HediffDefOf.Cut,p,arm);cut.Severity=8;p.health.AddHediff(cut);
                Ticks(1);Check(cut.Severity<8,"Deadpool healing repairs real wound through tick patch");
                var flu=p.health.AddHediff(HediffDef.Named("Flu"));flu.Severity=.1f;Ticks(1);Check(!p.health.hediffSet.hediffs.Contains(flu),"Deadpool healing cures disease");
                var scar=HediffMaker.MakeHediff(HediffDefOf.Cut,p,arm);scar.Severity=1;var permanent=scar.TryGetComp<HediffComp_GetsPermanent>();AccessTools.Field(permanent.GetType(),"isPermanentInt").SetValue(permanent,true);p.health.AddHediff(scar);
                Ticks(1);Check(!p.health.hediffSet.hediffs.Contains(scar),"Deadpool healing removes permanent scar");
                p.health.AddHediff(HediffDefOf.MissingBodyPart,arm);Ticks(8);Check(!p.health.hediffSet.PartIsMissing(arm),"Deadpool regrows missing arm");
                Check(p.needs.mood.CurLevel>.99f&&p.story.traits.HasTrait(TraitDefOf.Psychopath),"Deadpool mood and trait effects apply");
                var excluded=p.health.AddHediff(HediffDef.Named("Flu"));excluded.Severity=.1f;settings.SetExcludedFromHealing("Flu",true);Ticks(1);Check(p.health.hediffSet.hediffs.Contains(excluded),"Deadpool healing respects excluded condition");settings.SetExcludedFromHealing("Flu",false);p.health.RemoveHediff(excluded);
                bool enabled=settings.enableHealing;settings.enableHealing=false;cut=HediffMaker.MakeHediff(HediffDefOf.Cut,p,arm);cut.Severity=8;p.health.AddHediff(cut);Ticks(1);Check(Math.Abs(cut.Severity-8)<.01f,"Deadpool healing setting disables healing");settings.enableHealing=enabled;p.health.RemoveHediff(cut);
            });
            Test("Adamantium chamber and claws",()=>{
                var home=p.Position;Move(p,arena);p.drafter.Drafted=false;
                var chamber=(Building_AdamantiumChamber)ThingMaker.MakeThing(ThingDef.Named("DP_AdamantiumChamber"));GenSpawn.Spawn(chamber,arena+new IntVec3(4,0,4),map);
                Check(!chamber.HasFuelFor(p,true),"Adamantium chamber rejects insufficient fuel");chamber.Refuel.Refuel(1000f);float fuel=chamber.Refuel.Fuel;
                int duration=settings.infusionDurationTicks;settings.infusionDurationTicks=30;
                Move(p,chamber.InteractionCell);p.drafter.Drafted=false;
                p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DPDefOf.DP_UseAdamantiumChamberWithClaws,chamber));Ticks(8);
                Check(chamber.Infusion.Active&&DPAdamantiumUtility.ShouldHidePawnDuringInfusion(p),"Adamantium native job starts infusion and hides subject");Ticks(80);
                Check(DPAdamantiumUtility.HasAdamantium(p)&&DPAdamantiumUtility.HasWolverineClaws(p),"Adamantium native job grants skeleton and claws");
                Check(chamber.Refuel.Fuel<fuel&&!chamber.Infusion.Active&&!DPAdamantiumUtility.ShouldHidePawnDuringInfusion(p),"Adamantium infusion consumes fuel and restores subject");
                if(p.equipment.Primary!=null)p.equipment.DestroyEquipment(p.equipment.Primary);var gun=(ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Gun_Autopistol"));p.equipment.AddEquipment(gun);
                var claws=p.health.hediffSet.GetFirstHediffOfDef(DPDefOf.DP_WolverineClaws).TryGetComp<HediffComp_WolverineClaws>();
                claws.CompGetGizmos().OfType<Command_Action>().Single().action();Check(p.equipment.Primary?.def==DPDefOf.DP_WolverineClawWeapon,"Wolverine extends claws and replaces held weapon");
                float before=p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity);claws.CompGetGizmos().OfType<Command_Action>().Single().action();
                Check(p.equipment.Primary==gun,"Wolverine retracts claws and restores prior weapon");Check(p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity)>before,"Wolverine retraction wounds hands");
                var arm=p.RaceProps.body.AllParts.First(b=>b.def==BodyPartDefOf.Arm);p.health.AddHediff(HediffDefOf.MissingBodyPart,arm);Check(!p.health.hediffSet.PartIsMissing(arm),"Adamantium skeleton prevents protected limb removal");
                settings.infusionDurationTicks=duration;Move(p,home);
            });
            Test("Deadpool resurrection",()=>{
                foreach(var h in p.health.hediffSet.hediffs.Where(h=>h is Hediff_Injury||h is Hediff_MissingPart).ToList())p.health.RemoveHediff(h);
                p.Kill(null);Check(p.Dead&&p.Corpse!=null,"Deadpool death creates real corpse");Call(p.Corpse,"TickRare");Check(!p.Dead&&p.Spawned,"Deadpool corpse healing resurrects pawn");
            });
            KidnapChecks();
            settings.ticksBetweenHeals=interval;settings.baseHealAmount=heal;settings.regrowSpeed=regrow;
        }
        void KidnapChecks()
        {
            var settings=DeadpoolsHealingFactorMod.settings;
            Test("Deadpool kidnapping protection",()=>{
                ClearArena();var pirateFaction=Faction.OfPirates;
                if(pirateFaction==null){pirateFaction=FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(FactionDefOf.Pirate));Find.FactionManager.Add(pirateFaction);}
                var rescuer=Make("Kidnapper",arena);rescuer.SetFaction(pirateFaction);
                var guest=Make("ProtectedGuest",arena+new IntVec3(3,0,0));guest.drafter.Drafted=false;guest.SetFaction(pirateFaction);Add(guest,"DP_HealingFactor");guest.health.AddHediff(HediffDef.Named("Anesthetic"));
                Check(guest.Downed,"Kidnapping fixture has downed healing-factor guest");
                if(guest.guest==null)guest.guest=new Pawn_GuestTracker(guest);
                map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                bool protection=settings.preventHealingFactorKidnap;settings.preventHealingFactorKidnap=false;
                results.Add("INFO kidnapping guest="+(guest.guest!=null)+" reachable="+rescuer.CanReserveAndReach(guest,PathEndMode.Touch,Danger.Deadly)+" faction="+guest.Faction+" actual="+KidnapAIUtility.ReachableWoundedGuest(rescuer));Write();
                Check(KidnapAIUtility.ReachableWoundedGuest(rescuer)==guest,"Disabled kidnapping protection retains native candidate");settings.preventHealingFactorKidnap=true;
                Check(KidnapAIUtility.ReachableWoundedGuest(rescuer)==null,"Enabled kidnapping protection excludes healing-factor candidate");settings.preventHealingFactorKidnap=protection;guest.Destroy();rescuer.Destroy();
            });
        }
        void ExtraHulkChecks()
        {
            ClearArena();
            var p=characters["Hulk"];var settings=IncredibleHulkMod.Settings;
            var initialIdentity=HulkUtility.GetIdentityHediff(p).TryGetComp<HediffComp_HulkIdentity>();AccessTools.Field(typeof(HediffComp_HulkIdentity),"rageTicksRemaining").SetValue(initialIdentity,0);initialIdentity.TickRage();HulkUtility.Revert(p);
            Test("Hulk timers and rage",()=>{
                settings.enableTimeRestrictedHulk=true;HulkUtility.Transform(p);var identity=HulkUtility.GetIdentityHediff(p).TryGetComp<HediffComp_HulkIdentity>();identity.StartManualFormTimer(2);Ticks(3);Check(!HulkUtility.IsTransformed(p),"Hulk manual form timer expires and reverts");settings.enableTimeRestrictedHulk=false;
                var carrier=Make("HostileCarrier",p.Position+new IntVec3(2,0,2));carrier.SetFaction(Faction.OfPirates);
                Check(HulkUtility.ForceHostileCarryRage(p,carrier)&&HulkUtility.HasActiveRage(p)&&HulkUtility.IsTransformed(p),"Hulk hostile carry triggers emergency rage");
                settings.enableForcedHulkRevert=true;AccessTools.Field(typeof(HediffComp_HulkIdentity),"rageTicksRemaining").SetValue(identity,2);carrier.Destroy();Ticks(3);Check(!HulkUtility.IsTransformed(p),"Hulk forced rage timer expires and reverts");settings.enableForcedHulkRevert=false;
            });
            Test("Hulk resurrection",()=>{p.Kill(null);Ticks(1);Check(!p.Dead&&p.Spawned&&HulkUtility.IsTransformed(p),"Hulk death triggers emergency resurrection and transformation");var identity=HulkUtility.GetIdentityHediff(p).TryGetComp<HediffComp_HulkIdentity>();AccessTools.Field(typeof(HediffComp_HulkIdentity),"rageTicksRemaining").SetValue(identity,0);identity.TickRage();HulkUtility.Revert(p);});
            Test("Hulk Green Door",()=>{
                HulkImmortalityManager.SuppressImmediateResurrectionForNextDeath(p);p.Kill(null);Check(p.Dead,"Hulk Green Door fixture retains corpse");p.Corpse.Destroy();
                Check(HulkGreenDoorManager.HasScheduledReturn(p),"Destroyed Hulk corpse schedules Green Door return");Check(HulkGreenDoorManager.TryForceManifestNow(p),"Hulk Green Door manifests through native manager");Ticks(60);
                Check(!p.Dead&&p.Spawned,"Hulk returns from Green Door");Ticks(240);results.Add("INFO GreenDoor state "+HulkGreenDoorManager.GetDebugStateSummary(p)+" rage="+HulkUtility.HasActiveRage(p)+" hediffs="+string.Join(",",p.health.hediffSet.hediffs.Select(h=>h.def.defName)));Write();Check(!HulkUtility.IsTransformed(p)&&p.health.hediffSet.HasHediff(HulkDefOf.Drake_HulkRecoveryComa),"Hulk Green Door return ends in recovery coma");
                p.health.RemoveHediff(p.health.hediffSet.GetFirstHediffOfDef(HulkDefOf.Drake_HulkRecoveryComa));
            });
        }
        void GeneticsChecks()
        {
            foreach(var name in new[]{"Gambit_XGene","MM_MultipleManGene"})Test("Genetics "+name,()=>{
                var geneDef=DefDatabase<GeneDef>.GetNamed(name);var donor=Make("GeneDonor"+name,center+new IntVec3(30,0,24));donor.drafter.Drafted=false;donor.genes.AddGene(geneDef,false);
                var extractor=(Building_GeneExtractor)ThingMaker.MakeThing(ThingDefOf.GeneExtractor);GenSpawn.Spawn(extractor,CellFinder.RandomClosewalkCellNear(donor.Position,map,3),map);extractor.TryGetComp<CompPowerTrader>().PowerOn=true;extractor.TryAcceptPawn(donor);
                Genepack pack;
                if(geneDef.biostatArc>0){Check(!extractor.GetDirectlyHeldThings().Contains(donor),name+" retains native archite extraction restriction");pack=(Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);pack.Initialize(new List<GeneDef>{geneDef});}
                else {Check(extractor.GetDirectlyHeldThings().Contains(donor),name+" carrier accepted by native gene extractor");Call(extractor,"Finish");pack=map.listerThings.ThingsOfDef(ThingDefOf.Genepack).OfType<Genepack>().FirstOrDefault(g=>g.GeneSet.GenesListForReading.Contains(geneDef));Check(pack!=null&&!donor.Dead,name+" extraction creates native genepack");}
                var recipient=Make("GeneReceiver"+name,center+new IntVec3(30,0,30));var body=recipient.story.bodyType;var head=recipient.story.headType;
                var xenogerm=(Xenogerm)ThingMaker.MakeThing(ThingDefOf.Xenogerm);xenogerm.Initialize(new List<Genepack>{pack},"Validation",null);GeneUtility.ImplantXenogermItem(recipient,xenogerm);
                Check(recipient.genes.HasActiveGene(geneDef)&&recipient.story.bodyType==body&&recipient.story.headType==head,name+" native xenogerm implantation retains recipient identity");
                if(name=="Gambit_XGene"){Check(recipient.abilities.GetAbility(GambitDefOf.Gambit_ThrowCard)!=null&&recipient.health.hediffSet.HasHediff(GambitDefOf.Gambit_DeckHediff),"Gambit implanted gene grants deck and abilities");recipient.genes.RemoveGene(recipient.genes.GetGene(geneDef));Check(!recipient.health.hediffSet.HasHediff(GambitDefOf.Gambit_DeckHediff)&&recipient.abilities.GetAbility(GambitDefOf.Gambit_ThrowCard)==null,"Gambit gene removal removes linked powers");}
                else Check(DupeUtility.PawnHasDupeAbility(recipient),"Multiple Man implanted gene grants duplication");extractor.Destroy();
            });
            Test("Multiple Man injector mode",()=>{var settings=DupesMod.Instance.Settings;settings.ForceInjectorMode=true;var pawn=Make("MultipleManFallback",center+new IntVec3(35,0,30));var item=(ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("MM_MultipleManInjector"));Call(item.GetComp<CompUsable>(),"UsedBy",pawn);Check(pawn.health.hediffSet.HasHediff(MMDefOf.MM_MultipleManGeneHediff)&&DupeUtility.PawnHasDupeAbility(pawn),"Multiple Man alternate injector mode grants hediff powers");settings.ForceInjectorMode=false;});
        }
        void ExtraMultipleManChecks()
        {
            var p=characters["MultipleMan"];var settings=DupesMod.Instance.Settings;float cooldown=settings.SummonCooldownHours;settings.SummonCooldownHours=0;
            if(!p.genes.HasActiveGene(MMDefOf.MM_MultipleManGene))p.genes.AddGene(MMDefOf.MM_MultipleManGene,true);
            Test("Multiple Man clone lifetime",()=>{
                DupeUtility.TrySummonDupe(p);var dupe=map.mapPawns.AllPawnsSpawned.First(q=>DupeManager.Current.IsRegisteredDupe(q));DupeManager.Current.TryGetData(dupe,out var data);
                AccessTools.Field(typeof(DupeLifecycleData),"expirationTick").SetValue(data,Find.TickManager.TicksGame-1);dupe.drafter.Drafted=false;dupe.jobs.StopAll();data.Tick();Check(dupe.Destroyed,"Multiple Man undrafted duplicate expires");
                DupeUtility.TrySummonDupe(p);dupe=map.mapPawns.AllPawnsSpawned.First(q=>DupeManager.Current.IsRegisteredDupe(q));DupeManager.Current.TryGetData(dupe,out data);AccessTools.Field(typeof(DupeLifecycleData),"expirationTick").SetValue(data,Find.TickManager.TicksGame-1);dupe.drafter.Drafted=true;data.Tick();Check(!dupe.Destroyed,"Multiple Man drafted duplicate retains lifetime");DupeManager.Current.ForceDespawn(data,DupeDespawnReason.Manual);
                int cap=settings.MaxActiveDupes;settings.MaxActiveDupes=1;DupeUtility.TrySummonDupe(p);Check(!DupeManager.Current.CanSummon(p,out _),"Multiple Man duplicate cap enforced");
                dupe=map.mapPawns.AllPawnsSpawned.First(q=>DupeManager.Current.IsRegisteredDupe(q));
                var skill=dupe.skills.GetSkill(SkillDefOf.Melee);float xp=skill.xpSinceLastLevel;bool xpSetting=settings.SkillXpGainEnabled;settings.SkillXpGainEnabled=false;skill.Learn(100);Check(skill.xpSinceLastLevel==xp,"Multiple Man clone XP permission enforced");settings.SkillXpGainEnabled=xpSetting;
                float speed=settings.MoveSpeedMultiplier;settings.MoveSpeedMultiplier=1;float baseSpeed=dupe.GetStatValue(StatDefOf.MoveSpeed,cacheStaleAfterTicks:0);settings.MoveSpeedMultiplier=2;float doubled=dupe.GetStatValue(StatDefOf.MoveSpeed,cacheStaleAfterTicks:0);Check(doubled>baseSpeed*1.9f,"Multiple Man move-speed multiplier changes clone stat");settings.MoveSpeedMultiplier=speed;
                dupe.Kill(null);Check(dupe.Destroyed,"Multiple Man duplicate death erases clone");settings.MaxActiveDupes=cap;
            });settings.SummonCooldownHours=cooldown;
        }
        void StormDefenseChecks()
        {
            Test("Storm defensive powers",()=>{
                var p=characters["Storm"];var home=p.Position;Move(p,arena);float injury=p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity);
                new WeatherEvent_LightningStrike(map,p.Position).FireEvent();Check(p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity)==injury,"Storm immune during real lightning event");
                var shooter=Make("WindShooter",arena+new IntVec3(7,0,0));shooter.SetFaction(Faction.OfPirates);var weapon=(ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Gun_Autopistol"));shooter.equipment.AddEquipment(weapon);
                var verb=weapon.GetComp<CompEquippable>().PrimaryVerb;AccessTools.Field(typeof(Verb),"currentTarget").SetValue(verb,new LocalTargetInfo(p));
                map.GetComponent<StormGodComponent>().SummonStorm(60000);float chance=StormSettings.deflectionChance;StormSettings.deflectionChance=1;
                Check(!(bool)Call(verb,"TryCastShot"),"Storm wind deflection rejects incoming projectile at configured certainty");StormSettings.deflectionChance=chance;shooter.Destroy();Move(p,home);
            });
        }
        string PawnState(Pawn p)
        {
            string extra="";
            if(p.Name.ToStringShort=="Cyclops")extra=CyclopsUtility.GetController(p).Strain.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            if(p.Name.ToStringShort=="Gambit")extra=p.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff).TryGetComp<HediffComp_GambitDeck>().Cards.ToString();
            if(p.Name.ToStringShort=="Magneto")extra=p.health.hediffSet.GetFirstHediffOfDef(MagnetoDefOf.Magneto_Magnetism).TryGetComp<HediffComp_Magnetism>().GetDirectlyHeldThings().Count.ToString();
            if(p.Name.ToStringShort=="Nightcrawler"){TeleportUtility.TryGetTeleportComp(p,out var c);extra=c.TeleportEnabled.ToString();}
            return p.ThingID+"\t"+p.Name.ToStringShort+"\t"+string.Join(",",p.health.hediffSet.hediffs.Select(h=>h.def.defName).OrderBy(s=>s))+"\t"+p.abilities.abilities.Count+"\t"+extra;
        }
        void PrepareSave()
        {
            AccessTools.Field(typeof(HediffComp_OpticController),"strain").SetValue(CyclopsUtility.GetController(characters["Cyclops"]),43f);
            var gambit=characters["Gambit"].health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff).TryGetComp<HediffComp_GambitDeck>();AccessTools.Field(typeof(HediffComp_GambitDeck),"cards").SetValue(gambit,37);
            TeleportUtility.TryGetTeleportComp(characters["Nightcrawler"],out var teleport);teleport.SetTeleportEnabled(true);
            var magneto=characters["Magneto"];var steel=ThingMaker.MakeThing(ThingDefOf.Steel);GenSpawn.Spawn(steel,CellFinder.RandomClosewalkCellNear(magneto.Position,map,2),map);Call(magneto.health.hediffSet.GetFirstHediffOfDef(MagnetoDefOf.Magneto_Magnetism).TryGetComp<HediffComp_Magnetism>(),"TryGrabThing",steel);
            HulkUtility.SetCustomHulkSkinColor(characters["Hulk"],new Color(.2f,.4f,.6f));map.GetComponent<StormGodComponent>().SummonStorm(30000,WeatherDef.Named("Rain"));
            DupesMod.Instance.Settings.SummonCooldownHours=0;DupeUtility.TrySummonDupe(characters["MultipleMan"]);
            string[] names={"Cyclops","Deadpool","Gambit","Hulk","JeanGrey","Magneto","MultipleMan","Nightcrawler","Storm"};
            File.WriteAllLines(Path.Combine(SuiteStartup.Root,"StateExpected.tsv"),names.Select(n=>PawnState(characters[n])));
        }
        void ReloadChecks()
        {
            foreach(var line in File.ReadAllLines(Path.Combine(SuiteStartup.Root,"StateExpected.tsv")))
            {
                Test("Reloaded pawn",()=>{
                var fields=line.Split('\t');var pawn=map.mapPawns.AllPawns.FirstOrDefault(p=>p.ThingID==fields[0]);
                var actual=pawn==null?null:PawnState(pawn).Split('\t');
                bool equal=actual!=null&&fields.Take(4).SequenceEqual(actual.Take(4));
                if(equal&&fields[1]=="Cyclops")equal=Math.Abs(float.Parse(fields[4],System.Globalization.CultureInfo.InvariantCulture)-float.Parse(actual[4],System.Globalization.CultureInfo.InvariantCulture))<.5f;
                else if(equal)equal=fields[4]==actual[4];
                if(!equal){results.Add("INFO expected="+line+" actual="+(pawn==null?"missing":PawnState(pawn)));Write();}
                Check(equal,fields[1]+" identity, health, abilities and resource state survive native save/reload");
                });
            }
            Check(map.GetComponent<StormGodComponent>().isStormActive&&map.GetComponent<StormGodComponent>().targetWeather.defName=="Rain","Storm weather state survives save/reload");
            var hulk=map.mapPawns.AllPawns.First(p=>p.Name.ToStringShort=="Hulk");Check(HulkUtility.HasCustomHulkSkinColor(hulk),"Hulk per-pawn color survives save/reload");
            var prime=map.mapPawns.AllPawns.First(p=>p.Name.ToStringShort=="MultipleMan");Check(DupeManager.Current.HasActiveDupe(prime),"Multiple Man registered clone survives save/reload");
        }
        IEnumerator Run()
        {
            while (Current.Game == null || Find.CurrentMap == null || LongEventHandler.AnyEventNowOrWaiting) yield return null;
            if(SuiteStartup.Profile=="reload")
            {
                var previousGame=Current.Game;GameDataSaveLoader.LoadGame("Marvel-Validation");yield return null;
                while(ReferenceEquals(Current.Game,previousGame)||Current.Game==null||LongEventHandler.AnyEventNowOrWaiting||Find.CurrentMap==null)yield return null;
                Find.TickManager.Pause();map=Find.CurrentMap;Test("Native save reload",ReloadChecks);center=map.Center;arena=new IntVec3(45,0,45);KidnapChecks();Finish("RELOAD SUITE COMPLETE");yield break;
            }
            Find.TickManager.Pause(); map = Find.CurrentMap; center = map.Center;arena=new IntVec3(45,0,45);
            foreach(var pawn in map.mapPawns.AllPawnsSpawned.ToList())pawn.Destroy();
            ClearArena();
            Test("Assembly", () => {
                Check(LoadedModManager.RunningModsListForReading.Any(m => m.PackageIdPlayerFacing.Equals("drake.marvelunification", StringComparison.OrdinalIgnoreCase)), "Unified package active");
                Check(typeof(CyclopsGene.CyclopsGeneMod).Assembly.GetName().Name == "MarvelUnification", "Characters compiled into unified assembly");
                int count = 0;bool unique=true;
                foreach (var method in Harmony.GetAllPatchedMethods())
                {
                    var info = Harmony.GetPatchInfo(method);
                    foreach (var group in new[] { info.Prefixes, info.Postfixes, info.Transpilers, info.Finalizers })
                    {
                        var ours = group.Where(p => p.owner == "DrAke.MarvelUnification").ToList();
                        unique &= ours.GroupBy(p => p.PatchMethod).All(g => g.Count() == 1);
                        count += ours.Count;
                    }
                }
                Check(count > 20, "Unified Harmony patches present: " + count);
                Check(unique,"Every unified Harmony patch applied once");
                Check(ModsConfig.BiotechActive,"Required Biotech active");
                Check(CyclopsGene.CyclopsGeneMod.Settings != null && DeadpoolsHealingFactor.DeadpoolsHealingFactorMod.settings != null && GambitXGene.GambitMod.Settings != null && Drake.Hulk.IncredibleHulkMod.Settings != null && JeanGreyMod.JeanGreyMod.settings != null && MagnetoXGene.MagnetoMod.Settings != null && MultipleManXGene.DupesMod.Instance != null && Rimworld_Storm.StormMod.settings != null, "All eight original settings modules initialized");
            });
            string[] names = { "Cyclops", "Deadpool", "Gambit", "Hulk", "JeanGrey", "Magneto", "MultipleMan", "Nightcrawler", "Storm" };
            string[] powers = { "Cyclops_Gene", "DP_HealingFactor", "Gambit_XGene_Hediff", "Drake_HulkIdentity", "JG_PhoenixForce", "Magneto_Magnetism", "MM_MultipleManGeneHediff", "Nightcrawler_Teleportation", "Storm_Gene" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                Test(names[i] + " identity", () => {
                    var pawn = Make(names[index], center + new IntVec3((index % 3 - 1) * 18, 0, (index / 3 - 1) * 18));
                    var def = DefDatabase<HediffDef>.GetNamedSilentFail(powers[index]);
                    if (def == null) { results.Add("INFO Available hediffs: " + string.Join(",", DefDatabase<HediffDef>.AllDefsListForReading.Where(d => d.modContentPack?.PackageIdPlayerFacing.Equals("drake.marvelunification",StringComparison.OrdinalIgnoreCase) == true).Select(d => d.defName))); Write(); }
                    Check(def != null, names[index] + " power definition resolves");
                    if (names[index] == "Hulk") Drake.Hulk.HulkUtility.GrantGammaIdentity(pawn); else Add(pawn, powers[index]);
                    Check(pawn.health.hediffSet.HasHediff(def), names[index] + " receives original power");
                    Check(pawn.GetGizmos().ToList() != null, names[index] + " gizmos resolve");
                });
            }
            if(SuiteStartup.Selection=="duplication"){Test("Multiple Man",MultipleManChecks);Finish("DUPLICATION SUITE COMPLETE");yield break;}
            Test("Injectors and recipes",InjectorChecks);
            if(SuiteStartup.Selection=="full") {
                Test("Cyclops",CyclopsChecks);yield return null;
                Test("Gambit",GambitChecks);yield return null;
                Test("Hulk",HulkChecks);yield return null;
                Test("Magneto",MagnetoChecks);yield return null;
                Test("Multiple Man",MultipleManChecks);yield return null;
            }
            var nightcrawler=NightcrawlerChecks();while(nightcrawler.MoveNext())yield return nightcrawler.Current;
            if(SuiteStartup.Selection=="full") {
                Test("Storm",StormChecks);yield return null;
                Test("Jean Grey",JeanGreyChecks);yield return null;
            }
            Test("Deadpool",DeadpoolChecks);yield return null;
            Test("Hulk supporting systems",ExtraHulkChecks);yield return null;
            Test("Gene transfer",GeneticsChecks);yield return null;
            Test("Multiple Man lifecycle",ExtraMultipleManChecks);yield return null;
            Test("Storm defenses",StormDefenseChecks);yield return null;
            var phoenix=characters["JeanGrey"];Move(phoenix,arena);phoenix.Kill(null);yield return null;
            while(LongEventHandler.AnyEventNowOrWaiting)yield return null;
            Test("Phoenix resurrection",()=>Check(!phoenix.Dead&&phoenix.Spawned&&phoenix.health.hediffSet.HasHediff(HediffDef.Named("JG_PhoenixCooldown")),"Jean Grey death triggers Phoenix resurrection with cooldown"));
            foreach(var pair in characters.Where(c=>!c.Key.StartsWith("Recipient")).ToList())Test("Portrait "+pair.Key,()=>Portrait(pair.Value,pair.Key));
            var mods=new Mod[]{LoadedModManager.GetMod<CyclopsGeneMod>(),LoadedModManager.GetMod<DeadpoolsHealingFactorMod>(),LoadedModManager.GetMod<GambitMod>(),LoadedModManager.GetMod<IncredibleHulkMod>(),LoadedModManager.GetMod<JeanGreyMod.JeanGreyMod>(),LoadedModManager.GetMod<MagnetoMod>(),LoadedModManager.GetMod<DupesMod>(),LoadedModManager.GetMod<StormMod>()};
            foreach(var mod in mods){uiMod=mod;uiName=mod.SettingsCategory();var window=new SettingsTestWindow(mod);Find.WindowStack.Add(window);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(SuiteStartup.Root,"Settings-"+mod.GetType().Name+".png"));Check(!window.Failed,"Settings UI renders: "+uiName);Find.WindowStack.TryRemove(window);uiMod=null;mod.WriteSettings();}
            yield return null;
            Test("Persistent state fixture",PrepareSave);
            GameDataSaveLoader.SaveGame("Marvel-Validation");
            Check(File.Exists(Path.Combine(SuiteStartup.Root,"game-data","Saves","Marvel-Validation.rws")), "Real isolated game save written");
            Finish("SUITE COMPLETE");
        }
    }
    public sealed class SettingsTestWindow : Window
    {
        readonly Mod mod;
        public bool Failed;
        public SettingsTestWindow(Mod mod){this.mod=mod;forcePause=true;absorbInputAroundWindow=true;doCloseX=true;}
        public override Vector2 InitialSize=>new Vector2(950,900);
        public override void DoWindowContents(Rect rect){Widgets.Label(new Rect(0,0,rect.width,30),mod.SettingsCategory());try{mod.DoSettingsWindowContents(new Rect(0,35,rect.width,rect.height-35));}catch(Exception){Failed=true;}}
    }
}

