# Handoff Summaries

## 1. Scavenger Pigeons Quest Writer -> Framework Agent
### Completed Work (Scavenger Pigeons)
- Designed and finalized the "Scavenger Pigeons" (Çapulcu Güvercinler) secret intelligence cult storyline.
- Created 5 consecutive quests using the Questliner XML schema in `ScavengerPigeons/ModuleData/Questlines`:
  1. `QLF_HainGuvercin`
  2. `QLF_ItibarDersi`
  3. `QLF_YaDogruysa`
  4. `QLF_AnavataninIhaneti`
  5. `QLF_KopukBaglar`
- Implemented advanced XML mechanics:
  - `GlobalHero(Key)` and `SaveHeroToGlobal` to persist specific NPCs (Dost, Hain Zanaatkar) across quests.
  - `BattleResolvedAs` to properly track battle outcomes against dynamically spawned bandit parties (Konvoy).
  - Timeouts and delays (`TimeoutInDays="7"`) seamlessly integrated at the start of `QLF_KopukBaglar` to simulate a "1 week later" narrative skip without keeping the previous quest lingering.
- Configured MSBuild auto-versioning and deployment pipelines via PowerShell (`Build/UpdateVersion.ps1`).

### Status
- The `ScavengerPigeons` questline is fully implemented, strictly adheres to the schema, and has been committed and pushed to the repository.
- XML logic flaws identified during testing and self-reflection have been patched.

### Notes for the Framework Agent
- **Serialization Check:** Ensure the C# Engine's `GlobalHero` saving mechanism properly serializes the heroes to the campaign save file. Quests 4 and 5 heavily depend on this.
- **Timeout Check:** `QLF_KopukBaglar` relies on an `AutoStart` trigger followed immediately by a 7-day `TimeoutInDays` on its very first stage (`Stage0_Bekleme`). Please ensure the C# tick engine processes timeouts correctly on newly auto-started quests.
- **Action Scales:** The `ChangeRelation` amount was reduced to `5` because Bannerlord natively amplifies relation gains (e.g., via the player's Charm skill), which previously resulted in unintended +120 relation spikes.

---

## 2. Demir Ahit (Iron Covenant) Quest Writer -> Framework Agent
- **Tarih:** 27 Eylül 2026
- **Görevi Devreden:** Demir Ahit Senaryo & XML Geliştirme Ajanı
- **Görevi Devralacak:** Framework Geliştirme Ajanı / Subagent
- **İlgili Modül:** `DemirAhit` (Launcher Adı: **Iron Covenant**)

### Tamamlanan Çalışmalar ve Modül Mimarisi
1. **Görev Dosyaları (`DemirAhit/ModuleData/Questlines/`):**
   - `QLF_DemirAhit_Cirak.xml` (Apprentice's Oath - Clan Tier 2): Yıldız Demiri arayışı, diyalog akışında aşama geçişi (`Stage1_Davet` -> `Stage1_Toplama`), 10.000 altınlık hızlı satın alma kestirme yolu, pusu savaşı ve timeout partisi temizliği.
   - `QLF_DemirAhit_Kalfa.xml` (Journeyman's Trial - Clan Tier 3): Atölye savunması veya rakip tüccarla yüzleşme (rüşvet / serserilik tehdidi / korumalarla dövüş).
   - `QLF_DemirAhit_Usta.xml` (Master's Burden - Clan Tier 4): Demircinin kardeşi Kalan'ı yerleşke zindanından kurtarma operasyonu (gizli sızma / doğrudan kuşatma / fidye).
   - `QLF_DemirAhit_Miras.xml` (Forging the Legacy - Clan Tier 5): 50.000 altınlık Kalbin Ateşi ritüeli, kılıcın dövülmesi ve klan mirası veya krala hediye etme yol ayrımı.
   - `DemirAhit/ModuleData/items.xml`: Özel kılıçlar, mühür ve madenler (`YildizDemiri`, `GecicilAhitKilici`, `KalfaMuhru`, `KalbinAtesi`, `AhitKilici_Efsanevi`).

2. **Uluslararasılaştırma ve Dil Desteği (Localization):**
   - Tüm ana XML dosyalarındaki saf metinler standart İngilizceye dönüştürülmüştür.
   - `ModuleData/Languages/EN/std_IronCovenant_en.xml` (EN) ve `ModuleData/Languages/TR/std_IronCovenant_tr.xml` (TR) dil dosyaları eksiksiz oluşturulmuştur.

3. **DevOps & Build Hattı:**
   - `DemirAhit/Build/UpdateVersion.ps1` (`vMAJOR.UPDATE.HOTFIX.DEBUG` formatı).
   - `DemirAhit.csproj` üzerinde PreBuild (otomatik versiyon artırma) ve PostBuild (Bannerlord `Modules/DemirAhit` klasörüne otomatik deployment) ayarlanmıştır.
   - `SubModule.xml` içine `QuestlinerFramework` bağımlılığı ve "Iron Covenant" modül adı işlenmiştir.

### Motor Seviyesinde Talep Edilen & Tamamlanan Özellikler (IR Geçmişi)
- `BattleResolvedAs`: Mobil partilerle yapılan savaş sonuçlarının izlenmesi.
- `SaveHeroToGlobal` & `GlobalHero(Key)`: Demirci, tüccar ve Kalan karakterlerinin görevler boyunca tutarlı hafızada tutulması.
- `IsOptional="true"` (TargetResolver): Kral gibi hedeflerin bağımsız klanlarda null dönmesi halinde görevin kilitlenmemesi.
- `IsPrisonerInSettlement`: Sadece kale/şehir zindanındaki esirlerin hedeflenmesi (`SameSettlementAs` null hatasını önler).
- `DestroyParty`: Kaçılan veya zaman aşımına uğrayan pusu partilerinin haritadan silinmesi.

### Framework Ajanına Notlar ve Bekleyen İstek (Pending IR)
- **Bekleyen Özellik Talebi (`IR_DisabledDialogHints.md`):** Koşulu sağlanmayan diyalog seçeneklerinin tamamen gizlenmesi yerine gri/tıklanamaz olarak görünmesi ve üzerine gelindiğinde neden seçilemediğini belirten ipucu (`DisabledHint`) göstermesi için C# tarafında geliştirme beklenmektedir.
- **Diyalog Başlangıçları:** Oyundaki tanışma (`MakeMet`) pencerelerinin görev başlangıcını ezmesini engellemek için diyalog girişleri `InputToken="hero_main_options"` olarak ayarlanmıştır.
- **Hedef Takibi:** Metinlerde `{HedefAlias_Link}` formatı kullanılarak otomatik harita/ansiklopedi takibi (mavi ünlem) sağlanmıştır.
