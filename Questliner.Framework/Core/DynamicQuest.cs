using System;
using System.Collections.Generic;
using System.Linq;
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

        public void InjectVariablesToTextManager()
        {
            foreach (var kvp in TargetHeroes)
            {
                if (kvp.Value != null)
                {
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + "_Link", kvp.Value.EncyclopediaLinkWithName);
                }
            }
            foreach (var kvp in TargetSettlements)
            {
                if (kvp.Value != null)
                {
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + "_Link", kvp.Value.EncyclopediaLinkWithName);
                }
            }
            foreach (var kvp in TargetParties)
            {
                if (kvp.Value != null)
                {
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key, kvp.Value.Name);
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key + "_Link", kvp.Value.Name);
                }
            }
            foreach (var kvp in TrackedCounters)
            {
                TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key, kvp.Value);
            }
            foreach (var kvp in TrackedStrings)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                    TaleWorlds.Localization.MBTextManager.SetTextVariable(kvp.Key, new TaleWorlds.Localization.TextObject(kvp.Value));
            }
        }

        [SaveableField(5)]
        private string _modelId;

        private JournalLog? _currentDiscreteLog;
        private ConditionModel? _trackedCondition;

        [SaveableField(6)]
        private CampaignTime _stageStartTime;
        public CampaignTime StageStartTime => _stageStartTime;

        public DynamicQuest(QuestModel model, Hero? questGiver, CampaignTime duration) 
            : base(model.Id, questGiver, duration, 0)
        {
            Model = model;
            _modelId = model.Id;
            CurrentStageId = model.Stages.Count > 0 ? model.Stages[0].Id : string.Empty;
            SetDialogs();
            InitializeQuestOnCreation();
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
                Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
            }
        }

        
        public void ForceCompleteCurrentStage()
        {
            var currentStageModel = Model.Stages.FirstOrDefault(s => s.Id == CurrentStageId);
            if (currentStageModel != null)
            {
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
                    ForceSuccess();
                }
            }
        }

        public void ForceSuccess() 
        { 
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
                    CompleteQuestWithSuccess();
                    Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
                }
                else if (currentStageModel.IsFailNode)
                {
                    CompleteQuestWithFail();
                    Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
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
                foreach (var action in nextStageModel.OnEnterActions)
                {
                    Questliner.Framework.Engine.ActionExecutor.Execute(action, this);
                }
                
                // Track relevant targets and parse log string
                if (!string.IsNullOrEmpty(nextStageModel.LogText))
                {
                    TextObject parsedLogText = new TextObject(nextStageModel.LogText);

                    foreach (var kvp in TargetHeroes)
                    {
                        parsedLogText.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                        parsedLogText.SetTextVariable(kvp.Key + "_Link", kvp.Value.EncyclopediaLinkWithName);
                    }
                    foreach (var kvp in TargetSettlements)
                    {
                        parsedLogText.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                        parsedLogText.SetTextVariable(kvp.Key + "_Link", kvp.Value.EncyclopediaLinkWithName);
                    }
                    foreach (var kvp in TargetParties)
                    {
                        if (kvp.Value != null)
                        {
                            parsedLogText.SetTextVariable(kvp.Key, kvp.Value.Name);
                            parsedLogText.SetTextVariable(kvp.Key + ".Name", kvp.Value.Name);
                            parsedLogText.SetTextVariable(kvp.Key + "_Link", kvp.Value.Name);
                        }
                    }
                    foreach (var kvp in TrackedCounters)
                    {
                        parsedLogText.SetTextVariable(kvp.Key, kvp.Value);
                    }
                    foreach (var kvp in TrackedStrings)
                    {
                        if (!string.IsNullOrEmpty(kvp.Value))
                            parsedLogText.SetTextVariable(kvp.Key, new TextObject(kvp.Value));
                    }
                    parsedLogText.SetTextVariable("Player.Name", Hero.MainHero.Name);
                    parsedLogText.SetTextVariable("PlayerName", Hero.MainHero.Name);

                    // Track objects mentioned in the log text dynamically
                    foreach (var kvp in TargetHeroes)
                    {
                        if (nextStageModel.LogText.Contains("{" + kvp.Key) && !IsTracked(kvp.Value))
                            AddTrackedObject(kvp.Value);
                    }
                    foreach (var kvp in TargetSettlements)
                    {
                        if (nextStageModel.LogText.Contains("{" + kvp.Key) && !IsTracked(kvp.Value))
                            AddTrackedObject(kvp.Value);
                    }
                    foreach (var kvp in TargetParties)
                    {
                        if (nextStageModel.LogText.Contains("{" + kvp.Key) && kvp.Value != null && !IsTracked(kvp.Value))
                            AddTrackedObject(kvp.Value);
                    }

                    foreach (var cond in nextStageModel.Conditions)
                    {
                        if (!string.IsNullOrEmpty(cond.TargetAlias))
                        {
                            var targetHero = GetTargetHero(cond.TargetAlias);
                            if (targetHero != null && !IsTracked(targetHero)) AddTrackedObject(targetHero);

                            var targetSettlement = GetTargetSettlement(cond.TargetAlias);
                            if (targetSettlement != null && !IsTracked(targetSettlement)) AddTrackedObject(targetSettlement);

                            var targetParty = GetTargetParty(cond.TargetAlias);
                            if (targetParty != null && !IsTracked(targetParty)) AddTrackedObject(targetParty);
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



                if (nextStageModel.IsSuccessNode)
                {
                    CompleteQuestWithSuccess();
                    Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
                }
                else if (nextStageModel.IsFailNode)
                {
                    CompleteQuestWithFail();
                    Questliner.Framework.Behaviors.QuestlinerCampaignBehavior.Instance?.MarkQuestAsCompleted(Model.Id);
                }
            }
        }

        public Hero? GetTargetHero(string alias) => TargetHeroes.TryGetValue(alias, out var hero) ? hero : null;
        public Settlement? GetTargetSettlement(string alias) => TargetSettlements.TryGetValue(alias, out var st) ? st : null;
        public TaleWorlds.CampaignSystem.Party.MobileParty? GetTargetParty(string alias) => TargetParties.TryGetValue(alias, out var p) ? p : null;
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
                return Model == null || !Model.Stages.Any(s => s.TimeoutInDays > 0 || s.TimeoutInHours > 0);
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
    }
}

