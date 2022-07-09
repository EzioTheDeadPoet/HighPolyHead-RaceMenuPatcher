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
            var raceHphPartsMale = new Dictionary<IFormLinkGetter<IRaceGetter>, HashSet<IFormLinkGetter<IHeadPartGetter>>>();
            var raceHphPartsFemale = new Dictionary<IFormLinkGetter<IRaceGetter>, HashSet<IFormLinkGetter<IHeadPartGetter>>>();
            
            // List of Brow headparts
            var browHeadPartList = new HashSet<IFormLinkGetter<IHeadPartGetter>>();
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
                            if (hphHeadPart.EditorID.ToUpper().Contains("BROWS"))
                            {
                                browHeadPartList.Add(hphHeadPart.ToLinkGetter());
                            }
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

                if (raceRecord.HeadData != null )
                {
                    var raceFormLinkGetter = raceRecord.ToLinkGetter();
                    if( raceRecord.HeadData.Female != null)
                    {
                        foreach (var raceHead in raceRecord.HeadData.Female.HeadParts)
                        {
                            if (!raceHead.Head.TryResolve(state.LinkCache, out var head2)) continue;
                            if (!vanillaToHphParts.TryGetValue(head2.ToLinkGetter(), out var parts)) continue;
                            raceHphPartsFemale.GetOrAdd(raceFormLinkGetter).Add(parts);
                        }
                    }
                    if (raceRecord.HeadData.Male != null)
                    {
                        foreach (var raceHead in raceRecord.HeadData.Male.HeadParts)
                        {
                            if (!raceHead.Head.TryResolve(state.LinkCache, out var head2)) continue;
                            if (!vanillaToHphParts.TryGetValue(head2.ToLinkGetter(), out var parts)) continue;
                            raceHphPartsMale.GetOrAdd(raceFormLinkGetter).Add(parts);
                        }
                    }
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

                if (withoutLastTwo.EndsWith("Preset"))
                {
                    var hasBrows = false;
                    INpc npcOverride = state.PatchMod.Npcs.GetOrAddAsOverride(npcPreset);
                    for (var index = 0; index < npcOverride.HeadParts.Count; index++)
                    {
                        if (!vanillaToHphParts.TryGetValue(npcOverride.HeadParts[index], out var replacementHead))
                        {
                            continue;
                        }
                        IFormLinkGetter<IHeadPartGetter> resolvedHead = replacementHead;
                        npcOverride.HeadParts[index] = resolvedHead;
                        hasBrows = browHeadPartList.Contains(npcOverride.HeadParts[index]);
                    }
                    var raceHphParts = new Dictionary<IFormLinkGetter<IRaceGetter>, HashSet<IFormLinkGetter<IHeadPartGetter>>>();
                    
                    raceHphParts = npcOverride.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female)
                        ? raceHphPartsFemale
                        : raceHphPartsMale;
                        

                    if (!raceHphParts.TryGetValue(npcOverride.Race, out var parts))
                    {
                        continue;
                    }
                    foreach (var part in parts)
                    {
                        if (hasBrows)
                        {
                            if (!browHeadPartList.Contains(part))
                            {
                                npcOverride.HeadParts.Add(part);
                            }
                        }
                        else
                        {
                            npcOverride.HeadParts.Add(part);
                        }
                    }
                }
            }
        }
    }
}