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
        public static QuestlinerCampaignBehavior Instance { get; private set; }
        public System.Collections.Generic.List<string> CompletedQuestIds => _completedQuestIds;
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

                    if (dialogNode.Type == "Player")
                    {
                        starter.AddPlayerLine(globalDialogId, dialogNode.InputToken, dialogNode.OutputToken, textId,
                            () => DynamicDialogCondition(capturedQuestModel, capturedDialogNode),
                            () => DynamicDialogConsequence(capturedQuestModel, capturedDialogNode));
                    }
                    else
                    {
                        starter.AddDialogLine(globalDialogId, dialogNode.InputToken, dialogNode.OutputToken, textId,
                            () => DynamicDialogCondition(capturedQuestModel, capturedDialogNode),
                            () => DynamicDialogConsequence(capturedQuestModel, capturedDialogNode));
                    }
                }
            }
        }

        private bool DynamicDialogCondition(QuestModel questModel, CustomDialogModel dialogNode)
        {
            System.IO.File.AppendAllText("QuestlinerDebug.txt", $"[{System.DateTime.Now}] EVALUATING {dialogNode.Id} for Quest {questModel.Id}\n");
            // Set global text variables for dialog substitution
            TaleWorlds.Localization.MBTextManager.SetTextVariable("Player.Name", Hero.MainHero.Name);
            TaleWorlds.Localization.MBTextManager.SetTextVariable("PlayerName", Hero.MainHero.Name);

            // Find ongoing dynamic quest matching QuestId (may be null if quest hasn't started yet)
            var quest = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(q => q.Model.Id == questModel.Id && q.IsOngoing);

            if (quest == null && questModel.Prerequisites != null && questModel.Prerequisites.Count > 0)
            {
                foreach (var prereq in questModel.Prerequisites)
                {
                    if (!Engine.ConditionEvaluator.IsConditionMet(prereq, null)) return false;
                }
            }

            if (quest != null)
            {
                foreach (var kvp in quest.TargetHeroes)
                {
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + "_Link", kvp.Value.EncyclopediaLinkWithName);
                }
                foreach (var kvp in quest.TargetSettlements)
                {
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + "_Link", kvp.Value.EncyclopediaLinkWithName);
                }
            }

            Hero speaker = Hero.OneToOneConversationHero;
            
            // Generic party logic (Bandits, minor factions, etc)
            var cp = TaleWorlds.CampaignSystem.Party.MobileParty.ConversationParty;
            if (cp != null && cp.IsBandit)
            {
                string factionId = cp.MapFaction?.StringId?.ToLower() ?? "";
                string aliasLower = dialogNode.TargetAlias.ToLower().Replace("_", "").Replace(" ", "");
                string factionLower = factionId.Replace("_", "").Replace(" ", "");

                // If TargetAlias exactly matches the faction (e.g. "looters") or is "BanditParty" (any bandit)
                if (aliasLower == "banditparty" || aliasLower == factionLower)
                {
                    // Match! We proceed to condition checks.
                }
                else
                {
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(dialogNode.SpeakerStringId))
            {
                if (TaleWorlds.CampaignSystem.CharacterObject.OneToOneConversationCharacter == null) return false;
                if (!TaleWorlds.CampaignSystem.CharacterObject.OneToOneConversationCharacter.StringId.Contains(dialogNode.SpeakerStringId))
                    return false;

                // Only require an active quest if this is entering from an outside/native token
                if (!dialogNode.InputToken.StartsWith("ql_") && quest == null) return false;
                
                if (!string.IsNullOrEmpty(dialogNode.RequiredSettlementAlias) && quest != null)
                {
                    var reqSettlement = quest.GetTargetSettlement(dialogNode.RequiredSettlementAlias);
                    if (reqSettlement == null || TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement != reqSettlement)
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
                        System.IO.File.AppendAllText("QuestlinerDebug.txt", $"[{System.DateTime.Now}] Dialog: {dialogNode.Id} | Expected Hero for {dialogNode.TargetAlias}: {expectedHero?.Name} ({expectedHero?.StringId}) | Speaker: {speaker?.Name} ({speaker?.StringId})\n");
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
            foreach (var cond in dialogNode.Conditions)
            {
                if (!Questliner.Framework.Engine.ConditionEvaluator.IsConditionMet(cond, quest))
                    return false;
            }

            return true;
        }

        private void DynamicDialogConsequence(QuestModel questModel, CustomDialogModel dialogNode)
        {
            var quest = Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().FirstOrDefault(q => q.Model.Id == questModel.Id && q.IsOngoing);
            foreach (var action in dialogNode.Consequences)
            {
                Questliner.Framework.Engine.ActionExecutor.Execute(action, quest);
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
                // Don't show option if quest is already ongoing
                if (Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Any(q => q.Model.Id == questModel.Id))
                    continue;

                var startTrigger = questModel.Triggers.FirstOrDefault(t => t.EventType == "OnConversation");
                if (startTrigger != null)
                {
                    var resolver = questModel.TargetResolvers.FirstOrDefault(r => r.Alias == startTrigger.TargetAlias);
                    if (resolver != null && Engine.TargetResolverService.IsHeroMatch(resolver, speaker, null))
                    {
                        // PRE-RESOLVE ALL TARGETS TO PREVENT BROKEN QUESTS!
                        bool canResolveAll = true;
                        foreach(var res in questModel.TargetResolvers)
                        {
                            if (res.Alias == startTrigger.TargetAlias)
                            {
                                _pendingResolvedHeroes[res.Alias] = speaker;
                                continue;
                            }

                            if (res.TargetType == "Settlement")
                            {
                                var s = Engine.TargetResolverService.ResolveSettlement(res, null, speaker);
                                if (s == null) 
                                { 
                                    canResolveAll = false; 
                                    break; 
                                }
                                _pendingResolvedSettlements[res.Alias] = s;
                            }
                            else
                            {
                                var h = Engine.TargetResolverService.ResolveHero(res, null, speaker);
                                if (h == null) 
                                { 
                                    canResolveAll = false; 
                                    break; 
                                }
                                _pendingResolvedHeroes[res.Alias] = h;
                            }
                        }

                        if (canResolveAll)
                        {
                            _pendingQuestToStart = questModel;
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
            
            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                quest.CheckStageProgression();
            }

            _questsStartedInThisConversation.Clear();
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty) return;
            
            foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
            {
                var targetAlias = quest.TargetSettlements.FirstOrDefault(x => x.Value == settlement).Key;
                if (!string.IsNullOrEmpty(targetAlias))
                {
                    quest.SetFlag($"Entered_{targetAlias}", true);
                }
                quest.CheckStageProgression();
            }
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            EvaluateAllQuests();
        }

        private void OnHeroPrisonerTaken(PartyBase captor, Hero prisoner)
        {
            EvaluateAllQuests();
        }


        private void OnMapEventEnded(TaleWorlds.CampaignSystem.MapEvents.MapEvent mapEvent)
        {
            if (mapEvent.IsPlayerMapEvent) 
            {
                if (mapEvent.WinningSide == mapEvent.PlayerSide)
                {
                    var defeatedParties = mapEvent.InvolvedParties.Where(p => p.Side != mapEvent.PlayerSide);
                    foreach (var quest in Campaign.Current.QuestManager.Quests.OfType<DynamicQuest>().Where(q => q.IsOngoing).ToList())
                    {
                        foreach(var dp in defeatedParties)
                        {
                            if (dp.MobileParty != null && dp.MobileParty.Party != null && dp.MobileParty.Party.Culture != null)
                            {
                                quest.IncrementCounter($"Defeated_{dp.MobileParty.Party.Culture.StringId}_Parties", 1);
                            }
                        }
                    }
                }
                EvaluateAllQuests();
            }
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
