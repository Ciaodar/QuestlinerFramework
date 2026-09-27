# Handoff Summaries

## 1. Shadows' Ascendancy (Gölgelerin Yükselişi) Quest Writer -> Framework Agent
- **Tarih:** 27 Eylül 2026
- **Görevi Devreden:** Shadows' Ascendancy Senaryo & XML Geliştirme Ajanı
- **Görevi Devralacak:** Framework Geliştirme Ajanı / Yeni Subagent
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
  - **Kanun Yolu:** Çete liderinin rüşveti reddedilir. Lider oyuncuyu tehdit eder. Şehir içi dövüş yerine güvenli akış: Şehir menüsünden çıkılır (`Stage4_PusuBekleyis`). Oyuncu yerleşkeden açık haritaya çıktığında (`<Condition Type="!IsPlayerInSettlement" />`), 50 kişilik çete pususu (`CetePartisi`) belirir, tehdit diyaloğu açılır ve savaş başlar. Zafer sonrası görev tamamlanır (`GolgelerinFisiltisi_KanunAdami` ve `GolgelerinFisiltisi_Bitti` bayrakları set edilir).
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

---

## 2. Demir Ahit (Iron Covenant) Quest Writer -> Framework Agent
- **Tarih:** 27 Eylül 2026
- **Görevi Devreden:** Demir Ahit Senaryo & XML Geliştirme Ajanı
- **Görevi Devralacak:** Framework Geliştirme Ajanı / Subagent
- **İlgili Modül:** `DemirAhit` (Launcher Adı: **Iron Covenant**)
- **Mevcut Mod Versiyonu:** `v1.0.0.1`

### Tamamlanan Çalışmalar ve Modül Mimarisi

