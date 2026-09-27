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
