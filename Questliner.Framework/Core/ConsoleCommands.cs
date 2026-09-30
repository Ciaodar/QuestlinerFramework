using System.Collections.Generic;
using System.Linq;
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
            var q = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(x => x.IsOngoing && x.Model.Id == id);
            if (q == null) return "Active quest not found.";
            
            string oldStage = q.CurrentStageId;
            q.ForceCompleteCurrentStage();
            return $"Quest {id} advanced from {oldStage} to {q.CurrentStageId}.";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("force_success", "qlf")]
        public static string ForceSuccess(List<string> args)
        {
            if (args.Count < 1) return "Usage: qlf.force_success [QuestId]";
            var id = args[0];
            var q = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(x => x.IsOngoing && x.Model.Id == id);
            if (q == null) return "Active quest not found.";
            
            q.ForceSuccess();
            return $"Forced success for {id}.";
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
    }
}
