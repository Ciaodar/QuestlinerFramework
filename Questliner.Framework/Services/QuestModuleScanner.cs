using System;
using System.Collections.Generic;
using System.IO;
using Questliner.Shared.Models;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace Questliner.Framework.Services
{
    public static class QuestModuleScanner
    {
        public static List<QuestModel> LoadedQuests { get; private set; } = new List<QuestModel>();

        public static void ScanAndLoadQuestlines()
        {
            LoadedQuests.Clear();
            
            try
            {
                var modules = ModuleHelper.GetModules();
                foreach (var module in modules)
                {
                    string modulePath = ModuleHelper.GetModuleFullPath(module.Id);
                    string questlinesDir = Path.Combine(modulePath, "ModuleData", "Questlines");

                    if (Directory.Exists(questlinesDir))
                    {
                        var xmlFiles = Directory.GetFiles(questlinesDir, "*.xml", SearchOption.AllDirectories);
                        foreach (var xmlFile in xmlFiles)
                        {
                            LoadQuestXml(xmlFile, module.Id);
                        }
                    }
                }
                
                TaleWorlds.Library.Debug.Print($"[Questliner] Loaded {LoadedQuests.Count} dynamic questline(s).");
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Questliner] Error scanning questlines: {ex.Message}");
            }
        }

        private static void LoadQuestXml(string filePath, string sourceModuleId)
        {
            try
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(QuestModel));
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (serializer.Deserialize(fs) is QuestModel model)
                    {
                        model.SourceModuleId = sourceModuleId;
                        LoadedQuests.Add(model);
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Questliner] Failed to load quest file '{Path.GetFileName(filePath)}' from module '{sourceModuleId}': {ex.Message}");
            }
        }
    }
}
