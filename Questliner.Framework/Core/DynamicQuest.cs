using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Questliner.Shared.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Questliner.Framework.Core
{
    public class DynamicQuest : QuestBase
    {
        public QuestModel Model { get; private set; }

        private void EnsureModelLoaded()
        {
            if (Model == null && !string.IsNullOrEmpty(_modelId))
            {
                Model = Questliner.Framework.Services.QuestModuleScanner.LoadedQuests.FirstOrDefault(q => q.Id == _modelId);
            }
        }


        
        // TaleWorlds hack: If quest giver is null, we MUST provide a SpecialQuestType string,
        // otherwise Native silently cancels/deletes the quest during daily ticks or serialization.
        public override string SpecialQuestType => QuestGiver == null ? "QuestlinerSpecialQuest" : base.SpecialQuestType;

        // Dynamic State Storage
        [SaveableField(1)]
        private string _currentStageId = string.Empty;
        public string CurrentStageId { get => _currentStageId; private set => _currentStageId = value; }
        public StageModel? CurrentStage => Model?.Stages.FirstOrDefault(s => s.Id == CurrentStageId);

        [SaveableField(2)]
        private Dictionary<string, Hero> _targetHeroes = new Dictionary<string, Hero>();
        public Dictionary<string, Hero> TargetHeroes
        {
            get
            {
                if (_targetHeroes == null) _targetHeroes = new Dictionary<string, Hero>();
                return _targetHeroes;
            }
        }

        [SaveableField(3)]
        private Dictionary<string, Settlement> _targetSettlements = new Dictionary<string, Settlement>();
        public Dictionary<string, Settlement> TargetSettlements
        {
            get
            {
                if (_targetSettlements == null) _targetSettlements = new Dictionary<string, Settlement>();
                return _targetSettlements;
            }
        }

        [SaveableField(8)]
        private Dictionary<string, TaleWorlds.CampaignSystem.Party.MobileParty> _targetParties = new Dictionary<string, TaleWorlds.CampaignSystem.Party.MobileParty>();
        public Dictionary<string, TaleWorlds.CampaignSystem.Party.MobileParty> TargetParties
        {
            get
            {
                if (_targetParties == null) _targetParties = new Dictionary<string, TaleWorlds.CampaignSystem.Party.MobileParty>();
                return _targetParties;
            }
        }

        [SaveableField(4)]
        private Dictionary<string, bool> _trackedFlags = new Dictionary<string, bool>();
        public Dictionary<string, bool> TrackedFlags
        {
            get
            {
                if (_trackedFlags == null) _trackedFlags = new Dictionary<string, bool>();
                return _trackedFlags;
            }
        }

        [TaleWorlds.SaveSystem.SaveableField(7)]
        private System.Collections.Generic.Dictionary<string, int> _trackedCounters = new System.Collections.Generic.Dictionary<string, int>();
        public System.Collections.Generic.Dictionary<string, int> TrackedCounters
        {
            get
            {
                if (_trackedCounters == null) _trackedCounters = new System.Collections.Generic.Dictionary<string, int>();
                return _trackedCounters;
            }
        }
        public int GetCounter(string key) => TrackedCounters.TryGetValue(key, out var val) ? val : 0;
        public void IncrementCounter(string key, int amount) => TrackedCounters[key] = GetCounter(key) + amount;
        public void SetCounter(string key, int amount) => TrackedCounters[key] = amount;

        [TaleWorlds.SaveSystem.SaveableField(9)]
        private System.Collections.Generic.Dictionary<string, string> _trackedStrings = new System.Collections.Generic.Dictionary<string, string>();
        public System.Collections.Generic.Dictionary<string, string> TrackedStrings
        {
            get
            {
                if (_trackedStrings == null) _trackedStrings = new System.Collections.Generic.Dictionary<string, string>();
                return _trackedStrings;
            }
        }
        public string GetString(string key) => TrackedStrings.TryGetValue(key, out var val) ? val : string.Empty;
        public void SetStringVariable(string key, string value) => TrackedStrings[key] = value;

        public static void BindHeroToMBTextManager(string key, Hero hero)
        {
            if (string.IsNullOrEmpty(key) || hero == null) return;
            var heroName = hero.Name ?? new TextObject(string.Empty);
            var heroLink = hero.EncyclopediaLinkWithName ?? heroName;

            TextObject? charProp = null;
            if (hero.CharacterObject != null)
            {
                charProp = StringHelpers.SetCharacterProperties(key, hero.CharacterObject);
            }

            if (charProp != null)
            {
                charProp.SetTextVariable("Name", heroName);
                charProp.SetTextVariable("name", heroName);
                charProp.SetTextVariable("NAME", heroName);
                charProp.SetTextVariable("Link", heroLink);
                charProp.SetTextVariable("link", heroLink);
                charProp.SetTextVariable("LINK", heroLink);
                charProp.SetTextVariable("Gender", hero.IsFemale ? 1 : 0);
                charProp.SetTextVariable("gender", hero.IsFemale ? 1 : 0);
                charProp.SetTextVariable("GENDER", hero.IsFemale ? 1 : 0);
            }
            else
            {
                var heroText = new TextObject(heroName.ToString());
                heroText.SetTextVariable("Name", heroName);
                heroText.SetTextVariable("name", heroName);
                heroText.SetTextVariable("NAME", heroName);
                heroText.SetTextVariable("Link", heroLink);
                heroText.SetTextVariable("link", heroLink);
                heroText.SetTextVariable("LINK", heroLink);
                heroText.SetTextVariable("Gender", hero.IsFemale ? 1 : 0);
                heroText.SetTextVariable("gender", hero.IsFemale ? 1 : 0);
                heroText.SetTextVariable("GENDER", hero.IsFemale ? 1 : 0);
                MBTextManager.SetTextVariable(key, heroText);
            }

            MBTextManager.SetTextVariable(key + ".Name", heroName);
            MBTextManager.SetTextVariable(key + ".name", heroName);
            MBTextManager.SetTextVariable(key + ".NAME", heroName);
            MBTextManager.SetTextVariable(key + "_Name", heroName);
            MBTextManager.SetTextVariable(key + "_name", heroName);
            MBTextManager.SetTextVariable(key + "_NAME", heroName);
            MBTextManager.SetTextVariable(key + "_Link", heroLink);
            MBTextManager.SetTextVariable(key + "_link", heroLink);
            MBTextManager.SetTextVariable(key + "_LINK", heroLink);
            MBTextManager.SetTextVariable(key + ".Link", heroLink);
            MBTextManager.SetTextVariable(key + ".link", heroLink);
            MBTextManager.SetTextVariable(key + ".LINK", heroLink);
            MBTextManager.SetTextVariable(key + "_Gender", hero.IsFemale ? 1 : 0);
            MBTextManager.SetTextVariable(key + "_gender", hero.IsFemale ? 1 : 0);
            MBTextManager.SetTextVariable(key + "_GENDER", hero.IsFemale ? 1 : 0);
        }

        public static void BindSettlementToMBTextManager(string key, Settlement settlement)
        {
            if (string.IsNullOrEmpty(key) || settlement == null) return;
            var settName = settlement.Name ?? new TextObject(string.Empty);
            var settLink = settlement.EncyclopediaLinkWithName ?? settName;

            var settText = new TextObject("{=!}" + settName.ToString());
            settText.SetTextVariable("Name", settName);
            settText.SetTextVariable("name", settName);
            settText.SetTextVariable("NAME", settName);
            settText.SetTextVariable("Link", settLink);
            settText.SetTextVariable("link", settLink);
            settText.SetTextVariable("LINK", settLink);
            MBTextManager.SetTextVariable(key, settText);

            MBTextManager.SetTextVariable(key + ".Name", settName);
            MBTextManager.SetTextVariable(key + ".name", settName);
            MBTextManager.SetTextVariable(key + ".NAME", settName);
            MBTextManager.SetTextVariable(key + "_Name", settName);
            MBTextManager.SetTextVariable(key + "_name", settName);
            MBTextManager.SetTextVariable(key + "_NAME", settName);
            MBTextManager.SetTextVariable(key + "_Link", settLink);
            MBTextManager.SetTextVariable(key + "_link", settLink);
            MBTextManager.SetTextVariable(key + "_LINK", settLink);
            MBTextManager.SetTextVariable(key + ".Link", settLink);
            MBTextManager.SetTextVariable(key + ".link", settLink);
            MBTextManager.SetTextVariable(key + ".LINK", settLink);
        }

        public static void BindPartyToMBTextManager(string key, TaleWorlds.CampaignSystem.Party.MobileParty party)
        {
            if (string.IsNullOrEmpty(key) || party == null) return;
            var pName = party.Name ?? new TextObject(party.StringId);
            var partyText = new TextObject(pName.ToString());
            partyText.SetTextVariable("Name", pName);
            partyText.SetTextVariable("name", pName);
            partyText.SetTextVariable("NAME", pName);
            partyText.SetTextVariable("Link", pName);
            partyText.SetTextVariable("link", pName);
            partyText.SetTextVariable("LINK", pName);

            MBTextManager.SetTextVariable(key, partyText);
            MBTextManager.SetTextVariable(key + ".Name", pName);
            MBTextManager.SetTextVariable(key + ".name", pName);
            MBTextManager.SetTextVariable(key + ".NAME", pName);
            MBTextManager.SetTextVariable(key + "_Name", pName);
            MBTextManager.SetTextVariable(key + "_name", pName);
            MBTextManager.SetTextVariable(key + "_NAME", pName);
            MBTextManager.SetTextVariable(key + "_Link", pName);
            MBTextManager.SetTextVariable(key + "_link", pName);
            MBTextManager.SetTextVariable(key + "_LINK", pName);
            MBTextManager.SetTextVariable(key + ".Link", pName);
            MBTextManager.SetTextVariable(key + ".link", pName);
            MBTextManager.SetTextVariable(key + ".LINK", pName);
        }

        public static void BindHeroToTextObject(TextObject textObject, string key, Hero hero)
        {
            if (textObject == null || string.IsNullOrEmpty(key) || hero == null) return;
            var heroName = hero.Name ?? new TextObject(string.Empty);
            var heroLink = hero.EncyclopediaLinkWithName ?? heroName;

            TextObject? charProp = null;
            if (hero.CharacterObject != null)
            {
                charProp = StringHelpers.SetCharacterProperties(key, hero.CharacterObject, textObject);
            }

            if (charProp != null)
            {
                charProp.SetTextVariable("Name", heroName);
                charProp.SetTextVariable("name", heroName);
                charProp.SetTextVariable("NAME", heroName);
                charProp.SetTextVariable("Link", heroLink);
                charProp.SetTextVariable("link", heroLink);
                charProp.SetTextVariable("LINK", heroLink);
                charProp.SetTextVariable("Gender", hero.IsFemale ? 1 : 0);
                charProp.SetTextVariable("gender", hero.IsFemale ? 1 : 0);
                charProp.SetTextVariable("GENDER", hero.IsFemale ? 1 : 0);
            }
            else
            {
                var heroText = new TextObject(heroName.ToString());
                heroText.SetTextVariable("Name", heroName);
                heroText.SetTextVariable("name", heroName);
                heroText.SetTextVariable("NAME", heroName);
                heroText.SetTextVariable("Link", heroLink);
                heroText.SetTextVariable("link", heroLink);
                heroText.SetTextVariable("LINK", heroLink);
                heroText.SetTextVariable("Gender", hero.IsFemale ? 1 : 0);
                heroText.SetTextVariable("gender", hero.IsFemale ? 1 : 0);
                heroText.SetTextVariable("GENDER", hero.IsFemale ? 1 : 0);
                textObject.SetTextVariable(key, heroText);
            }

            textObject.SetTextVariable(key + ".Name", heroName);
            textObject.SetTextVariable(key + ".name", heroName);
            textObject.SetTextVariable(key + ".NAME", heroName);
            textObject.SetTextVariable(key + "_Name", heroName);
            textObject.SetTextVariable(key + "_name", heroName);
            textObject.SetTextVariable(key + "_NAME", heroName);
            textObject.SetTextVariable(key + "_Link", heroLink);
            textObject.SetTextVariable(key + "_link", heroLink);
            textObject.SetTextVariable(key + "_LINK", heroLink);
            textObject.SetTextVariable(key + ".Link", heroLink);
            textObject.SetTextVariable(key + ".link", heroLink);
            textObject.SetTextVariable(key + ".LINK", heroLink);
            textObject.SetTextVariable(key + "_Gender", hero.IsFemale ? 1 : 0);
            textObject.SetTextVariable(key + "_gender", hero.IsFemale ? 1 : 0);
            textObject.SetTextVariable(key + "_GENDER", hero.IsFemale ? 1 : 0);
        }

        public static void BindSettlementToTextObject(TextObject textObject, string key, Settlement settlement)
        {
            if (textObject == null || string.IsNullOrEmpty(key) || settlement == null) return;
            var settName = settlement.Name ?? new TextObject(string.Empty);
            var settLink = settlement.EncyclopediaLinkWithName ?? settName;

            var settText = new TextObject("{=!}" + settName.ToString());
            settText.SetTextVariable("Name", settName);
            settText.SetTextVariable("name", settName);
            settText.SetTextVariable("NAME", settName);
            settText.SetTextVariable("Link", settLink);
            settText.SetTextVariable("link", settLink);
            settText.SetTextVariable("LINK", settLink);
            textObject.SetTextVariable(key, settText);

            textObject.SetTextVariable(key + ".Name", settName);
            textObject.SetTextVariable(key + ".name", settName);
            textObject.SetTextVariable(key + ".NAME", settName);
            textObject.SetTextVariable(key + "_Name", settName);
            textObject.SetTextVariable(key + "_name", settName);
            textObject.SetTextVariable(key + "_NAME", settName);
            textObject.SetTextVariable(key + "_Link", settLink);
            textObject.SetTextVariable(key + "_link", settLink);
            textObject.SetTextVariable(key + "_LINK", settLink);
            textObject.SetTextVariable(key + ".Link", settLink);
            textObject.SetTextVariable(key + ".link", settLink);
            textObject.SetTextVariable(key + ".LINK", settLink);
        }

        public static void BindPartyToTextObject(TextObject textObject, string key, TaleWorlds.CampaignSystem.Party.MobileParty party)
        {
            if (textObject == null || string.IsNullOrEmpty(key) || party == null) return;
            var pName = party.Name ?? new TextObject(party.StringId);
            var partyText = new TextObject(pName.ToString());
            partyText.SetTextVariable("Name", pName);
            partyText.SetTextVariable("name", pName);
            partyText.SetTextVariable("NAME", pName);
            partyText.SetTextVariable("Link", pName);
            partyText.SetTextVariable("link", pName);
            partyText.SetTextVariable("LINK", pName);

            textObject.SetTextVariable(key, partyText);
            textObject.SetTextVariable(key + ".Name", pName);
            textObject.SetTextVariable(key + ".name", pName);
            textObject.SetTextVariable(key + ".NAME", pName);
            textObject.SetTextVariable(key + "_Name", pName);
            textObject.SetTextVariable(key + "_name", pName);
            textObject.SetTextVariable(key + "_NAME", pName);
            textObject.SetTextVariable(key + "_Link", pName);
            textObject.SetTextVariable(key + "_link", pName);
            textObject.SetTextVariable(key + "_LINK", pName);
            textObject.SetTextVariable(key + ".Link", pName);
            textObject.SetTextVariable(key + ".link", pName);
            textObject.SetTextVariable(key + ".LINK", pName);
        }

        public void InjectVariablesToTextManager()
        {
            // 1. Inject Global Heroes
            if (GlobalStateManager.Instance.GlobalHeroes != null)
            {
                foreach (var kvp in GlobalStateManager.Instance.GlobalHeroes)
                {
                    if (kvp.Value != null)
                    {
                        BindHeroToMBTextManager(kvp.Key, kvp.Value);
                    }
                }
            }

            // 2. Inject Quest Target Heroes (overrides globals if duplicate key)
            foreach (var kvp in TargetHeroes)
            {
                if (kvp.Value != null)
                {
                    BindHeroToMBTextManager(kvp.Key, kvp.Value);
                }
            }

            // 3. Inject Settlements
            foreach (var kvp in TargetSettlements)
            {
                if (kvp.Value != null)
                {
                    BindSettlementToMBTextManager(kvp.Key, kvp.Value);
                }
            }

            // 4. Inject Mobile Parties
            foreach (var kvp in TargetParties)
            {
                if (kvp.Value != null)
                {
                    BindPartyToMBTextManager(kvp.Key, kvp.Value);
                }
            }

            // 5. Tracked Counters & Strings
            foreach (var kvp in TrackedCounters)
            {
                MBTextManager.SetTextVariable(kvp.Key, kvp.Value);
            }
            foreach (var kvp in TrackedStrings)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                    MBTextManager.SetTextVariable(kvp.Key, new TextObject(kvp.Value));
            }

            // 6. Main Hero / Player
            if (Hero.MainHero != null)
            {
                BindHeroToMBTextManager("Player", Hero.MainHero);
                BindHeroToMBTextManager("PLAYER", Hero.MainHero);
                MBTextManager.SetTextVariable("PlayerName", Hero.MainHero.Name);
                MBTextManager.SetTextVariable("PLAYER_NAME", Hero.MainHero.Name);
            }
        }

        public TextObject FormatText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return new TextObject(string.Empty);
            TextObject parsedText = new TextObject(rawText);

            if (GlobalStateManager.Instance.GlobalHeroes != null)
            {
                foreach (var kvp in GlobalStateManager.Instance.GlobalHeroes)
                {
                    if (kvp.Value != null)
                    {
                        BindHeroToTextObject(parsedText, kvp.Key, kvp.Value);
                    }
                }
            }

            foreach (var kvp in TargetHeroes)
            {
                if (kvp.Value != null)
                {
                    BindHeroToTextObject(parsedText, kvp.Key, kvp.Value);
                }
            }

            foreach (var kvp in TargetSettlements)
            {
                if (kvp.Value != null)
                {
                    BindSettlementToTextObject(parsedText, kvp.Key, kvp.Value);
                }
            }

            foreach (var kvp in TargetParties)
            {
                if (kvp.Value != null)
                {
                    BindPartyToTextObject(parsedText, kvp.Key, kvp.Value);
                }
            }

            foreach (var kvp in TrackedCounters)
            {
                parsedText.SetTextVariable(kvp.Key, kvp.Value);
            }

            foreach (var kvp in TrackedStrings)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                    parsedText.SetTextVariable(kvp.Key, new TextObject(kvp.Value));
            }

            if (Hero.MainHero != null)
            {
                BindHeroToTextObject(parsedText, "Player", Hero.MainHero);
                BindHeroToTextObject(parsedText, "PLAYER", Hero.MainHero);
                parsedText.SetTextVariable("PlayerName", Hero.MainHero.Name);
                parsedText.SetTextVariable("PLAYER_NAME", Hero.MainHero.Name);
            }

            return parsedText;
        }

        [SaveableField(5)]
        private string _modelId;

        private JournalLog? _currentDiscreteLog;
        private ConditionModel? _trackedCondition;

        [SaveableField(6)]
        private CampaignTime _stageStartTime;
        public CampaignTime StageStartTime => _stageStartTime;

        [SaveableField(10)]
        private CampaignTime _stageDeadline = CampaignTime.Never;
        public CampaignTime StageDeadline => _stageDeadline;

        public DynamicQuest(QuestModel model, Hero? questGiver, CampaignTime duration) 
            : base(model.Id, questGiver, CampaignTime.DaysFromNow(9999f), 0)
        {
            Model = model;
            _modelId = model.Id;
            CurrentStageId = model.Stages.Count > 0 ? model.Stages[0].Id : string.Empty;
            SetDialogs();
            InitializeQuestOnCreation();

            // Problem 10 Fix: Never track MainHero! TaleWorlds automatically tracks questGiver if passed into base()
            if (Campaign.Current?.QuestManager != null && Hero.MainHero != null && IsTracked(Hero.MainHero))
            {
                Campaign.Current.QuestManager.RemoveTrackedObjectForQuest(Hero.MainHero, this);
            }
        }

        public void ForceFail() 
        { 
            var failStage = Model.Stages.FirstOrDefault(s => s.IsFailNode);
            if (failStage != null && CurrentStageId != failStage.Id)
            {
                TransitionToStage(failStage.Id);
            }
            else
            {
                CompleteQuestWithFail(); 
            }
        }

        
        public void ForceCompleteCurrentStage()
        {
            var currentStageModel = Model.Stages.FirstOrDefault(s => s.Id == CurrentStageId);
            if (currentStageModel != null)
            {
                // Execute timeout actions if the stage had an active timeout (Problem 4 fix)
                if (currentStageModel.TimeoutInDays > 0 || currentStageModel.TimeoutInHours > 0)
                {
                    foreach (var action in currentStageModel.OnTimeoutActions)
                    {
                        Questliner.Framework.Engine.ActionExecutor.Execute(action, this);
                    }
                }

                foreach (var action in currentStageModel.OnExitActions)
                {
                    Questliner.Framework.Engine.ActionExecutor.Execute(action, this);
                }

                if (currentStageModel.IsSuccessNode)
                {
                    ForceSuccess();
                }
                else if (currentStageModel.IsFailNode)
                {
                    ForceFail();
                }
                else if (!string.IsNullOrEmpty(currentStageModel.NextStageId))
                {
                    TransitionToStage(currentStageModel.NextStageId);
                }
                else
                {
                    var changeStageAction = currentStageModel.OnTimeoutActions.FirstOrDefault(a => a.ActionType == "ChangeStage")
                                         ?? currentStageModel.OnExitActions.FirstOrDefault(a => a.ActionType == "ChangeStage");
                    if (changeStageAction != null && !string.IsNullOrEmpty(changeStageAction.ObjectId))
                    {
                        TransitionToStage(changeStageAction.ObjectId);
                    }
                    else
                    {
                        ForceSuccess();
                    }
                }
            }
        }

        public void EnsureGlobalHeroesPersisted()
        {
            if (Model == null) return;

            var actionList = new List<ActionModel>();

            if (Model.Stages != null)
            {
                foreach (var stage in Model.Stages)
                {
                    if (stage.OnEnterActions != null) actionList.AddRange(stage.OnEnterActions);
                    if (stage.OnExitActions != null) actionList.AddRange(stage.OnExitActions);
                    if (stage.OnTimeoutActions != null) actionList.AddRange(stage.OnTimeoutActions);
                }
            }

            if (Model.CustomDialogs != null)
            {
                foreach (var diag in Model.CustomDialogs)
                {
                    if (diag.Consequences != null) actionList.AddRange(diag.Consequences);
                }
            }

            if (Model.GameMenuOptions != null)
            {
                foreach (var menuOpt in Model.GameMenuOptions)
                {
                    if (menuOpt.Consequences != null) actionList.AddRange(menuOpt.Consequences);
                }
            }

            foreach (var action in actionList)
            {
                if (string.Equals(action.ActionType, "SaveHeroToGlobal", System.StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(action.TargetAlias)
                    && !string.IsNullOrEmpty(action.StringValue))
                {
                    string globalKey = action.StringValue.Trim();
                    string targetAlias = action.TargetAlias.Trim();

                    Hero? targetHero = GetTargetHero(targetAlias);

                    if (targetHero != null)
                    {
                        GlobalStateManager.Instance.SetGlobalHero(globalKey, targetHero);
                        BindHeroToMBTextManager(globalKey, targetHero);
                        TaleWorlds.Library.Debug.Print($"[Questliner] EnsureGlobalHeroesPersisted: persisted '{globalKey}' -> {targetHero.Name} ({targetHero.StringId}) from TargetHeroes['{targetAlias}']");
                    }
                    else if (GlobalStateManager.Instance.GetGlobalHero(globalKey) == null)
                    {
                        Hero? hero = GlobalStateManager.Instance.GetGlobalHero(targetAlias);
                        if (hero == null && (targetAlias.Equals("Speaker", System.StringComparison.OrdinalIgnoreCase) || targetAlias.Equals("ConversationHero", System.StringComparison.OrdinalIgnoreCase)))
                        {
                            hero = Hero.OneToOneConversationHero;
                        }
                        if (hero == null)
                        {
                            hero = Campaign.Current?.AliveHeroes.FirstOrDefault(h => h.StringId == targetAlias);
                        }

                        if (hero != null)
                        {
                            GlobalStateManager.Instance.SetGlobalHero(globalKey, hero);
                            BindHeroToMBTextManager(globalKey, hero);
                            TaleWorlds.Library.Debug.Print($"[Questliner] EnsureGlobalHeroesPersisted: fallback saved '{globalKey}' -> {hero.Name} ({hero.StringId})");
                        }
                    }
                }
            }
        }

        public void ForceSuccess() 
        { 
            EnsureGlobalHeroesPersisted();
            var successStage = Model.Stages.FirstOrDefault(s => s.IsSuccessNode);
            if (successStage != null && CurrentStageId != successStage.Id)
            {
                var currentStageModel = Model.Stages.FirstOrDefault(s => s.Id == CurrentStageId);
                if (currentStageModel != null)
                {
                    foreach (var action in currentStageModel.OnExitActions)
                    {
                        Questliner.Framework.Engine.ActionExecutor.Execute(action, this);
                    }
                }
                TransitionToStage(successStage.Id);
            }
            else
            {
                CompleteQuestWithSuccess(); 
                Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
            }
        }





        protected override void OnStartQuest()
        {
            if (!string.IsNullOrEmpty(CurrentStageId)) TransitionToStage(CurrentStageId);
        }

        public void CheckStageProgression()
        {
            if (string.IsNullOrEmpty(CurrentStageId)) return;
            
            if (_currentDiscreteLog != null && _trackedCondition != null)
            {
                int current = Questliner.Framework.Engine.ConditionEvaluator.GetCurrentProgress(_trackedCondition, this);
                if (current != _currentDiscreteLog.CurrentProgress)
                {
                    _currentDiscreteLog.UpdateCurrentProgress(current);
                }
            }
            
            var currentStageModel = Model.Stages.FirstOrDefault(s => s.Id == CurrentStageId);
            if (currentStageModel == null) return;

            // Dynamically refresh tracking markers if targets have changed settlements or formed parties
            RefreshActiveStageTracking();

            // Timeout check
            if ((currentStageModel.TimeoutInDays > 0 && StageStartTime.ElapsedDaysUntilNow >= currentStageModel.TimeoutInDays) ||
                (currentStageModel.TimeoutInHours > 0 && StageStartTime.ElapsedHoursUntilNow >= currentStageModel.TimeoutInHours))
            {
                foreach (var action in currentStageModel.OnTimeoutActions)
                {
                    Questliner.Framework.Engine.ActionExecutor.Execute(action, this);
                }
                
                // If the timeout action changed the stage, we don't want to process conditions anymore
                if (CurrentStageId != currentStageModel.Id) return;
            }

            // Live-update the discrete journal progress bar if applicable
            if (_currentDiscreteLog != null && _trackedCondition != null)
            {
                int current = Questliner.Framework.Engine.ConditionEvaluator.GetCurrentProgress(_trackedCondition, this);
                _currentDiscreteLog.UpdateCurrentProgress(current);
            }

            // Failure condition check (Problem 5 & 7 fix)
            if (currentStageModel.FailConditions != null && currentStageModel.FailConditions.Count > 0)
            {
                bool isAnyOp = !string.Equals(currentStageModel.FailConditionsOperator, "All", StringComparison.OrdinalIgnoreCase);
                bool failMet = isAnyOp 
                    ? currentStageModel.FailConditions.Any(c => Questliner.Framework.Engine.ConditionEvaluator.IsConditionMet(c, this))
                    : currentStageModel.FailConditions.All(c => Questliner.Framework.Engine.ConditionEvaluator.IsConditionMet(c, this));

                if (failMet)
                {
                    if (!string.IsNullOrEmpty(currentStageModel.FailStageId))
                    {
                        TransitionToStage(currentStageModel.FailStageId);
                    }
                    else
                    {
                        CompleteQuestWithFail();
                    }
                    return;
                }
            }

            bool allMet = currentStageModel.Conditions.Count > 0 && currentStageModel.Conditions.All(c => Questliner.Framework.Engine.ConditionEvaluator.IsConditionMet(c, this));

            if (allMet)
            {
                // Exit Actions
                foreach (var action in currentStageModel.OnExitActions)
                {
                    Questliner.Framework.Engine.ActionExecutor.Execute(action, this);
                }

                // Transition
                if (currentStageModel.IsSuccessNode)
                {
                    EnsureGlobalHeroesPersisted();
                    CompleteQuestWithSuccess();
                    Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
                }
                else if (currentStageModel.IsFailNode)
                {
                    CompleteQuestWithFail();
                }
                else if (!string.IsNullOrEmpty(currentStageModel.NextStageId))
                {
                    TransitionToStage(currentStageModel.NextStageId);
                }
            }
        }

        public void TransitionToStage(string nextStageId)
        {
            CurrentStageId = nextStageId;
            _stageStartTime = CampaignTime.Now;
            
            var nextStageModel = Model.Stages.FirstOrDefault(s => s.Id == nextStageId);
            if (nextStageModel != null)
            {
                float totalHours = nextStageModel.TimeoutInDays * 24f + nextStageModel.TimeoutInHours;
                if (totalHours > 0)
                {
                    _stageDeadline = CampaignTime.HoursFromNow(totalHours);
                    ChangeQuestDueTime(CampaignTime.HoursFromNow(totalHours + 1f));
                }
                else
                {
                    _stageDeadline = CampaignTime.Never;
                    ChangeQuestDueTime(CampaignTime.DaysFromNow(9999f));
                }

                if (nextStageModel.IsSuccessNode)
                {
                    EnsureGlobalHeroesPersisted();
                    Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
                }

                foreach (var action in nextStageModel.OnEnterActions)
                {
                    Questliner.Framework.Engine.ActionExecutor.Execute(action, this);
                }
                
                // Track relevant targets and parse log string
                if (!string.IsNullOrEmpty(nextStageModel.LogText))
                {
                    // Problem 6 Fix: Clear previously tracked objects of this quest so old stage exclamation marks don't linger forever
                    if (Campaign.Current?.QuestManager != null)
                    {
                        Campaign.Current.QuestManager.RemoveAllTrackedObjectsForQuest(this);
                    }

                    TextObject parsedLogText = FormatText(nextStageModel.LogText);

                    // Track objects mentioned in the log text dynamically
                    foreach (var kvp in TargetHeroes)
                    {
                        if (kvp.Value == null) continue;
                        string key = kvp.Key;
                        bool isMentioned = nextStageModel.LogText.IndexOf("{" + key, StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isMentioned)
                        {
                            TrackHeroLocation(kvp.Value);
                        }
                    }

                    if (GlobalStateManager.Instance.GlobalHeroes != null)
                    {
                        foreach (var kvp in GlobalStateManager.Instance.GlobalHeroes)
                        {
                            if (kvp.Value == null) continue;
                            string key = kvp.Key;
                            bool isMentioned = nextStageModel.LogText.IndexOf("{" + key, StringComparison.OrdinalIgnoreCase) >= 0;

                            if (isMentioned)
                            {
                                TrackHeroLocation(kvp.Value);
                            }
                        }
                    }

                    foreach (var kvp in TargetSettlements)
                    {
                        if (kvp.Value != null && nextStageModel.LogText.IndexOf("{" + kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 && !IsTracked(kvp.Value))
                        {
                            AddTrackedObject(kvp.Value);
                        }
                    }

                    foreach (var kvp in TargetParties)
                    {
                        if (kvp.Value != null && kvp.Value.IsActive && !IsTracked(kvp.Value))
                        {
                            string key = kvp.Key;
                            if (nextStageModel.LogText.IndexOf("{" + key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                nextStageModel.Conditions.Any(c => string.Equals(c.TargetAlias, key, StringComparison.OrdinalIgnoreCase)) ||
                                nextStageModel.OnEnterActions.Any(a => string.Equals(a.TargetAlias, key, StringComparison.OrdinalIgnoreCase)))
                            {
                                AddTrackedObject(kvp.Value);
                            }
                        }
                    }

                    foreach (var cond in nextStageModel.Conditions)
                    {
                        if (!string.IsNullOrEmpty(cond.TargetAlias))
                        {
                            var targetHero = GetTargetHero(cond.TargetAlias);
                            if (targetHero != null)
                            {
                                TrackHeroLocation(targetHero);
                            }

                            var targetSettlement = GetTargetSettlement(cond.TargetAlias);
                            if (targetSettlement != null && !IsTracked(targetSettlement)) AddTrackedObject(targetSettlement);

                            var targetParty = GetTargetParty(cond.TargetAlias);
                            if (targetParty != null && !IsTracked(targetParty)) AddTrackedObject(targetParty);
                        }
                    }

                    foreach (var act in nextStageModel.OnEnterActions)
                    {
                        if (!string.IsNullOrEmpty(act.TargetAlias))
                        {
                            var targetHero = GetTargetHero(act.TargetAlias);
                            if (targetHero != null)
                            {
                                if (act.ActionType == "MakeMet" || act.ActionType == "TalkTo" || act.ActionType == "ChangeRelation")
                                {
                                    TrackHeroLocation(targetHero);
                                }
                            }
                        }
                    }

                    if (nextStageModel.LogType == "Discrete" || nextStageModel.LogType == "Progress")
                    {
                        _trackedCondition = nextStageModel.Conditions.FirstOrDefault(c => c.Amount > 1) ?? nextStageModel.Conditions.FirstOrDefault();
                        int current = _trackedCondition != null ? Questliner.Framework.Engine.ConditionEvaluator.GetCurrentProgress(_trackedCondition, this) : 0;
                        int target = nextStageModel.TargetAmount > 1 ? nextStageModel.TargetAmount : (_trackedCondition?.Amount ?? 1);
                        
                        _currentDiscreteLog = AddDiscreteLog(parsedLogText, new TextObject(""), current, target);
                    }
                    else
                    {
                        _currentDiscreteLog = null;
                        _trackedCondition = null;
                        AddLog(parsedLogText);
                    }
                }



                // Problem 10 Fix: Never track MainHero!
                if (Campaign.Current?.QuestManager != null && Hero.MainHero != null && IsTracked(Hero.MainHero))
                {
                    Campaign.Current.QuestManager.RemoveTrackedObjectForQuest(Hero.MainHero, this);
                }

                if (nextStageModel.IsSuccessNode)
                {
                    EnsureGlobalHeroesPersisted();
                    CompleteQuestWithSuccess();
                    Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
                }
                else if (nextStageModel.IsFailNode)
                {
                    CompleteQuestWithFail();
                }
            }
        }

        public void TrackHeroLocation(Hero? hero)
        {
            if (hero == null || hero == Hero.MainHero) return;
            if (!IsTracked(hero)) AddTrackedObject(hero);
            var sett = Questliner.Framework.Engine.TargetResolverService.GetHeroSettlement(hero);
            if (sett != null && !IsTracked(sett)) AddTrackedObject(sett);
            if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsActive && !IsTracked(hero.PartyBelongedTo))
            {
                AddTrackedObject(hero.PartyBelongedTo);
            }
        }

        public void RefreshActiveStageTracking()
        {
            foreach (var kvp in TargetHeroes)
            {
                if (kvp.Value != null && IsTracked(kvp.Value))
                {
                    TrackHeroLocation(kvp.Value);
                }
            }

            if (GlobalStateManager.Instance.GlobalHeroes != null)
            {
                foreach (var kvp in GlobalStateManager.Instance.GlobalHeroes)
                {
                    if (kvp.Value != null && IsTracked(kvp.Value))
                    {
                        TrackHeroLocation(kvp.Value);
                    }
                }
            }
        }

        public Hero? GetTargetHero(string alias)
        {
            if (string.IsNullOrEmpty(alias)) return null;
            if (TargetHeroes.TryGetValue(alias, out var hero)) return hero;
            return System.Linq.Enumerable.FirstOrDefault(TargetHeroes, k => string.Equals(k.Key, alias, StringComparison.OrdinalIgnoreCase)).Value;
        }

        public Settlement? GetTargetSettlement(string alias)
        {
            if (string.IsNullOrEmpty(alias)) return null;
            if (TargetSettlements.TryGetValue(alias, out var st)) return st;
            return System.Linq.Enumerable.FirstOrDefault(TargetSettlements, k => string.Equals(k.Key, alias, StringComparison.OrdinalIgnoreCase)).Value;
        }

        public TaleWorlds.CampaignSystem.Party.MobileParty? GetTargetParty(string alias)
        {
            if (string.IsNullOrEmpty(alias)) return null;
            if (TargetParties.TryGetValue(alias, out var p)) return p;
            return System.Linq.Enumerable.FirstOrDefault(TargetParties, k => string.Equals(k.Key, alias, StringComparison.OrdinalIgnoreCase)).Value;
        }

        public bool GetFlag(string key) => TrackedFlags.TryGetValue(key, out var val) && val;
        public void SetFlag(string key, bool value) => TrackedFlags[key] = value;

        public override TextObject Title 
        {
            get 
            {
                EnsureModelLoaded();
                return new TextObject(Model?.Title ?? "{=!}Eksik/Silinmiş Mod Görevi");
            }
        }
        
        public override bool IsRemainingTimeHidden 
        {
            get
            {
                EnsureModelLoaded();
                return CurrentStage == null || (CurrentStage.TimeoutInDays <= 0 && CurrentStage.TimeoutInHours <= 0);
            }
        }

        protected override void InitializeQuestOnGameLoad()
        {
            Model = Questliner.Framework.Services.QuestModuleScanner.LoadedQuests.FirstOrDefault(q => q.Id == _modelId);
            
            if (Model == null)
            {
                TaleWorlds.Library.Debug.Print($"[Questliner] Orphaned quest detected: {_modelId}. Canceling safely.");
                TaleWorlds.Core.MBInformationManager.AddQuickInformation(new TaleWorlds.Localization.TextObject("{=!}Bir mod silindiği için ona ait olan görev iptal edildi."));
                CompleteQuestWithCancel(new TaleWorlds.Localization.TextObject("{=!}Bu görev, ait olduğu mod dosyaları bulunamadığı için sistem tarafından otomatik olarak iptal edilmiştir."));
                return;
            }

            SetDialogs();
        }

        protected override void SetDialogs()
        {
            // Dynamic dialogues bound to this quest instance
        }

        protected override void OnCompleteWithSuccess()
        {
            base.OnCompleteWithSuccess();
            EnsureGlobalHeroesPersisted();
            Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(_modelId ?? Model?.Id ?? string.Empty);
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();

            if (_targetParties != null)
            {
                foreach (var kvp in _targetParties)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.SetPartyUsedByQuest(false);

                        if (kvp.Value.IsActive && (kvp.Value.StringId.StartsWith("ql_bandit_") || kvp.Value.StringId.StartsWith("ql_custom_party_")))
                        {
                            try
                            {
                                kvp.Value.IgnoreByOtherPartiesTill(CampaignTime.Now);
                                kvp.Value.Ai.SetDoNotMakeNewDecisions(false);
                                if (kvp.Value.MapEvent == null)
                                {
                                    TaleWorlds.CampaignSystem.Actions.DestroyPartyAction.Apply(null, kvp.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                TaleWorlds.Library.Debug.Print($"[Questliner] Error destroying quest party {kvp.Value.StringId}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        public override void OnFailed()
        {
            base.OnFailed();
        }

        public override void OnCanceled()
        {
            base.OnCanceled();
        }

        protected override void OnTimedOut()
        {
            base.OnTimedOut();
        }

        protected override void OnBetrayal()
        {
            base.OnBetrayal();
        }
    }
}

