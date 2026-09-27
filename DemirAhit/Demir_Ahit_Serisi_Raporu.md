# Demir Ahit (Iron Covenant) Serisi - Kapsamlı Senaryo ve Teknik Tasarım Raporu (Güncel ve Nihai Sürüm)

Bu belge, Questliner Framework üzerinde çalışan, **klan Tier ilerlemesine bağlı** 4 aşamalı ana görev zincirinin (Iron Covenant / Demir Ahit) nihai teknik, senaryo ve yerelleştirme dökümünü içerir.

Modülün Launcher adı **Iron Covenant**, teknik ID'si **DemirAhit**'tir.

---

## 0. Altyapı, Dil Mimarisi ve SubModule Yapılandırması

### A. SubModule.xml ve Özel Eşyalar (items.xml)
- Modül adı uluslararası standart gereği İngilizce olarak `<Name value="Iron Covenant" />` şeklinde tanımlanmıştır.
- TaleWorlds motorunun `ModuleData/items.xml` dosyasındaki özel eşyaları kampanya sözlüğüne başarıyla yüklemesi için `<Xmls>` bloğu `<IncludedGameTypes>` ile eksiksiz yapılandırılmıştır:
  ```xml
  <Xmls>
    <XmlNode>
      <XmlName id="Items" path="items" />
      <IncludedGameTypes>
        <GameType value="Campaign" />
        <GameType value="CampaignStoryMode" />
      </IncludedGameTypes>
    </XmlNode>
  </Xmls>
  ```
- Bu kayıt sayesinde `YildizDemiri`, `GeciciAhitKilici`, `KalfaMuhru`, `KalbinAtesi` ve `AhitKilici_Efsanevi` eşyaları oyuna ve envanter işlemlerine (`GiveItem`, `TakeItem`, `HasItem`) eksiksiz dahil olur. Eşyalarda `is_merchandise="false"` kullanılarak pazarlarda rastgele satılmaları engellenmiştir. Silah hasarları Bannerlord native standardına uygun olarak `swing_damage` / `swing_damage_type` şeklinde tanımlanmıştır.

### B. Çift Dilli Yerelleştirme (Localization) ve language_data.xml
- TaleWorlds motorunun dil dosyalarını otomatik tanıması için `ModuleData/Languages` hiyerarşisi BUTR/Native standartlarına uygun hale getirilmiştir:
  - `ModuleData/Languages/EN/language_data.xml` (İngilizce dil paketi: `std_IronCovenant_en.xml`)
  - `ModuleData/Languages/TR/language_data.xml` (Türkçe dil paketi: `std_IronCovenant_tr.xml`)
- Her iki dil dosyasında tam 90 adet string ID birebir örtüşmekte, Kalradya evrenine uygun zengin, edebi ve sürükleyici bir ortaçağ üslubu kullanılmaktadır.

### C. Motor Desteği: Devre Dışı Diyalog İpuçları (ShowWhenDisabled & DisabledHint)
- C# motorunun sunduğu `ShowWhenDisabled="true"` ve `DisabledHint="{=hint_id}Text"` özelliği tüm görevlerdeki kritik seçeneklere uygulanmıştır:
  - 10.000 Altın Kestirme Kılıç Alımı (`{=da_hint_gold}10.000 Altın Gerekiyor`)
  - Köy Muhtarından 2.000 Altına Yedek Maden Satın Alımı (`{=da_hint_muhtar_gold}2.000 Altın Gerekiyor`)
  - Tüccara 2.000 Altın Rüşvet Verme (`{=da_hint_bribe_gold}2.000 Altın Gerekiyor`)
  - Serserilik Becerisi Kontrolleri (`{=da_hint_roguery_50}`, `{=da_hint_roguery_80}`)
  - 50.000 Altınlık Ritüel Ateşi (`{=da_hint_heart_gold}50.000 Altın Gerekiyor`)
  - Krala Kılıç Sunma Şartı (`{=da_hint_vassal_req}Bir Krallığa Bağlı Vasal Olmalısınız`)

---

## 1. Görev Zinciri Detayları

### Görev 1: Çırağın Yemini (QLF_DemirAhit_Cirak / Apprentice's Oath)
- **Önkoşul:** `ClanTier >= 2` ve `!GlobalFlag(DemirAhit_Baslatildi)`
- **Süre Sınırı:** 45 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (`TargetType="Hero" Filters="Artisan" Selector="Nearest"`)
  - `HammadeKaynagi` (`TargetType="Settlement" Filters="Village" Selector="Nearest"`)
  - `KoyMuhtari` (`TargetType="Hero" Filters="RuralNotable" RequiredSettlementAlias="HammadeKaynagi" Selector="Nearest"`)
  - `RakipTuccar` (`TargetType="Hero" Filters="Merchant" Selector="Random"`)
