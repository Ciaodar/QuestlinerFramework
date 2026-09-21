using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
namespace Questliner.Framework.Core
{
    public class GlobalStateManager
    {
        private static GlobalStateManager? _instance;
        public static GlobalStateManager Instance => _instance ??= new GlobalStateManager();

        // The core dictionary holding our global persistent flags
        public Dictionary<string, bool> GlobalFlags { get; internal set; }
        
        // Persistent Heroes to pass between different quests
        public Dictionary<string, Hero> GlobalHeroes { get; internal set; }

        private GlobalStateManager()
        {
            GlobalFlags = new Dictionary<string, bool>();
            GlobalHeroes = new Dictionary<string, Hero>();
        }

        public void SetFlag(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            GlobalFlags[key] = value;
        }

        public bool GetFlag(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return GlobalFlags.TryGetValue(key, out bool val) && val;
        }

        public void SetGlobalHero(string key, Hero hero)
        {
            if (string.IsNullOrEmpty(key) || hero == null) return;
            GlobalHeroes[key] = hero;
        }

        public Hero? GetGlobalHero(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return GlobalHeroes.TryGetValue(key, out Hero? h) ? h : null;
        }

        // Called on Game Start/Load to clear/initialize
        public void Reset()
        {
            GlobalFlags.Clear();
            GlobalHeroes.Clear();
        }

    }
}
