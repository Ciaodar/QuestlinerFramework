using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Questliner.Framework.Core
{
    public static class ConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("start_quest", "qlf")]
        public static string StartQuest(List<string> args)
        {
            if (args.Count < 1) return "Usage: qlf.start_quest [QuestId]";
            var id = args[0];
            var q = Questliner.Framework.Services.QuestModuleScanner.LoadedQuests.FirstOrDefault(x => x.Id == id);
            if (q == null) return "Quest not found in XMLs.";
            var dq = new DynamicQuest(q, TaleWorlds.CampaignSystem.Hero.MainHero, TaleWorlds.CampaignSystem.CampaignTime.DaysFromNow(q.TimeLimitInDays > 0 ? q.TimeLimitInDays : 9999));
            foreach(var res in q.TargetResolvers)
            {
                if (res.TargetType == "Settlement")
                {
                    var s = Questliner.Framework.Engine.TargetResolverService.ResolveSettlement(res, dq);
                    if (s != null) dq.TargetSettlements[res.Alias] = s;
                }
                else if (res.TargetType == "MobileParty")
                {
                    var p = Questliner.Framework.Engine.TargetResolverService.ResolveMobileParty(res, dq);
                    if (p != null) dq.TargetParties[res.Alias] = p;
                }
                else
                {
                    var h = Questliner.Framework.Engine.TargetResolverService.ResolveHero(res, dq);
                    if (h != null) dq.TargetHeroes[res.Alias] = h;
                }
            }
            dq.StartQuest();
            return $"Quest {id} started.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("list_active", "qlf")]
        public static string ListQuests(List<string> args)
        {
            var quests = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList();
            if (quests.Count == 0) return "No active QLF quests.";
            string res = "Active QLF Quests:\n";
            foreach (var q in quests) res += $"- {q.Model.Id} (Stage: {q.CurrentStageId})\n";
            return res;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("complete_stage", "qlf")]
        public static string CompleteStage(List<string> args)
        {
            if (args.Count < 1) return "Usage: qlf.complete_stage [QuestId]";
            var id = args[0];
            var q = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>()
                .FirstOrDefault(x => x.IsOngoing && string.Equals(x.Model?.Id, id, System.StringComparison.OrdinalIgnoreCase));
            if (q == null) return $"Active quest '{id}' not found.";
            
            string oldStage = q.CurrentStageId;
            q.ForceCompleteCurrentStage();
            return $"Quest {id} advanced from {oldStage} to {q.CurrentStageId}.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("jump_stage", "qlf")]
        public static string JumpStage(List<string> args)
        {
            if (args.Count < 2) return "Usage: qlf.jump_stage [QuestId] [StageId]";
            var id = args[0];
            var stageId = args[1];
            var q = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>()
                .FirstOrDefault(x => x.IsOngoing && string.Equals(x.Model?.Id, id, System.StringComparison.OrdinalIgnoreCase));
            if (q == null) return $"Active quest '{id}' not found.";

            var targetStageModel = q.Model?.Stages.FirstOrDefault(s => string.Equals(s.Id, stageId, System.StringComparison.OrdinalIgnoreCase));
            if (targetStageModel == null) return $"Stage '{stageId}' not found in quest '{id}'.";

            string oldStage = q.CurrentStageId;
            q.ChangeStage(targetStageModel);
            return $"Quest '{id}' jumped from '{oldStage}' to '{q.CurrentStageId}'.";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("force_success", "qlf")]
        public static string ForceSuccess(List<string> args)
        {
            if (args.Count < 1) return "Usage: qlf.force_success [QuestId]";
            var id = args[0];
            var q = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>()
                .FirstOrDefault(x => x.IsOngoing && string.Equals(x.Model?.Id, id, System.StringComparison.OrdinalIgnoreCase));
            if (q == null) return $"Active quest '{id}' not found.";
            
            q.ForceSuccess();
            return $"Forced success for {id}.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("force_fail", "qlf")]
        public static string ForceFail(List<string> args)
        {
            if (args.Count < 1) return "Usage: qlf.force_fail [QuestId]";
            var id = args[0];
            var q = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>()
                .FirstOrDefault(x => x.IsOngoing && string.Equals(x.Model?.Id, id, System.StringComparison.OrdinalIgnoreCase));
            if (q == null) return $"Active quest '{id}' not found.";

            q.ForceFail();
            if (q.IsOngoing)
            {
                q.CompleteQuestWithFail();
            }
            return $"Forced fail for {id}.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("print_targets", "qlf")]
        public static string PrintTargets(List<string> args)
        {
            if (args.Count < 1) return "Usage: qlf.print_targets [QuestId]";
            var id = args[0];
            var q = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>()
                .FirstOrDefault(x => x.IsOngoing && string.Equals(x.Model?.Id, id, System.StringComparison.OrdinalIgnoreCase));
            if (q == null) return $"Active quest '{id}' not found.";

            var sb = new StringBuilder();
            sb.AppendLine($"=== Targets for Quest '{id}' (Stage: {q.CurrentStageId}) ===");

            sb.AppendLine("[Heroes]");
            if (q.TargetHeroes == null || q.TargetHeroes.Count == 0)
            {
                sb.AppendLine("  (None)");
            }
            else
            {
                foreach (var kvp in q.TargetHeroes)
                {
                    var h = kvp.Value;
                    if (h == null)
                    {
                        sb.AppendLine($"  - [{kvp.Key}]: (null)");
                        continue;
                    }
                    string location = h.CurrentSettlement != null ? $"Settlement: {h.CurrentSettlement.Name}"
                                    : (h.StayingInSettlement != null ? $"Settlement: {h.StayingInSettlement.Name}"
                                    : (h.PartyBelongedTo != null ? $"Party: {h.PartyBelongedTo.Name}"
                                    : (h.PartyBelongedToAsPrisoner != null ? $"Prisoner of: {h.PartyBelongedToAsPrisoner.Name}"
                                    : "Field / Unknown")));
                    sb.AppendLine($"  - [{kvp.Key}] {h.Name} ({h.StringId}) | Alive: {h.IsAlive} | HP: {h.HitPoints} | Location: {location}");
                }
            }

            sb.AppendLine("[Settlements]");
            if (q.TargetSettlements == null || q.TargetSettlements.Count == 0)
            {
                sb.AppendLine("  (None)");
            }
            else
            {
                foreach (var kvp in q.TargetSettlements)
                {
                    var s = kvp.Value;
                    if (s == null)
                    {
                        sb.AppendLine($"  - [{kvp.Key}]: (null)");
                        continue;
                    }
                    string clanName = s.OwnerClan?.Name?.ToString() ?? "None";
                    string kingdomName = s.OwnerClan?.Kingdom?.Name?.ToString() ?? (s.MapFaction as Kingdom)?.Name?.ToString() ?? "None";
                    sb.AppendLine($"  - [{kvp.Key}] {s.Name} ({s.StringId}) | Clan: {clanName} | Kingdom: {kingdomName}");
                }
            }

            sb.AppendLine("[Parties]");
            if (q.TargetParties == null || q.TargetParties.Count == 0)
            {
                sb.AppendLine("  (None)");
            }
            else
            {
                foreach (var kvp in q.TargetParties)
                {
                    var p = kvp.Value;
                    if (p == null)
                    {
                        sb.AppendLine($"  - [{kvp.Key}]: (null)");
                        continue;
                    }
                    int count = p.MemberRoster != null ? p.MemberRoster.TotalManCount : 0;
                    var pos = p.GetPosition2D;
                    sb.AppendLine($"  - [{kvp.Key}] {p.Name} ({p.StringId}) | Men: {count} | Pos: ({pos.X:F1}, {pos.Y:F1}) | Active: {p.IsActive}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("set_flag", "qlf")]
        public static string SetFlag(List<string> args)
        {
            if (args.Count < 2) return "Usage: qlf.set_flag [FlagName] [1/0]";
            var flag = args[0];
            bool val = args[1] == "1" || args[1].ToLower() == "true";
            GlobalStateManager.Instance.SetFlag(flag, val);
            return $"Global Flag '{flag}' set to {val}.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("get_flags", "qlf")]
        public static string GetFlags(List<string> args)
        {
            var flags = GlobalStateManager.Instance.GlobalFlags;
            if (flags == null || flags.Count == 0) return "No active global flags.";
            return "Active Global Flags:\n" + string.Join("\n", flags.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("list_global_heroes", "qlf")]
        public static string ListGlobalHeroes(List<string> args)
        {
            var heroes = GlobalStateManager.Instance.GlobalHeroes;
            if (heroes == null || heroes.Count == 0) return "No stored global heroes.";
            return "Stored Global Heroes:\n" + string.Join("\n", heroes.Select(kvp => $"- {kvp.Key}: {kvp.Value?.Name} ({kvp.Value?.StringId})"));
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("reset_completed", "qlf")]
        public static string ResetCompleted(List<string> args)
        {
            if (args.Count < 1) return "Usage: qlf.reset_completed [QuestId]";
            var id = args[0];
            Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.RemoveCompletedQuest(id);
            return $"Quest {id} removed from completed quests.";
        }
    }
}