#### A. Görev Zinciri Dosyaları (`DemirAhit/ModuleData/Questlines/`)
Klan seviyesi ilerlemesine bağlı 4 aşamalı ana hikaye zinciri tamamlanmıştır:
1. `QLF_DemirAhit_Cirak.xml` (Apprentice's Oath - Clan Tier 2):
   - Davet ve toplama aşamaları bölünerek doğal diyalog akışı kuruldu (`Stage1_Davet` -> `Stage1_Toplama`).
   - 10.000 Altınlık alternatif kestirme satın alma yolu eklendi (`Stage2_KestirmeYol`).
   - Pusu savaşı (`KiralikKatiller`), `BattleResolvedAs` zafer kontrolü ve başarısızlıkta partinin `DestroyParty` ile silinmesi sağlandı.
2. `QLF_DemirAhit_Kalfa.xml` (Journeyman's Trial - Clan Tier 3):
   - `Stage2A_Nobet`: Köyde nöbet tutma ve gelen 60 kişilik `Sabotajcilar` haydut grubunu savuşturma.
   - `Stage2B_Takip`: Rakip tüccarla yüzleşme; 2.000 altın rüşvet verme, `SkillCheck(Roguery, 50)` ile tehdit etme veya korumalarla (`Korumalar`) savaşma dallanmaları.
3. `QLF_DemirAhit_Usta.xml` (Master's Burden - Clan Tier 4):
   - Demircinin kardeşi Kalan'ı düşman yerleşkesinin zindanından kurtarma operasyonu.
   - Gizli sızma (`SkillCheck(Roguery, 80)`), yakalanma durumunda 150 kişilik muhafız pususu (`GarnizonMuhafizlari`), kuşatma veya fidye ödeme seçenekleri.
4. `QLF_DemirAhit_Miras.xml` (Forging the Legacy - Clan Tier 5):
   - 50.000 altın değerindeki "Kalbin Ateşi" ritüelinin tamamlanması ve efsanevi kılıcın dövülmesi.
   - Nihai seçim: Kılıcı klan mirası olarak saklama (`AhitKilici_Efsanevi`) VEYA krala sunma (`IsVassalOfTargetFaction` koşuluna bağlı, bağımsız klanlarda gizlenir).
5. `DemirAhit/ModuleData/items.xml`:
   - `YildizDemiri`, `GecicilAhitKilici`, `KalfaMuhru`, `KalbinAtesi` ve `AhitKilici_Efsanevi` eşyaları tanımlandı.

#### B. Uluslararasılaştırma ve Yerelleştirme (Localization)
- Tüm görev ve eşya XML dosyalarındaki ham metinler standart İngilizceye dönüştürüldü.
- İki dilli dil paketleri oluşturuldu:
  - `DemirAhit/ModuleData/Languages/EN/std_IronCovenant_en.xml` (İngilizce)
  - `DemirAhit/ModuleData/Languages/TR/std_IronCovenant_tr.xml` (Türkçe)

#### C. Otomasyon, Derleme ve Dağıtım (CI/CD Pipeline)
- `DemirAhit/Build/UpdateVersion.ps1`: `vMAJOR.UPDATE.HOTFIX.DEBUG` formatında versiyonlama scripti.
- `DemirAhit.csproj`: PreBuild ile versiyon artırımı; PostBuild ile `SubModule.xml`, `ModuleData/` ve derlenen `.dll` dosyalarının Bannerlord `Modules/DemirAhit` klasörüne `xcopy` ile otomatik aktarımı.
- `SubModule.xml`: Eksik olan `<DependedModule Id="QuestlinerFramework" />` bağımlılığı eklendi, Launcher adı "Iron Covenant" olarak güncellendi.

### Motor Seviyesinde Talep Edilen & Tamamlanan Özellikler (IR Geçmişi)
1. `BattleResolvedAs` Koşulu: Mobil parti savaş sonuçlarını (`Victory`, `Defeat`, `Retreat`, `DestroyedByAI`) doğrulamak için eklendi.
2. `SaveHeroToGlobal` & `GlobalHero(Key)`: Demirci, tüccar ve Kalan karakterlerinin görevler boyunca kalıcı ve tutarlı olarak seçilmesini sağladı.
3. `IsOptional="true"` (TargetResolver): Kral gibi opsiyonel hedeflerin bağımsız klanlarda null dönmesi halinde görevin kilitlenmesini önledi.
4. `IsPrisonerInSettlement` Filtresi: Hedefin sadece esir değil, kale/şehir zindanında tutulmasını garanti ederek `SameSettlementAs` null hatasını engelledi.
5. `DestroyParty` Aksiyonu: Kaçılan veya zaman aşımına uğrayan pusu partilerinin haritadan silinmesini sağladı.

### Framework Ajanına Notlar ve Bekleyen İstek (Pending IR)
- **Bekleyen Özellik Talebi (`IR_DisabledDialogHints.md`):** Koşulu sağlanmayan diyalog seçeneklerinin tamamen gizlenmesi yerine gri/tıklanamaz olarak görünmesi ve üzerine gelindiğinde neden seçilemediğini belirten ipucu (`DisabledHint`) göstermesi için C# tarafında geliştirme beklenmektedir.
- **Diyalog Başlangıçları:** Oyundaki tanışma (`MakeMet`) pencerelerinin görev başlangıcını ezmesini engellemek için diyalog girişleri `InputToken="hero_main_options"` olarak ayarlanmıştır.
- **Hedef Takibi:** Metinlerde `{HedefAlias_Link}` formatı kullanılarak otomatik harita/ansiklopedi takibi (mavi ünlem) sağlanmıştır.

---

## 3. Scavenger Pigeons (Çapulcu Güvercinler) Quest Writer -> Framework Agent
- **Tarih:** 27 Eylül 2026
- **Görevi Devreden:** Scavenger Pigeons Senaryo & XML Geliştirme Ajanı
- **Görevi Devralacak:** Framework Geliştirme Ajanı / Subagent
- **İlgili Modül:** ScavengerPigeons
- **Mevcut Mod Versiyonu:** 1.0.0.4

### Tamamlanan Çalışmalar ve Modül Mimarisi

#### A. Görev Zinciri
1. QLF_HainGuvercin: Örgütle ilk temas, kaçan kuryeyi koruma veya satma.
2. QLF_ItibarDersi: Tüccarı döverek veya rüşvet alarak örgütü kandırma.
3. QLF_YaDogruysa: Masum rahibi öldürme veya gerçeği araştırma (Sadakat testi başlangıcı).
4. QLF_AnavataninIhaneti: İhanetle suçlanma ve "Dost" ile "Zanaatkar" aracılığıyla büyük final testi. SaveHeroToGlobal ile bu iki anahtar karakter hafızaya kaydedilir.
5. QLF_KopukBaglar: Bir önceki görevden 1 hafta sonra gizlice başlar (AutoStart + İlk aşamada 7 günlük TimeoutInDays beklemesi). Gerçek hain olan Zanaatkar'ın 100 kişilik sahte konvoyu (Konvoy) ile savaşılır (BattleResolvedAs kontrolü).

#### B. Motor Seviyesinde Kritik Kullanımlar (Notlar)
- SaveHeroToGlobal / GlobalHero(Key): 4. Görevde yaratılan anahtar karakterlerin 5. Görevde de aynı kişiler olması bu sistemle güvenceye alınmıştır.
- TimeoutInDays Bekleme Sistemi: Save dosyasını şişirmemek için, 4. görev anında başarıyla kapatılırken, 1 haftalık zaman atlaması 5. görevin (QLF_KopukBaglar) Stage0_Bekleme aşamasının başında çalıştırılmıştır.
- ChangeRelation: Bannerlord'daki Charm (Cazibe) çarpanı nedeniyle çok yüksek (+120) sayılara ulaştığından Amount="5" gibi düşük tutulmuştur.

#### C. Otomasyon ve Dağıtım (CI/CD)
- ScavengerPigeons/Build/UpdateVersion.ps1 kullanılarak PreBuild event üzerinden versiyonlama (1.0.0.4) ve Bannerlord Modules klasörüne Deployment süreçleri .csproj içerisinde otomatikleştirildi.
