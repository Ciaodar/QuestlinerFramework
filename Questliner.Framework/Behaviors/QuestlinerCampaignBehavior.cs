using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using Questliner.Framework.Core;
using Questliner.Shared.Models;

namespace Questliner.Framework.Behaviors
{
    public class QuestlinerCampaignBehavior : CampaignBehaviorBase
    {
        public static QuestlinerCampaignBehavior? Instance { get; private set; }
        public System.Collections.Generic.List<string> CompletedQuestIds => _completedQuestIds;

        public void MarkQuestAsCompleted(string questId)
        {
            if (!_completedQuestIds.Contains(questId))
            {
                _completedQuestIds.Add(questId);
            }
        }
        public override void RegisterEvents()
        {
            Instance = this;
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private QuestModel? _pendingQuestToStart = null;
        private Dictionary<string, Hero> _pendingResolvedHeroes = new Dictionary<string, Hero>();
        private Dictionary<string, Settlement> _pendingResolvedSettlements = new Dictionary<string, Settlement>();
        private List<DynamicQuest> _questsStartedInThisConversation = new List<DynamicQuest>();
        private string? _currentConversationQuestId = null;
        
        [TaleWorlds.SaveSystem.SaveableField(10)]
        private System.Collections.Generic.List<string> _completedQuestIds = new System.Collections.Generic.List<string>();

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Inject conversation options to start XML quests
            starter.AddPlayerLine("ql_quest_start", "hero_main_options", "ql_quest_giver_response", 
                "{=ql_dialog_start}I am looking for a task. Do you have any work for me?", 
                CheckQuestStartCondition, 
                null);

            starter.AddDialogLine("ql_quest_giver_response", "ql_quest_giver_response", "close_window", 
                "{=ql_dialog_response}Indeed I do. Let's begin.", 
                null, 
                StartMatchingQuest);

            // Inject Custom Game Menus
            foreach (var questModel in Questliner.Framework.Services.QuestModuleScanner.LoadedQuests)
            {
                foreach (var menuOption in questModel.GameMenuOptions)
                {
                    bool MenuCondition(TaleWorlds.CampaignSystem.GameMenus.MenuCallbackArgs args)
                    {
                        args.optionLeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType.Continue;
                        if (menuOption.IsLeave)
                        {
                            args.optionLeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType.Leave;
                        }

                        foreach(var condition in menuOption.Conditions)
                        {
                            if (!Engine.ConditionEvaluator.IsConditionMet(condition, null))
                                return false;
                        }
                        return true;
                    }

                    void MenuConsequence(TaleWorlds.CampaignSystem.GameMenus.MenuCallbackArgs args)
                    {
                        foreach(var action in menuOption.Consequences)
                        {
                            Engine.ActionExecutor.Execute(action, null);
                        }
                    }

                    starter.AddGameMenuOption(menuOption.TargetMenu, menuOption.Id, menuOption.Text, MenuCondition, MenuConsequence, menuOption.IsLeave);
                }
            }

            // Inject handover/discuss dialogue for ONGOING quest stages
            starter.AddPlayerLine("ql_quest_discuss", "hero_main_options", "ql_quest_discuss_response", 
                "{=ql_dialog_discuss}About the task...", 
                DiscussCondition, 
                null);

            starter.AddDialogLine("ql_quest_discuss_response", "ql_quest_discuss_response", "ql_quest_discuss_options", 
                "{=ql_dialog_discuss_npc}Do you have what I asked for?", 
                null, 
                null);

            starter.AddPlayerLine("ql_quest_discuss_yes", "ql_quest_discuss_options", "ql_quest_discuss_finish", 
                "{=ql_dialog_discuss_yes}Yes, I have completed the preparations.", 
                HandoverCondition, 
                HandoverConsequence);

            starter.AddDialogLine("ql_quest_discuss_finish", "ql_quest_discuss_finish", "close_window", 
                "{=ql_dialog_discuss_finish}Excellent work. Let us proceed.", 
                null, 
                null);

            starter.AddPlayerLine("ql_quest_discuss_no", "ql_quest_discuss_options", "hero_main_options", 
                "{=ql_dialog_discuss_no}Not yet, I need more time.", 
                null, 
                null);

            // INJECT INFINITE DYNAMIC CUSTOM DIALOGUES FROM ALL LOADED QUESTS
            foreach (var questModel in Questliner.Framework.Services.QuestModuleScanner.LoadedQuests)
            {
                foreach (var dialogNode in questModel.CustomDialogs)
                {
                    string globalDialogId = $"ql_custom_{questModel.Id}_{dialogNode.Id}";
                    string textId = $"{{=ql_custom_{questModel.Id}_{dialogNode.Id}_text}}{dialogNode.Text}";

                    // Capture iteration variables for lambda closures safely
                    var capturedQuestModel = questModel;
                    var capturedDialogNode = dialogNode;

                    int nodePriority = dialogNode.Priority > 0 ? dialogNode.Priority : 100;

                    if (dialogNode.Type == "Player")
                    {
                        starter.AddPlayerLine(globalDialogId, dialogNode.InputToken, dialogNode.OutputToken, textId,
                            () => DynamicDialogCondition(capturedQuestModel, capturedDialogNode),
                            () => DynamicDialogConsequence(capturedQuestModel, capturedDialogNode),
                            nodePriority,
                            (out TaleWorlds.Localization.TextObject explanation) => DynamicClickableCondition(capturedQuestModel, capturedDialogNode, out explanation));
                    }
                    else
                    {
                        starter.AddDialogLine(globalDialogId, dialogNode.InputToken, dialogNode.OutputToken, textId,
                            () => DynamicDialogCondition(capturedQuestModel, capturedDialogNode),
                            () => DynamicDialogConsequence(capturedQuestModel, capturedDialogNode),
                            nodePriority);
                    }
                }
            }
        }

