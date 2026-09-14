using System;
using Questliner.Framework.Behaviors;
using Questliner.Framework.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Questliner.Framework
{
    public class QuestlinerSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            
            // Scan XMLs safely when the game has actually started
            QuestModuleScanner.ScanAndLoadQuestlines();

            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarterObject;

                campaignStarter.AddBehavior(new QuestlinerCampaignBehavior());
            }
        }
    }
}
