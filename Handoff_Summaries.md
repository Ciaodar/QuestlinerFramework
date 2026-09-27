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

---

## 3. Shadows' Ascendancy (Gölgelerin Yükselişi) Quest Writer -> Framework Agent
- **Tarih:** 27 Eylül 2026
- **Görevi Devreden:** Shadows' Ascendancy Senaryo & XML Geliştirme Ajanı
- **Görevi Devralacak:** Framework Geliştirme Ajanı / Subagent
- **İlgili Modül:** `ShadowsAscendancy` (Launcher Adı: **Shadows' Ascendancy**)
- **Mevcut Mod Versiyonu:** `v1.0.0.5`

### Tamamlanan Çalışmalar ve Modül Mimarisi

#### A. Görev 1: Gölgelerin Fısıltısı (`QLF_GolgelerinFisiltisi`)
- **Dosya:** `ShadowsAscendancy/ModuleData/Questlines/quest_golgelerinfisiltisi.xml`
- **Konsept:** Klan Seviyesi 2 olan oyuncunun yerel bir şehirde tüccarın kervanlarına saldıran haydutları ve arkasındaki şehir çetesini araştırması.
- **Tetikleyici & Akış:**
  - `OnSettlementEnter` ile oyuncu şehre girdiğinde tüccarla zorunlu diyalog (`ForceConversation`) açılır.
  - Haritada `HedefKervan` (Kaçakçı Kervanı) spawn edilir ve oyuncu onları yendiğinde Şifreli Defter (`sifreli_defter`) ele geçirilir.
  - Şehre dönülerek Çete Lideri (`KacakciLideri`) ile yüzleşilir.
  - **Kanun Yolu:** Çete liderinin rüşveti reddedilir. Lider oyuncuyu tehdit eder. Şehir içinde savaş başlatmak motor açısından riskli olduğundan senaryo revize edildi: Şehir menüsünden güvenle çıkılır (`Stage4_PusuBekleyis`). Oyuncu yerleşkeden açık haritaya çıktığında (`<Condition Type="!IsPlayerInSettlement" />`), 50 kişilik çete pususu (`CetePartisi`) belirir, tehdit diyaloğu açılır ve savaş başlar. Zafer sonrası görev tamamlanır (`GolgelerinFisiltisi_KanunAdami` ve `GolgelerinFisiltisi_Bitti` bayrakları set edilir).
  - **Suç Yolu (Soylu Şantajı):** Oyuncu bir lord olarak suikast gibi ayak işleri yapmayı reddeder; çete lideriyle anlaşıp tüccarı haraca bağlar. Tüccar korkudan boyun eğer, kan dökülmeden +10.000 Denar kazanılır (`GolgelerinFisiltisi_KaranlikOrtak` ve `GolgelerinFisiltisi_Bitti` bayrakları set edilir).
- **Kritik Düzeltmeler:**
  - `NextStageId` boş bırakıldığı için aşama geçişlerinin tıkanması sorunu tüm stage'lerde düzeltildi.
  - Görevin başka bir şehre girildiğinde yeniden başlamasını engellemek için `<Prerequisites>` altına `<Condition Type="!GlobalFlag" ObjectId="GolgelerinFisiltisi_Bitti" Amount="1" />` eklendi.

#### B. Görev 2: Karaborsa Savaşları (`QLF_KaraborsaSavaslari`)
- **Dosya:** `ShadowsAscendancy/ModuleData/Questlines/quest_karaborsasavaslari.xml`
- **Konsept:** Klan Seviyesi 3 olan ve ilk görevi bitiren oyuncuya karşı, ilk görevdeki seçimine göre yozlaşmış bir lordun suikast tertiplemesi.
- **Tetikleyici & Akış:**
  - Haritada hareket halindeyken `AutoStart` ile 60 kişilik kiralık katil grubu (`KiralikKatiller`) oyuncuya saldırır.
  - Katiller yenildiğinde haritada 1 kişilik Yaralı Komutan (`KatilKomutan`) partisi bekletilir (`Hold`).
  - Komutanla yapılan sorgu diyaloğunda ilk görevdeki seçime göre (`GlobalFlag` üzerinden `KanunAdami` vs `KaranlikOrtak`) komutanın itirafı dinamik olarak değişir. Komutan `lordun_muhurlu_emri` eşyasını verir ve partisi `DestroyParty` ile güvenle haritadan silinir.
  - Oyuncu `Trait(Devious)` filtresiyle belirlenmiş olan `YozlasmisLord` ile yüzleşir:
    1. **Şantaj:** 20.000 Denar alınır, görev biter (Onur/Renown kaybedilir).
    2. **Savaş:** `DeclareWar` aksiyonu ile krallıkla savaş ilan edilir.
    3. **Politik Adalet:** Kanıt krallık hükümdarına (`Hukumdar`) sunulur. Hükümdar oyuncuya +30 Nüfuz ve +30 İlişki verir, yozlaşmış lordu `MakeFugitive` eylemi ile sürgüne gönderir.
- **Kritik Düzeltmeler:**
  - `LeaveEncounter` aksiyonu `DestroyParty`'den sonra çalıştırılarak encounter context'inin erken silinmesi engellendi.
  - `NextStageId` zinciri eksiksiz bağlandı.

#### C. Lokalizasyon (Localization) Sistemi
- XML'lerdeki tüm diyalog, log ve unvan metinleri İngilizce (Base Language) formatına dönüştürüldü ve `{=shdw_...}` formatında benzersiz string ID'leri tanımlandı.
- `ShadowsAscendancy/ModuleData/Languages/language_data.xml` eklendi.
- `ShadowsAscendancy/ModuleData/Languages/TR/std_ShadowsAscendancy_tr.xml` dosyası oluşturuldu ve hem 1. hem de 2. görevin tüm Türkçe metinleri eksiksiz olarak işlendi.

#### D. Otomasyon & Build Pipeline (CI/CD)
- `ShadowsAscendancy/Build/UpdateVersion.ps1` scripti yazıldı (`vMAJOR.UPDATE.HOTFIX.DEBUG` formatı).
- `ShadowsAscendancy.csproj` içerisine PreBuild target olarak bağlandı.
- PostBuild target ile `SubModule.xml`, `ModuleData/` ve `.dll` dosyaları doğrudan Bannerlord'un `Modules/ShadowsAscendancy` dizinine deploy edilmektedir.
- Hem `D:\SteamLibrary` hem de `C:\Program Files (x86)\Steam` Steam kütüphane yollarını destekleyecek dinamik path koşulu eklendi.

### Motor Seviyesinde Talep Edilen & Tamamlanan Özellikler (IR Geçmişi)
- `<Condition Type="!IsPlayerInSettlement" />`: Görev 1'de şehir dışına çıkış pusu mekaniğinin açık haritada tetiklenmesi amacıyla talep edildi. C# motorunda başarıyla desteklendiği doğrulandı.

### Framework Ajanına Notlar ve Sonraki Adımlar
1. **Oyun İçi Doğrulama:**
   - Görev 1'de şehirden çıktıktan hemen sonra `!IsPlayerInSettlement` koşulunun açık harita saatlik döngüsünde tetiklenip 50 kişilik çete pususunu başarıyla spawn ettiğini teyit edin.
2. **Quest 3 (Genişletme):**
   - Kullanıcı talep ederse, Karaborsa Savaşları'nın ardından sürgüne gönderilen lordun intikamı veya Kalradya çapında bir yeraltı karteli kurma temalı 3. bir final görevi tasarlanabilir.
3. **Disiplin Kuralları:**
   - C# kodlarına müdahale edilmemeli (XML yazarı yetki alanı dışındadır).
   - Her başarılı derleme ve testin ardından `dotnet build`, `git add .`, `git commit -m "..."` ve `git push` adımları eksiksiz yürütülmelidir.