- **Akış ve Maden Toplama Yolları:**
  1. **Davet (Stage1_Davet):** Oyuncunun şanı demircinin kulağına gider. Demirci klan kılıcını yenilemeyi teklif eder. 5 parça Yıldız Demiri (`YildizDemiri`) ister.
  2. **Kestirme Yol:** Demirciyle konuşurken sabırsız oyuncular için 10.000 altın ödeyerek ustanın arkasındaki dövülmüş kılıcı satın alma seçeneği (`Demirci_Teklif_Kestirme`, `ShowWhenDisabled="true"` ve `DisabledHint` ile).
  3. **Yıldız Demiri Tedariki (Stage1_Toplama):** Demirci, oyuncuyu maden ocağının bulunduğu `{HammadeKaynagi_Link}` köyüne ve köy büyüğü `{KoyMuhtari_Link}`'na yönlendirir. Oyuncu köye vardığında muhtar, madenlerin `{RakipTuccar_Link}`'ın kiraladığı haydutlar tarafından çalındığını söyler:
     - **Yol A (Savaş ve Ganimet):** Muhtarın yönlendirmesiyle 30 kişilik `KiralikKatiller` pususuna saldırılır. Zafer sonrası (`Stage2_Pusu`), 5 adet `YildizDemiri` ganimet olarak heybeye eklenir (`GiveItem Amount="5"`).
     - **Yol B (Köy Ambarından Satın Alma):** Muhtara madencilerin zararını karşılamak için 2.000 altın ödenerek ocak mahzenindeki 5 adet yedek Yıldız Demiri doğrudan satın alınır (`ShowWhenDisabled="true"`, `DisabledHint`).
     - **Yol C (Zaman Aşımı Pususu):** Oyuncu 3 gün içinde muhtarla görüşmezse haydutlar açık haritada oyuncuya doğrudan pusu kurar. Zaferde yine madenler ele geçirilir.
  4. **Teslimat ve Başarı (Stage3_Teslim & Stage3_Basari_Normal):** 5 adet Yıldız Demiri `{EfsaneviDemirci_Link}`'e teslim edilir. Diyalog sonucunda doğrudan `Stage3_Basari_Normal` aşamasına geçilir (`IsSuccessNode="true"`). Demirci madenleri alıp (`TakeItem Amount="5"`), oyuncuya `GeciciAhitKilici`, +50 Renown ve +15 İlişki verir. `DemirAhit1_Basarili=1` bayrağı set edilir ve görev başarıyla noktalanır.

---

### Görev 2: Kalfa Sınavı (QLF_DemirAhit_Kalfa / Journeyman's Trial)
- **Önkoşul:** `ClanTier >= 3` ve `GlobalFlag(DemirAhit1_Basarili) == 1`
- **Süre Sınırı:** 60 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (`GlobalHero(DemirAhit_AnaDemirci)`)
  - `RakipTuccar` (`GlobalHero(DemirAhit_RakipTuccar)`)
- **Akış ve Aşamalar:**
  1. **Uyarı (Stage1_Uyari):** Demirci, rakip tüccarın atölyeyi ateşe vermek üzere kundakçılar tuttuğunu bildirir.
  2. **Yol A - Nöbet (Stage2A_Nobet & Stage2A_Savas):** Atölye çevresinde 5 gün nöbet tutulur. Gece vakti saldıran 60 kişilik `Sabotajcilar` grubu savaşta püskürtülür.
  3. **Yol B - Yüzleşme (Stage2B_Takip & Stage2B_Savas):** Tüccar ile doğrudan yüzleşilir:
     - *Rüşvet:* 2.000 altın ödenerek kalleşliğe son verdirilir (`ShowWhenDisabled="true"`).
     - *Tehdit:* `SkillCheck(Roguery, 50)` ile tüccar korkutulur (`ShowWhenDisabled="true"`).
     - *Savaş:* 40 kişilik paralı asker grubu (`Korumalar`) darmadağın edilir.
  4. **Başarı (Stage3_Basari):** Demirci oyuncuya `KalfaMuhru` takdim eder, +10 İlişki ve +30 Renown kazanılır. `DemirAhit2_Basarili=1` set edilir.