        private bool DynamicDialogCondition(QuestModel questModel, CustomDialogModel dialogNode)
        {
            // TaleWorlds.Library.Debug.Print($"[Questliner] EVALUATING {dialogNode.Id} for Quest {questModel.Id}");
            
            // Set global text variables for dialog substitution
            if (GlobalStateManager.Instance.GlobalHeroes != null)
            {
                foreach (var kvp in GlobalStateManager.Instance.GlobalHeroes)
                {
                    if (kvp.Value != null)
                    {
                        DynamicQuest.BindHeroToMBTextManager(kvp.Key, kvp.Value);
                    }
                }
            }

            if (Hero.MainHero != null)
            {
                DynamicQuest.BindHeroToMBTextManager("Player", Hero.MainHero);
                DynamicQuest.BindHeroToMBTextManager("PLAYER", Hero.MainHero);
                TaleWorlds.Localization.MBTextManager.SetTextVariable("PlayerName", Hero.MainHero.Name);
                TaleWorlds.Localization.MBTextManager.SetTextVariable("PLAYER_NAME", Hero.MainHero.Name);
            }

            // Find ongoing dynamic quest matching QuestId (may be null if quest hasn't started yet)
            var quest = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(q => q.Model?.Id == questModel.Id && q.IsOngoing);

            // If this quest has already been completed, do not re-evaluate its dialogs
            if (quest == null && _completedQuestIds.Contains(questModel.Id))
            {
                return false;
            }

            // Internal token scoping: prevents cross-quest leakage
            if (dialogNode.InputToken.StartsWith("ql_"))
            {
                if (!string.IsNullOrEmpty(_currentConversationQuestId) && _currentConversationQuestId != questModel.Id)
                {
                    return false;
                }

                if (quest == null && _currentConversationQuestId != questModel.Id)
                {
                    return false;
                }
            }

            if (quest == null && questModel.Prerequisites != null && questModel.Prerequisites.Count > 0)
            {
                foreach (var prereq in questModel.Prerequisites)
                {
                    if (!Engine.ConditionEvaluator.IsConditionMet(prereq, null)) return false;
                }
            }

            if (quest != null)
            {
                quest.InjectVariablesToTextManager();
            }

            Hero speaker = Hero.OneToOneConversationHero;
            
            // Generic party logic (Bandits, minor factions, etc)
            var cp = TaleWorlds.CampaignSystem.Party.MobileParty.ConversationParty;
            bool isQuestParty = false;
            
            if (quest != null && cp != null)
            {
                isQuestParty = quest.TargetParties.Values.Contains(cp);
            }

            if (cp != null && (cp.IsBandit || isQuestParty))
            {
                bool matchedAlias = false;
                if (isQuestParty && quest != null)
                {
                    var aliasKvp = quest.TargetParties.FirstOrDefault(x => x.Value == cp);
                    if (aliasKvp.Key == dialogNode.TargetAlias)
                    {
                        matchedAlias = true;
                    }
                }
                
                if (!matchedAlias && cp.IsBandit)
                {
                    string factionId = cp.MapFaction?.StringId?.ToLower() ?? "";
                    string aliasLower = (dialogNode.TargetAlias ?? "").ToLower().Replace("_", "").Replace(" ", "");
                    string factionLower = factionId.Replace("_", "").Replace(" ", "");
                    if (aliasLower == "banditparty" || aliasLower == factionLower)
                    {
                        matchedAlias = true;
                    }
                }

                if (!matchedAlias)
                {
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(dialogNode.SpeakerStringId))
            {
                if (TaleWorlds.CampaignSystem.CharacterObject.OneToOneConversationCharacter == null) return false;
                if (!TaleWorlds.CampaignSystem.CharacterObject.OneToOneConversationCharacter.StringId.Contains(dialogNode.SpeakerStringId))
                    return false;

                if (!string.IsNullOrEmpty(dialogNode.RequiredSettlementAlias))
                {
                    Settlement? reqSettlement = null;
                    if (quest != null)
                    {
                        reqSettlement = quest.GetTargetSettlement(dialogNode.RequiredSettlementAlias);
                    }
                    else
                    {
                        var resolver = questModel.TargetResolvers.FirstOrDefault(r => r.Alias == dialogNode.RequiredSettlementAlias);
                        if (resolver != null && Settlement.CurrentSettlement != null && Engine.TargetResolverService.IsSettlementMatch(resolver, Settlement.CurrentSettlement, null))
                        {
                            reqSettlement = Settlement.CurrentSettlement;
                        }
                    }

                    if (reqSettlement == null || Settlement.CurrentSettlement != reqSettlement)
                        return false;
                }
            }
            else
            {
                // Regular Hero targeting logic
                if (speaker == null) return false;
                
                // If it's entering a specific target conversation, verify it's the exact same hero
                if (!dialogNode.InputToken.StartsWith("ql_") && !string.IsNullOrEmpty(dialogNode.TargetAlias))
                {
                    if (quest != null)
                    {
                        var expectedHero = quest.GetTargetHero(dialogNode.TargetAlias);
                        // TaleWorlds.Library.Debug.Print($"[Questliner] Dialog: {dialogNode.Id} | Expected Hero for {dialogNode.TargetAlias}: {expectedHero?.Name} ({expectedHero?.StringId}) | Speaker: {speaker?.Name} ({speaker?.StringId})");
                        if (expectedHero != speaker) return false;
                    }
                    else
                    {
                        // It's a prequel dialog! The target hasn't been resolved into a quest yet.
                        var resolver = questModel.TargetResolvers.FirstOrDefault(r => r.Alias == dialogNode.TargetAlias);
                        if (resolver == null) 
                        {
                            // If there is no TargetResolver for this alias, it MUST be a generic map faction alias like "looters".
                            // Since we are talking to a Hero, they are not a map faction, so this is a mismatch!
                            return false;
                        }
                        
                        // If there is a resolver, we MUST check if the speaker hero matches the filters!
                        if (!Questliner.Framework.Engine.TargetResolverService.IsHeroMatch(resolver, speaker, null, null))
                        {
                            return false;
                        }
                    }
                }
            }

            // Check custom XML conditions
            if (!dialogNode.ShowWhenDisabled)
            {
                foreach (var cond in dialogNode.Conditions)
                {
                    if (!Questliner.Framework.Engine.ConditionEvaluator.IsConditionMet(cond, quest))
                        return false;
                }
            }

            return true;
        }

        private bool DynamicClickableCondition(QuestModel questModel, CustomDialogModel dialogNode, out TaleWorlds.Localization.TextObject explanation)
        {
            explanation = new TaleWorlds.Localization.TextObject("");

            if (dialogNode.ShowWhenDisabled)
            {
                var quest = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(q => q.Model?.Id == questModel.Id && q.IsOngoing);
                
                foreach (var cond in dialogNode.Conditions)
                {
                    if (!Questliner.Framework.Engine.ConditionEvaluator.IsConditionMet(cond, quest))
                    {
                        if (!string.IsNullOrEmpty(dialogNode.DisabledHint))
                        {
                            explanation = new TaleWorlds.Localization.TextObject(dialogNode.DisabledHint);
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        private void DynamicDialogConsequence(QuestModel questModel, CustomDialogModel dialogNode)
        {
            _currentConversationQuestId = questModel.Id;
            var quest = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(q => q.Model?.Id == questModel.Id && q.IsOngoing);
            foreach (var action in dialogNode.Consequences)
            {
                Questliner.Framework.Engine.ActionExecutor.Execute(action, quest);
            }

            if (dialogNode.OutputToken == "close_window")
            {
                _currentConversationQuestId = null;
            }
        }

        private bool CheckQuestStartCondition()
        {
            _pendingQuestToStart = null;
            _pendingResolvedHeroes.Clear();
            _pendingResolvedSettlements.Clear();

            Hero speaker = Hero.OneToOneConversationHero;
            if (speaker == null) return false;

            foreach (var questModel in Questliner.Framework.Services.QuestModuleScanner.LoadedQuests)
            {
                // Don't show option if quest is already completed
                if (_completedQuestIds.Contains(questModel.Id))
                    continue;

                // Don't show option if quest is already ongoing
                if (Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Any(q => q.Model?.Id == questModel.Id))
                    continue;

                // Check prerequisites before offering the quest
                if (questModel.Prerequisites != null && questModel.Prerequisites.Count > 0)
                {
                    bool prereqsMet = true;
                    foreach (var prereq in questModel.Prerequisites)
                    {
                        if (!Engine.ConditionEvaluator.IsConditionMet(prereq, null))
                        {
                            prereqsMet = false;
                            break;
                        }
                    }
                    if (!prereqsMet) continue;
                }

                var startTrigger = questModel.Triggers.FirstOrDefault(t => t.EventType == "OnConversation");
                if (startTrigger != null)
                {
                    var resolver = questModel.TargetResolvers.FirstOrDefault(r => r.Alias == startTrigger.TargetAlias);
                    if (resolver != null && Engine.TargetResolverService.IsHeroMatch(resolver, speaker, null))
                    {
                        // PRE-RESOLVE ALL TARGETS TO PREVENT BROKEN QUESTS!
                        bool canResolveAll = true;
                        var duration = TaleWorlds.CampaignSystem.CampaignTime.DaysFromNow(questModel.TimeLimitInDays > 0 ? questModel.TimeLimitInDays : 9999);
                        var tempQuest = new DynamicQuest(questModel, speaker, duration);
                        
                        foreach(var res in questModel.TargetResolvers)
                        {
                            if (res.Alias == startTrigger.TargetAlias)
                            {
                                tempQuest.TargetHeroes[res.Alias] = speaker;
                                continue;
                            }

                            if (res.TargetType == "Settlement")
                            {
                                var s = Engine.TargetResolverService.ResolveSettlement(res, tempQuest, speaker);
                                if (s == null && !res.IsOptional) 
                                { 
                                    canResolveAll = false; 
                                    break; 
                                }
                                if (s != null) tempQuest.TargetSettlements[res.Alias] = s;
                            }
                            else if (res.TargetType == "MobileParty")
                            {
                                var p = Engine.TargetResolverService.ResolveMobileParty(res, tempQuest, speaker);
                                if (p == null && !res.IsOptional) 
                                { 
                                    canResolveAll = false; 
                                    break; 
                                }
                                if (p != null) tempQuest.TargetParties[res.Alias] = p;
                            }
                            else
                            {
                                var h = Engine.TargetResolverService.ResolveHero(res, tempQuest, speaker);
                                if (h == null && !res.IsOptional) 
                                { 
                                    canResolveAll = false; 
                                    break; 
                                }
                                if (h != null) tempQuest.TargetHeroes[res.Alias] = h;
                            }
                        }

                        if (canResolveAll)
                        {
                            _pendingQuestToStart = questModel;
                            // Store the pre-resolved tempQuest so we don't recreate it
                            _pendingResolvedHeroes = tempQuest.TargetHeroes.ToDictionary(k => k.Key, v => v.Value);
                            _pendingResolvedSettlements = tempQuest.TargetSettlements.ToDictionary(k => k.Key, v => v.Value);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void StartMatchingQuest()
        {
            if (_pendingQuestToStart != null && Hero.OneToOneConversationHero != null)
            {
                var duration = CampaignTime.DaysFromNow(_pendingQuestToStart.TimeLimitInDays > 0 ? _pendingQuestToStart.TimeLimitInDays : 9999);
                var newQuest = new DynamicQuest(_pendingQuestToStart, Hero.OneToOneConversationHero, duration);
                
                // Inject the safely pre-resolved targets!
                foreach(var kvp in _pendingResolvedHeroes)
                {
                    newQuest.TargetHeroes[kvp.Key] = kvp.Value;
                }
                foreach(var kvp in _pendingResolvedSettlements)
                {
                    newQuest.TargetSettlements[kvp.Key] = kvp.Value;
                }
                
                _questsStartedInThisConversation.Add(newQuest);
                newQuest.StartQuest();
                _pendingQuestToStart = null;
                _pendingResolvedHeroes.Clear();
            _pendingResolvedSettlements.Clear();
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // DynamicQuest instances are natively synced by Campaign.Current.QuestManager.
            dataStore.SyncData("Questliner_CompletedQuests", ref _completedQuestIds);
            
            // Protect against null initialization from legacy saves
            if (_completedQuestIds == null)
            {
                _completedQuestIds = new List<string>();
            }

            // Sync global flags
            var globalFlags = GlobalStateManager.Instance.GlobalFlags;
            dataStore.SyncData("Questliner_GlobalFlags", ref globalFlags);
            GlobalStateManager.Instance.GlobalFlags = globalFlags ?? new Dictionary<string, bool>();

            // Sync global heroes
            var globalHeroes = GlobalStateManager.Instance.GlobalHeroes;
            dataStore.SyncData("Questliner_GlobalHeroes", ref globalHeroes);
            GlobalStateManager.Instance.GlobalHeroes = globalHeroes ?? new Dictionary<string, Hero>();
        }

        private DynamicQuest? _activeDiscussQuest = null;
        private string _activeDiscussTargetAlias = string.Empty;

        private bool DiscussCondition()
        {
            _activeDiscussQuest = null;
            _activeDiscussTargetAlias = string.Empty;

            Hero speaker = Hero.OneToOneConversationHero;
            if (speaker == null) return false;

            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                var alias = quest.TargetHeroes.FirstOrDefault(x => x.Value == speaker).Key;
                if (!string.IsNullOrEmpty(alias))
                {
                    var stage = quest.Model?.Stages.FirstOrDefault(s => s.Id == quest.CurrentStageId);
                    if (stage != null && stage.Conditions.Any(c => c.ConditionType == "TalkedTo" && c.TargetAlias == alias))
                    {
                        // Check if we already talked to them in this stage
                        if (quest.GetFlag($"TalkedTo_{alias}")) continue;

                        // Check if the modder provided a custom dialog for this stage and target, so we don't duplicate
                        bool hasCustomDialog = quest.Model?.CustomDialogs.Any(cd => 
                            cd.TargetAlias == alias &&
                            cd.InputToken == "hero_main_options" &&
                            cd.Conditions.Any(c => c.ConditionType == "IsStageActive" && c.ObjectId == quest.CurrentStageId)
                        ) ?? false;

                        if (hasCustomDialog) continue;

                        _activeDiscussQuest = quest;
                        _activeDiscussTargetAlias = alias;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HandoverCondition()
        {
            if (_activeDiscussQuest == null || string.IsNullOrEmpty(_activeDiscussQuest.CurrentStageId)) return false;
            var stage = _activeDiscussQuest.Model?.Stages.FirstOrDefault(s => s.Id == _activeDiscussQuest.CurrentStageId);
            if (stage == null) return false;

            // Only show "Yes" if all non-TalkedTo conditions are actually met
            foreach (var c in stage.Conditions)
            {
                if (c.ConditionType == "TalkedTo" && c.TargetAlias == _activeDiscussTargetAlias) continue;
                
                if (!Questliner.Framework.Engine.ConditionEvaluator.IsConditionMet(c, _activeDiscussQuest))
                {
                    return false;
                }
            }
            return true;
        }

        private void HandoverConsequence()
        {
            if (_activeDiscussQuest != null && !string.IsNullOrEmpty(_activeDiscussTargetAlias))
            {
                // Explicitly set the TalkedTo flag ONLY when the player chooses the "Yes, I have it" dialog option.
                _activeDiscussQuest.SetFlag($"TalkedTo_{_activeDiscussTargetAlias}", true);
                _activeDiscussQuest.CheckStageProgression();
            }
        }

        private void OnConversationEnded(IEnumerable<CharacterObject> characters)
        {
            // Removed automatic blind-fire TalkedTo flag setting.
            // TalkedTo is now strictly driven by the "About the task..." dialogue option.
            
            if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null && TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.LeaveEncounter)
            {
                TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Finish(true);
                if (Campaign.Current.CurrentMenuContext != null)
                {
                    TaleWorlds.CampaignSystem.GameMenus.GameMenu.ExitToLast();
                }
            }

            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                quest.CheckStageProgression();
            }

            _questsStartedInThisConversation.Clear();
            _currentConversationQuestId = null;
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty) return;
            
            // 1. Process Ongoing Quests
            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                var targetAlias = quest.TargetSettlements.FirstOrDefault(x => x.Value == settlement).Key;
                if (!string.IsNullOrEmpty(targetAlias))
                {
                    quest.SetFlag($"Entered_{targetAlias}", true);
                }
                quest.CheckStageProgression();
            }

            // 2. Check for OnSettlementEnter Triggers
            foreach (var questModel in Questliner.Framework.Services.QuestModuleScanner.LoadedQuests)
            {
                if (_completedQuestIds.Contains(questModel.Id))
                    continue;

                if (Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Any(q => q.Model?.Id == questModel.Id))
                    continue; // Zaten aktif

                var trigger = questModel.Triggers.FirstOrDefault(t => t.EventType == "OnSettlementEnter");
                if (trigger != null)
                {
                    var resolver = questModel.TargetResolvers.FirstOrDefault(r => r.Alias == trigger.TargetAlias);
                    if (resolver != null && Engine.TargetResolverService.IsSettlementMatch(resolver, settlement, null))
                    {
                        var duration = TaleWorlds.CampaignSystem.CampaignTime.DaysFromNow(questModel.TimeLimitInDays > 0 ? questModel.TimeLimitInDays : 9999);
                        var tempQuest = new DynamicQuest(questModel, Hero.MainHero, duration);
                        tempQuest.TargetSettlements[trigger.TargetAlias] = settlement;
                        
                        bool canResolveAll = true;
                        foreach (var res in questModel.TargetResolvers)
                        {
                            if (res.Alias == trigger.TargetAlias) continue;
                            
                            if (res.TargetType == "Settlement")
                            {
                                var s = Engine.TargetResolverService.ResolveSettlement(res, tempQuest, Hero.MainHero);
                                if (s == null && !res.IsOptional) { canResolveAll = false; break; }
                                if (s != null) tempQuest.TargetSettlements[res.Alias] = s;
                            }
                            else if (res.TargetType == "MobileParty")
                            {
                                var p = Engine.TargetResolverService.ResolveMobileParty(res, tempQuest, Hero.MainHero);
                                if (p == null && !res.IsOptional) { canResolveAll = false; break; }
                                if (p != null) tempQuest.TargetParties[res.Alias] = p;
                            }
                            else
                            {
                                var h = Engine.TargetResolverService.ResolveHero(res, tempQuest, Hero.MainHero);
                                if (h == null && !res.IsOptional) { canResolveAll = false; break; }
                                if (h != null) tempQuest.TargetHeroes[res.Alias] = h;
                            }
                        }

                        if (canResolveAll)
                        {
                            bool reqsMet = true;
                            foreach (var req in questModel.Prerequisites)
                            {
                                if (!Engine.ConditionEvaluator.IsConditionMet(req, tempQuest))
                                {
                                    reqsMet = false;
                                    break;
                                }
                            }

                            if (reqsMet)
                            {
                                tempQuest.StartQuest();
                            }
                        }
                    }
                }
            }
        }

        private void OnHeroKilled(Hero victim, Hero killer, TaleWorlds.CampaignSystem.Actions.KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                var targetAlias = quest.TargetHeroes.FirstOrDefault(x => x.Value == victim).Key;
                if (!string.IsNullOrEmpty(targetAlias))
                {
                    quest.SetFlag($"Killed_{targetAlias}", true);
                }
            }
            EvaluateAllQuests();
        }

        private void OnHeroPrisonerTaken(PartyBase captor, Hero prisoner)
        {
            EvaluateAllQuests();
        }


        private void OnMapEventEnded(TaleWorlds.CampaignSystem.MapEvents.MapEvent mapEvent)
        {
            if (mapEvent == null) return;
            var involvedParties = mapEvent.InvolvedParties.ToList();

            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                // Native specific defeat counters
                if (mapEvent.IsPlayerMapEvent && mapEvent.WinningSide == mapEvent.PlayerSide)
                {
                    var defeatedParties = mapEvent.InvolvedParties.Where(p => p.Side != mapEvent.PlayerSide);
                    foreach(var dp in defeatedParties)
                    {
                        var cultureId = dp.MobileParty?.Party?.Culture?.StringId;
                        if (!string.IsNullOrEmpty(cultureId) && cultureId != "looters")
                        {
                            quest.IncrementCounter($"Defeated_{cultureId}_Parties", 1);
                        }

                        if (dp.MobileParty != null && (dp.MobileParty.IsBandit || cultureId == "looters"))
                        {
                            quest.IncrementCounter("Defeated_looters_Parties", 1);
                            quest.IncrementCounter("Defeated_BanditParty_Parties", 1);
                        }
                    }
                }

                // Questliner framework TargetParty battle result tracking
                foreach (var targetPartyKvp in quest.TargetParties)
                {
                    var alias = targetPartyKvp.Key;
                    var targetParty = targetPartyKvp.Value;
                    
                    if (targetParty != null && targetParty.Party != null && involvedParties.Contains(targetParty.Party))
                    {
                        string result = "";
                        bool isPlayerInvolved = involvedParties.Contains(PartyBase.MainParty);
                        
                        if (isPlayerInvolved)
                        {
                            if (mapEvent.WinningSide == mapEvent.PlayerSide)
                            {
                                result = "Victory";
                            }
                            else if (mapEvent.WinningSide == TaleWorlds.Core.BattleSideEnum.None)
                            {
                                result = "Retreat";
                            }
                            else
                            {
                                result = "Defeat";
                            }
                        }
                        else
                        {
                            var targetSide = mapEvent.AttackerSide.Parties.Any(p => p.Party == targetParty.Party) ? TaleWorlds.Core.BattleSideEnum.Attacker : TaleWorlds.Core.BattleSideEnum.Defender;
                            if (mapEvent.WinningSide != TaleWorlds.Core.BattleSideEnum.None && mapEvent.WinningSide != targetSide)
                            {
                                result = "DestroyedByAI";
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            quest.SetStringVariable($"BattleResult_{alias}", result);
                            if (result == "Victory" && isPlayerInvolved)
                            {
                                quest.IncrementCounter($"Defeated_{alias}_Parties", 1);
                            }
                        }
                    }
                }

                quest.CheckStageProgression();
            }
            
            EvaluateAllQuests();
        }

        
        private void OnClanChangedKingdom(TaleWorlds.CampaignSystem.Clan clan, TaleWorlds.CampaignSystem.Kingdom oldKingdom, TaleWorlds.CampaignSystem.Kingdom newKingdom, TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            if (clan == TaleWorlds.CampaignSystem.Clan.PlayerClan)
            {
                EvaluateAllQuests();
            }
        }

        private void OnHourlyTick()
        {
            EvaluateAllQuests();

            // Check for AutoStart quests
            foreach (var questModel in Questliner.Framework.Services.QuestModuleScanner.LoadedQuests)
            {
                if (_completedQuestIds.Contains(questModel.Id))
                    continue;

                if (Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Any(q => q.Model?.Id == questModel.Id))
                    continue;

                if (questModel.StartType == "AutoStart" || questModel.Triggers.Any(t => t.EventType == "AutoStart"))
                {
                    TryAutoStartQuest(questModel);
                }
            }
        }

        public void TryAutoStartQuest(QuestModel questModel)
        {
            if (_completedQuestIds.Contains(questModel.Id)) return;
            if (Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Any(q => q.Model?.Id == questModel.Id))
                return;

            var duration = TaleWorlds.CampaignSystem.CampaignTime.DaysFromNow(questModel.TimeLimitInDays > 0 ? questModel.TimeLimitInDays : 9999);
            var tempQuest = new DynamicQuest(questModel, Hero.MainHero, duration); // AutoStart quests default to MainHero as giver if no other giver is specified, but TargetResolvers should assign proper heroes.
            
            bool canResolveAll = true;
            foreach (var res in questModel.TargetResolvers)
            {
                if (res.TargetType == "Settlement")
                {
                    var s = Engine.TargetResolverService.ResolveSettlement(res, tempQuest, Hero.MainHero);
                    if (s == null && !res.IsOptional) { canResolveAll = false; break; }
                    if (s != null) tempQuest.TargetSettlements[res.Alias] = s;
                }
                else if (res.TargetType == "MobileParty")
                {
                    var p = Engine.TargetResolverService.ResolveMobileParty(res, tempQuest, Hero.MainHero);
                    if (p == null && !res.IsOptional) { canResolveAll = false; break; }
                    if (p != null) tempQuest.TargetParties[res.Alias] = p;
                }
                else
                {
                    var h = Engine.TargetResolverService.ResolveHero(res, tempQuest, Hero.MainHero);
                    if (h == null && !res.IsOptional) { canResolveAll = false; break; }
                    if (h != null) tempQuest.TargetHeroes[res.Alias] = h;
                }
            }

            if (canResolveAll)
            {
                bool reqsMet = true;
                foreach (var req in questModel.Prerequisites)
                {
                    if (!Engine.ConditionEvaluator.IsConditionMet(req, tempQuest))
                    {
                        reqsMet = false;
                        break;
                    }
                }

                if (reqsMet)
                {
                    tempQuest.StartQuest();
                }
            }
        }

        private void EvaluateAllQuests()
        {
            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                quest.CheckStageProgression();
            }
        }
    }
}
