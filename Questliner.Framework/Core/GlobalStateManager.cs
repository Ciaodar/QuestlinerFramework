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
            if (GlobalFlags.TryGetValue(key, out bool val)) return val;
            return System.Linq.Enumerable.FirstOrDefault(GlobalFlags, k => string.Equals(k.Key, key, System.StringComparison.OrdinalIgnoreCase)).Value;
        }

        public void SetGlobalHero(string key, Hero hero)
        {
            if (string.IsNullOrEmpty(key) || hero == null) return;
            GlobalHeroes[key] = hero;
        }

        public Hero? GetGlobalHero(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (GlobalHeroes.TryGetValue(key, out Hero? h)) return h;
            return System.Linq.Enumerable.FirstOrDefault(GlobalHeroes, k => string.Equals(k.Key, key, System.StringComparison.OrdinalIgnoreCase)).Value;
        }

        // Called on Game Start/Load to clear/initialize
        public void Reset()
        {
            GlobalFlags.Clear();
            GlobalHeroes.Clear();
        }

    }
}