---

### Görev 3: Ustanın Yükü (QLF_DemirAhit_Usta / Master's Burden)
- **Önkoşul:** `ClanTier >= 4` ve `GlobalFlag(DemirAhit2_Basarili) == 1`
- **Süre Sınırı:** 90 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (`GlobalHero(DemirAhit_AnaDemirci)`)
  - `Kalan` (Hero, `IsPrisonerInSettlement`, Random)
  - `DusmanKale` (Settlement, `SameSettlementAs(Kalan)`)
- **Akış ve Aşamalar:**
  1. **Haber (Stage1_Haber):** Demircinin kardeşi Kalan, kadim su verme tuzlarını taşırken esir düşmüştür.
  2. **Kurtarma (Stage2_Kurtarma):** Oyuncu kaleyi kuşatabilir, fidye ödeyebilir veya zindana sızabilir:
     - *Gizli Sızma:* `SkillCheck(Roguery, 80)` ile nöbetçiler sessizce atlatılır (`ShowWhenDisabled="true"`). Başarılı sızma doğrudan `Stage3_Zafer`'e ulaştırır.
     - *Açık Savaş:* Zindanda yakalanılırsa 150 kişilik kale garnizonu (`GarnizonMuhafizlari`) ile savaşılır.
  3. **Zafer (Stage3_Zafer):** Kalan kurtarıldığında (`!TargetIsPrisoner`) görev tamamlanır; Kalan hafızaya alınır (`DemirAhit_Kalan`). +20 Demirci İlişkisi, +20 Kalan İlişkisi, +30 Renown. `DemirAhit3_Basarili=1` set edilir.

---

### Görev 4: Mirasın Dövülüşü (QLF_DemirAhit_Miras / Forging the Legacy)
- **Önkoşul:** `ClanTier >= 5` ve `GlobalFlag(DemirAhit3_Basarili) == 1`
- **Süre Sınırı:** 60 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (`GlobalHero(DemirAhit_AnaDemirci)`)
  - `Kalan` (`GlobalHero(DemirAhit_Kalan)`)
  - `IttifakKrali` (`KingdomLeader, PlayerFaction`, `IsOptional="true"`)
- **Akış ve Aşamalar:**
  1. **Haber (Stage1_Haber):** İkiz ustalar çeliği bin kat dövmüştür; kılıca ruh vermek için "Kalbin Ateşi" ritüeli gerekir.
  2. **Kaynak (Stage2_Kaynak):** 50.000 altın toplanıp demirciye teslim edilir (`TakeGold Amount="50000"`, `ShowWhenDisabled="true"`).
  3. **Kader Seçimi (Stage3_Sonuc):** Efsanevi kılıç hazır olduğunda nihai yol ayrımı sunulur:
     - *Klan Mirası:* Kılıç klanın kutsal yadigarı olarak kuşanılır → `AhitKilici_Efsanevi`, +150 Renown, +200 Influence.
     - *Krala Hediye:* Oyuncu bir krallığın vasalıysa (`IsVassalOfTargetFaction`, `ShowWhenDisabled="true"`) kılıç krala takdim edilir → +50 Renown, +500 Influence, +50 Kral İlişkisi.
- **Sonuç:** `DemirAhit4_Basarili=1`, tüm seri görkemli bir finalle tamamlanır.

---

## 2. Özel Eşyalar (items.xml)
- `YildizDemiri` (Goods, Value: 100, Mesh: iron_ingot, `is_merchandise="false"`)
- `GeciciAhitKilici` (OneHandedSword, Swing: 70, Thrust: 35, Speed: 95, `is_merchandise="false"`)
- `KalfaMuhru` (Goods / Amulet, Value: 500, Mesh: jewelry_amulet, `is_merchandise="false"`)
- `KalbinAtesi` (Goods, Value: 5000, Mesh: ore_iron, `is_merchandise="false"`)
- `AhitKilici_Efsanevi` (OneHandedSword, Swing: 90, Thrust: 45, Speed: 105, Value: 20000, `is_merchandise="false"`)

---

## 3. CI/CD & Derleme Durumu
- `DemirAhit.csproj` içerisine `D:\SteamLibrary` ve `C:\Program Files (x86)\Steam` çoklu kütüphane desteği entegre edilmiştir.
- `dotnet build` komutu PreBuild versiyon artırımını (`UpdateVersion.ps1`) ve PostBuild dosya dağıtımını (`xcopy`) sıfır hata ve sıfır uyarıyla tamamlamaktadır.
