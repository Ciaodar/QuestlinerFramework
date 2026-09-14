using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace Questliner.Framework.Core
{
    public class QuestlinerSaveDefiner : SaveableTypeDefiner
    {
        public QuestlinerSaveDefiner() : base(854932) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(DynamicQuest), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, Hero>));
            ConstructContainerDefinition(typeof(Dictionary<string, Settlement>));
            ConstructContainerDefinition(typeof(Dictionary<string, bool>));
            ConstructContainerDefinition(typeof(System.Collections.Generic.Dictionary<string, int>));
        }
    }
}
