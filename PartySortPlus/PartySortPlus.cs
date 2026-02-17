using Dalamud.Plugin;
using ECommons.SimpleGui;
using ECommons.Configuration;
using PartySortPlus.Configuration;
using ECommons.Automation.LegacyTaskManager;
using ECommons;
using ECommons.DalamudServices;
using System;
using ECommons.Schedulers;
using ECommons.Logging;
using System.IO.Compression;
using System.IO;
using PartySortPlus.GUI;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace PartySortPlus;

/**
 * TODO LIST: 
 * - Add in usage of base classes for jobs when sorting.
 * - Figure out drag and drop issue.
 */
public unsafe class PartySortPlus: IDalamudPlugin
{
    public static PartySortPlus? P;
    public static Config? C;

    public TaskManager? TaskManager;

    public bool SoftForceUpdate = false;
    public bool ForceUpdate = false;

    public PartySortPlus(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this, ECommons.Module.DalamudReflector);

        _ = new TickScheduler(() =>
        {
            C = EzConfig.Init<Config>();
            var ver = GetType().Assembly.GetName().Version?.ToString();
            if (C != null && C.LastVersion != ver)
            {
                try
                {
                    using (var fs = new FileStream(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, $"Backup_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.zip"), FileMode.Create))
                    using (var arch = new ZipArchive(fs, ZipArchiveMode.Create))
                    {
                        arch.CreateEntryFromFile(EzConfig.DefaultConfigurationFileName, EzConfig.DefaultSerializationFactory.DefaultConfigFileName);
                    }
                    C.LastVersion = ver ?? string.Empty; // Ensure non-null assignment
                    DuoLog.Information($"Because plugin version was changed, a backup of your current configuraton has been created.");
                }
                catch (Exception e)
                {
                    e.Log();
                }
            }

            EzConfigGui.Init(UI.DrawMain);
            EzCmd.Add("/psp", OnCommand, "Run a party list sort!"
                + "\n/psp ui → Show the configuration window."
                + "\n/psp config → Show the configuration window."
                + "\n/psp tutorial → Show the tutorial window again."
            );

            new EzFrameworkUpdate(OnUpdate);
            new EzTerritoryChanged(TerritoryChanged);
            TaskManager = new TaskManager()
            {
                TimeLimitMS = 2000,
                AbortOnTimeout = true,
                TimeoutSilently = false,
            };
        });
    }

    public void Dispose()
    {
        ECommonsMain.Dispose();
        P = null;
        C = null;
    }

    private void TerritoryChanged(ushort id)
    {
        SoftForceUpdate = true;
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.EqualsIgnoreCaseAny("debug"))
        {
            if (C != null)
            {
                C.Debug = !C.Debug;
                DuoLog.Information($"Debug mode is now {(C.Debug ? "enabled" : "disabled")}");
            }
        } 
        else if(arguments.EqualsIgnoreCaseAny("ui") || arguments.EqualsIgnoreCaseAny("config"))
        {
            EzConfigGui.Window.IsOpen ^= true;
        }
        else if(arguments.EqualsIgnoreCaseAny("tutorial"))
        {
            if (C != null)
            {
                C.ShowTutorial = !C.ShowTutorial;
                DuoLog.Information($"Tutorial mode is now {(C.ShowTutorial ? "enabled" : "disabled")}");
            }
        }
        else
        {
            ForceUpdate = true;
        }
    }

    private void OnUpdate()
    {
        if (C == null) return;
        if (TaskManager == null) return;

        if (Player.Interactable)
        {
            if (!TaskManager.IsBusy)
            {
                List<ApplyRule> newRule = [];
                if (C.Enable)
                {
                    foreach (var r in C.GlobalProfile.Rules)
                    {
                        if(r.Enabled)
                        {
                            var territories = r.Territories;
                            var jobs = r.Jobs;
                            var partyJobs = r.PartyJobs;
                            if (C.Cond_Enabled && C.Cond_Territory && C.Cond_Roles && C.Cond_Jobs && C.Cond_PartyJobs)
                            {
                                if (C.AllowNegativeConditions)
                                {
                                    if (r.Not.Territories.Count > 0 || r.Not.Jobs.Count > 0 || r.Not.PartyJobs.Count > 0)
                                    {
                                        newRule.Add(r);
                                    }
                                }
                                else
                                {
                                    newRule.Add(r);
                                }
                            }
                        }
                    }
                    if (ForceUpdate || (SoftForceUpdate && newRule.Count > 0))
                    {
                        SoftForceUpdate = false;
                        ForceUpdate = false;
                        PluginLog.Debug($"Force updating party list with {newRule.Count} rules.");

                        PluginLog.Debug($"Current territory: {Player.Territory}");
                        PluginLog.Debug($"Current job: {Player.Job}");

                        foreach (ref var partyMember in AgentHUD.Instance()->PartyMembers)
                        {
                            if (partyMember.Object == null) { continue; }
                            PluginLog.Debug($"Current party member: {partyMember.Name}");
                            PluginLog.Debug($"Current party member job: {partyMember.Object->ClassJob}");
                        }

                        foreach (var rule in newRule)
                        {
                            bool skipTerritoryCheck = rule.Territories.Count == 0;
                            bool skipJobCheck = rule.Jobs.Count == 0;
                            bool skipPartyJobCheck = rule.PartyJobs.Count == 0;

                            PluginLog.Debug($"Checking rule: {rule.GUID}");

                            if (skipTerritoryCheck) { PluginLog.Debug($"Territory check skipped."); }
                            if (skipJobCheck) { PluginLog.Debug($"Job check skipped."); }
                            if (skipPartyJobCheck) { PluginLog.Debug($"Party job check skipped."); }

                            if (!skipTerritoryCheck)
                            {
                                if (!rule.Territories.Contains(Svc.ClientState.TerritoryType))
                                {
                                    PluginLog.Debug($"Territory check failed.");
                                    continue;
                                }
                                PluginLog.Debug($"Territory check passed.");
                            }

                            if (!skipJobCheck)
                            {
                                if (!rule.Jobs.Contains(Player.Job))
                                {
                                    PluginLog.Debug($"Job check failed.");
                                    continue;
                                }
                                PluginLog.Debug($"Job check passed.");
                            }

                            if (!skipPartyJobCheck)
                            {
                                var partyJobs = GetPartyMemberJobs();
                                bool allRuleJobsInParty = true;

                                foreach (var requiredJob in rule.PartyJobs)
                                {
                                    if (!partyJobs.Contains(requiredJob.ToString()))
                                    {
                                        allRuleJobsInParty = false;
                                        PluginLog.Debug($"Party job check failed: required job {requiredJob} not present.");
                                        break;
                                    }
                                }

                                if (!allRuleJobsInParty)
                                {
                                    continue;
                                }

                                PluginLog.Debug($"Party job check passed.");
                            }

                            if (rule.SelectedPresets.Count == 0)
                            {
                                PluginLog.Error($"Rule {rule.GUID} has no selected presets.");
                                continue;
                            }

                            string presetNameToUse = rule.SelectedPresets[0];
                            int presetIndex = C.GlobalProfile.Presets.FindIndex(x => x.Name.EqualsIgnoreCase(presetNameToUse));

                            if (presetIndex == -1)
                            {
                                PluginLog.Error($"Preset '{presetNameToUse}' not found for rule {rule.GUID}.");
                                continue;
                            }

                            try
                            {
                                Preset presetToUse = C.GlobalProfile.Presets[presetIndex];
                                SortPartyList(presetToUse);
                                PluginLog.Information($"Processed rule {rule.GUID}. All other rules skipped.");
                            }
                            catch (Exception ex)
                            {
                                PluginLog.Error($"Exception while processing rule {rule.GUID}: {ex.Message}");
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    private void SortPartyList(Preset presetToUse)
    {
        PluginLog.Information($"Sorting party list with preset {presetToUse.Name}.");

        int partyCount = (int)InfoProxyPartyMember.Instance()->GetEntryCount();

        var currentJobsSnapshot = GetPartyMemberJobs();
        var used = new bool[partyCount];
        var targetOrder = new List<string>();

        foreach (var job in presetToUse.JobOrder)
        {
            for (int i = 0; i < partyCount; i++)
            {
                if (!used[i] && currentJobsSnapshot[i].Equals(job.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    targetOrder.Add(currentJobsSnapshot[i]);
                    used[i] = true;
                    break;
                }
            }
        }

        for (int i = 0; i < partyCount; i++)
        {
            if (!used[i])
            {
                targetOrder.Add(currentJobsSnapshot[i]);
            }
        }

        PluginLog.Debug($"Target job order: {string.Join(", ", targetOrder)}");
        PluginLog.Debug($"Current job order: {string.Join(", ", currentJobsSnapshot)}");

        for (int i = 0; i < targetOrder.Count; i++)
        {
            var currentJobs = GetPartyMemberJobs();
            var currentIndices = GetPartyMemberJobsByIndex();

            if (currentJobs[i].Equals(targetOrder[i], StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int swapIndex = -1;
            for (int j = i + 1; j < partyCount; j++)
            {
                if (currentJobs[j].Equals(targetOrder[i], StringComparison.OrdinalIgnoreCase))
                {
                    swapIndex = j;
                    break;
                }
            }

            if (swapIndex == -1)
            {
                continue;
            }

            PluginLog.Information($"Swapping {currentJobs[swapIndex]} (index {currentIndices[swapIndex]}) into position {i}");
            InfoProxyPartyMember.Instance()->ChangeOrder(currentIndices[swapIndex], i);
        }
    }

    private List<string> GetPartyMemberJobs()
    {
        var members = new List<(int Index, string Job)>();

        foreach (ref var partyMember in AgentHUD.Instance()->PartyMembers)
        {
            if (partyMember.Object != null)
            {
                var job = ECommons.ExcelServices.ExcelJobHelper.GetJobById(partyMember.Object->ClassJob);
                if (job.HasValue)
                {
                    members.Add((partyMember.Index, job.Value.Abbreviation.ToString()));
                }
            }
        }

        members.Sort((a, b) => a.Index.CompareTo(b.Index));

        return members.Select(m => m.Job).ToList();
    }

    private List<int> GetPartyMemberJobsByIndex()
    {
        var indices = new List<int>();

        foreach (ref var partyMember in AgentHUD.Instance()->PartyMembers)
        {
            if (partyMember.Object != null)
            {
                indices.Add(partyMember.Index);
            }
        }

        indices.Sort();
        return indices;
    }
}
