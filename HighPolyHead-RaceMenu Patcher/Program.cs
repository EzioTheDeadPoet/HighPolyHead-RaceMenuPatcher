using System;
using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using System.Threading.Tasks;
using Noggog;

namespace HighPolyHeadUpdateRaces
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "High Poly Head - RaceMenu Patcher.esp")
                .Run(args);
        }
        
        public static readonly ModKey ModKey = ModKey.FromNameAndExtension("High Poly Head.esm");

        private static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LoadOrder.ContainsKey(ModKey))
            {
                throw new Exception("You need High Poly Head mod installed for this patch to do anything.");
            }
            // Dictionary containing correlation between vanilla headparts to the HPH equivalent
            var vanillaToHphParts = new Dictionary<IFormLinkGetter<IHeadPartGetter>, IFormLinkGetter<IHeadPartGetter>>();
            // Dictionary of the Race record headparts NPCs inherit that need replacing in the presets
            var raceHeadPartsMale = new Dictionary<IFormLinkGetter<IRaceGetter>, HashSet<IFormLinkGetter<IHeadPartGetter>>>();
            var raceHeadPartsFemale = new Dictionary<IFormLinkGetter<IRaceGetter>, HashSet<IFormLinkGetter<IHeadPartGetter>>>();
            
            // List of Brow headparts
            
            foreach (var hphHeadPart in state.LoadOrder.PriorityOrder.OnlyEnabled().HeadPart().WinningOverrides())
            {
                if (hphHeadPart.EditorID == null || !hphHeadPart.EditorID.StartsWith("00KLH_")) continue;
                // for each HPH record, loop through the vanilla ones again - seems a bit inefficient? compare two lists with LINQ instead?
                foreach (var vanillaHeadPart in state.LoadOrder.PriorityOrder.HeadPart().WinningOverrides())
                {
                    if (vanillaHeadPart.EditorID != null && hphHeadPart.EditorID.EndsWith(vanillaHeadPart.EditorID) 
                                                         && !vanillaHeadPart.EditorID.StartsWith("00KLH_") )
                    {
                        if (!vanillaToHphParts.ContainsKey(vanillaHeadPart.ToLinkGetter()))
                        {
                            vanillaToHphParts[vanillaHeadPart.ToLinkGetter()] = hphHeadPart.ToLinkGetter();
                        }
                        IHeadPart gimmeHead = state.PatchMod.HeadParts.GetOrAddAsOverride(vanillaHeadPart);
                        gimmeHead.Flags &= ~HeadPart.Flag.Playable;
                    }
                }
            }

            foreach (var raceRecord in state.LoadOrder.PriorityOrder.OnlyEnabled().Race().WinningOverrides())
            {
                if (raceRecord.EditorID == null)
                {
                    continue;
                }
                if (raceRecord.HeadData == null)
                {
                    continue;
                }
                var hasMaleOverride = false;
                var hasFemaleOverride = false;
                if (raceRecord.HeadData.Male != null)
                {
                    // male first
                    foreach (var raceHead in raceRecord.HeadData.Male.HeadParts)
                    {
                        if (!raceHead.Head.TryResolve(state.LinkCache, out var head2)) continue;
                        if (vanillaToHphParts.ContainsKey(head2.ToLinkGetter()))
                        {
                            hasMaleOverride = true;
                        }
                    }
                }
                if (raceRecord.HeadData.Female != null)
                {
                    foreach (var raceHead in raceRecord.HeadData.Female.HeadParts)
                    {
                        if (!raceHead.Head.TryResolve(state.LinkCache, out var head2)) continue;
                        if (vanillaToHphParts.ContainsKey(head2.ToLinkGetter()))
                        {
                            hasFemaleOverride = true;
                        }
                    }
                }
                if(!hasFemaleOverride && !hasMaleOverride)
                {
                    continue;
                }
                
                var raceOverride = raceRecord.DeepCopy();
                bool changed = false;

                if (raceOverride.HeadData != null )
                {
                    var raceFormLinkGetter = raceOverride.ToLinkGetter();
                    if( raceOverride.HeadData.Female != null)
                    {
                        foreach (var raceHead in raceOverride.HeadData.Female.HeadParts)
                        {
                            if (!raceHead.Head.TryResolve(state.LinkCache, out var head2)) continue;
                            if (!vanillaToHphParts.TryGetValue(head2.ToLinkGetter(), out var part)) continue;
                            raceHeadPartsFemale.GetOrAdd(raceFormLinkGetter).Add(head2.ToLinkGetter());
                            changed = true;
                            raceHead.Head.SetTo(part);
                        }
                    }
                    if (raceOverride.HeadData.Male != null)
                    {
                        foreach (var raceHead in raceOverride.HeadData.Male.HeadParts)
                        {
                            if (!raceHead.Head.TryResolve(state.LinkCache, out var head2)) continue;
                            if (!vanillaToHphParts.TryGetValue(head2.ToLinkGetter(), out var part)) continue;
                            raceHeadPartsMale.GetOrAdd(raceFormLinkGetter).Add(head2.ToLinkGetter());
                            changed = true;
                            raceHead.Head.SetTo(part);
                        }
                    }
                }
                if( changed)
                {
                    state.PatchMod.Races.Set(raceOverride);
                }

            }
            // Now NPC records for preset defaults
            // by now you can tell ive given up on efficiency and just wanted to get the damn thing working
            foreach(var npcPreset in state.LoadOrder.PriorityOrder.OnlyEnabled().Npc().WinningOverrides())
            {
                if (npcPreset.EditorID == null) continue;
                var eid = npcPreset.EditorID;
                if(eid.Length <= 3)
                {
                    continue;
                }
                var withoutLastTwo = eid.Substring(0, eid.Length - 2);

                if (!withoutLastTwo.EndsWith("Preset"))
                {
                    
                    

                    var changed = false;
                    HashSet<Type> npcPartTypes = new HashSet<Type>();

                    var npcDeepCopy = npcPreset.DeepCopy();
                    
                    for (var index = 0; index < npcDeepCopy.HeadParts.Count; index++)
                    {
                        npcPartTypes.Add(npcDeepCopy.HeadParts[index].Type);
                    }

                    IReadOnlyList<IHeadPartReferenceGetter> raceHeadParts;

                    if (!npcDeepCopy.Race.TryResolve(state.LinkCache, out var race)) continue;
                    if (npcDeepCopy.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                    { 
                        if (race.HeadData?.Female?.HeadParts == null) continue;
                        raceHeadParts = race.HeadData.Female.HeadParts;
                    }
                    else
                    {
                        if (race.HeadData?.Male?.HeadParts == null) continue;
                        raceHeadParts = race.HeadData.Male.HeadParts;
                    }
                        
                    foreach (var part in raceHeadParts)
                    {
                        if (npcPartTypes.Contains(part.Head.Type))
                        {
                            npcDeepCopy.HeadParts.Add(part.Head);
                        }
                    }

                    if (changed)
                    {
                        state.PatchMod.Npcs.Set(npcDeepCopy);
                    }
                }

                if (withoutLastTwo.EndsWith("Preset"))
                {
                    INpc npcOverride = state.PatchMod.Npcs.GetOrAddAsOverride(npcPreset);
                    for (var index = 0; index < npcOverride.HeadParts.Count; index++)
                    {
                        if (!vanillaToHphParts.TryGetValue(npcOverride.HeadParts[index], out var replacementHead))
                        {
                            continue;
                        }
                        npcOverride.HeadParts[index] = replacementHead;
                    }
                }
            }
        }
    }
}