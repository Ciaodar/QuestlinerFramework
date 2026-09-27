# Demir Ahit (Iron Covenant) Serisi — Kapsamlı Senaryo ve Teknik Tasarım Raporu (Nihai Sürüm)

Bu belge, Questliner Framework üzerinde çalışan, **klan Tier ilerlemesine bağlı** 4 aşamalı ana görev zincirinin (Iron Covenant / Demir Ahit) nihai teknik ve senaryo dökümünü içerir.

Tüm görevler Framework standartlarına, Bannerlord native mekaniklerine ve çift dilli (EN/TR) yerelleştirme mimarisine tam uyumlu olarak uygulanmıştır.

---

## 0. Altyapı ve Global Durum Yönetimi

Demir Ahit serisi klan seviyesi ilerledikçe otomatik (`AutoStart`) tetiklenir:
- Görevler arası karakter sürekliliği `SaveHeroToGlobal` eylemi ve `GlobalHero(Key)` filtresi ile sağlanır (Örn: Çırak görevinde seçilen demirci ve tüccar, sonraki tüm görevlerde aynı kişiler olarak hafızadan çağrılır).
- Hedeflerin haritada mavi ünlem ile takip edilmesi ve tıklanabilir bağlantı olması için metinlerde `{HedefAlias_Link}` standardı kullanılır.
- İlk karşılaşma diyaloglarının native "tanışma" ekranlarıyla çakışmaması için oyuncu giriş diyalogları `hero_main_options` token'ı ile ana konuşma menüsüne entegre edilmiştir.
- Başarısızlık veya zaman aşımında (Timeout) haritada başıboş kalan partiler `DestroyParty` eylemi ile temizlenir.

---

## 1. Görev Zinciri Detayları

### Görev 1: Çırağın Yemini (QLF_DemirAhit_Cirak / Apprentice's Oath)
- **Önkoşul:** `ClanTier >= 2` ve `!GlobalFlag(DemirAhit_Baslatildi)`
- **Süre Sınırı:** 45 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (Artisan, Nearest)
  - `HammadeKaynagi` (Village, Nearest)
  - `RakipTuccar` (Merchant, Random)
- **Akış ve Aşamalar:**
  1. **Davet (Stage1_Davet):** Oyuncunun şanı demircinin kulağına gider. Demirci klan kılıcını yenilemeyi teklif eder.
  2. **Toplama (Stage1_Toplama):** Oyuncu nerede bulacağını sorduğunda demirci `{HammadeKaynagi_Link}` civarını işaret eder. 5x Yıldız Demiri (`YildizDemiri`) toplanması istenir.
  3. **Kestirme Yol Seçeneği (Stage2_KestirmeYol):** Oyuncu 10.000 altın ödeyerek ustanın arkasındaki hazır kılıcı satın alıp görevi anında başarıyla tamamlayabilir.
  4. **Pusu (Stage2_Pusu):** 2 gün içinde teslim edilmezse Rakip Tüccar'ın kiraladığı 30 kişilik `KiralikKatiller` saldırır. `BattleResolvedAs` kontrolü ile zafer aranır. Kaçılır/yenilinirse 2 maden silinir, parti `DestroyParty` ile yok edilir ve toplama aşamasına dönülür.
  5. **Teslimat (Stage3_Teslim):** Madenler teslim edilir.
- **Ödül:** `GecicilAhitKilici`, +50 Renown, +15 İlişki, `DemirAhit1_Basarili=1`.

---

### Görev 2: Kalfa Sınavı (QLF_DemirAhit_Kalfa / Journeyman's Trial)
- **Önkoşul:** `ClanTier >= 3` ve `GlobalFlag(DemirAhit1_Basarili) == 1`
- **Süre Sınırı:** 60 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (`GlobalHero(DemirAhit_AnaDemirci)`)
  - `RakipTuccar` (`GlobalHero(DemirAhit_RakipTuccar)`)
- **Akış ve Aşamalar:**
  1. **Uyarı (Stage1_Uyari):** Demirci, atölyesine gece baskınları planlandığını ve arkasında rakip tüccarın olduğunu söyler.
  2. **Yol A - Nöbet (Stage2A_Nobet & Stage2A_Savas):** Köyde 5 gün nöbet tutulur. Süre sonunda 60 kişilik `Sabotajcilar` saldırır. `BattleResolvedAs` zaferi ile aşama geçilir.
  3. **Yol B - Yüzleşme (Stage2B_Takip & Stage2B_Savas):** Tüccar ile yüzleşilir:
     - *Rüşvet:* 2.000 altın vererek anlaşma.
     - *Tehdit:* `SkillCheck(Roguery, 50)` ile tüccarı korkutma.
     - *Savaş:* Tehdit tutmazsa 40 kişilik `Korumalar` saldırır, yenilmeleri gerekir.
  4. **Başarı (Stage3_Basari):** Sorun çözülür.
- **Ödül:** `KalfaMuhru`, +10 İlişki, `DemirAhit2_Basarili=1`.

---

### Görev 3: Ustanın Yükü (QLF_DemirAhit_Usta / Master's Burden)
- **Önkoşul:** `ClanTier >= 4` ve `GlobalFlag(DemirAhit2_Basarili) == 1`
- **Süre Sınırı:** 90 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (`GlobalHero(DemirAhit_AnaDemirci)`)
  - `Kalan` (Hero, `IsPrisonerInSettlement`, Random)
  - `DusmanKale` (Settlement, `SameSettlementAs(Kalan)`)
- **Akış ve Aşamalar:**
  1. **Haber (Stage1_Haber):** Demircinin kardeşi Kalan bir kale zindanına esir düşmüştür.
  2. **Kurtarma (Stage2_Kurtarma):** Oyuncu kaleyi kuşatabilir, fidye ödeyebilir veya zindana sızabilir:
     - *Gizli Sızma:* `SkillCheck(Roguery, 80)` ile Kalan zindandan kaçırılır (`RemovePrisoner`).
     - *Yakalanma:* Yetenek yetmezse 150 kişilik `GarnizonMuhafizlari` saldırır. Yenilgi durumunda muhafızlar `DestroyParty` ile temizlenir ve kurtarma tekrar denenebilir.
  3. **Zafer (Stage3_Zafer):** Kalan özgür kaldığında (`!TargetIsPrisoner`) görev tamamlanır ve Kalan global hafızaya kaydedilir (`DemirAhit_Kalan`).
- **Ödül:** +20 Demirci İlişkisi, +20 Kalan İlişkisi, +30 Renown, `DemirAhit3_Basarili=1`.

---

### Görev 4: Mirasın Dövülüşü (QLF_DemirAhit_Miras / Forging the Legacy)
- **Önkoşul:** `ClanTier >= 5` ve `GlobalFlag(DemirAhit3_Basarili) == 1`
- **Süre Sınırı:** 60 Gün
- **Hedefler:**
  - `EfsaneviDemirci` (`GlobalHero(DemirAhit_AnaDemirci)`)
  - `Kalan` (`GlobalHero(DemirAhit_Kalan)`)
  - `IttifakKrali` (`KingdomLeader, SameFactionAs(Player)`, `IsOptional="true"`)
- **Akış ve Aşamalar:**
  1. **Haber (Stage1_Haber):** Kılıç tamamlanmak üzeredir, son ruhu vermek için "Kalbin Ateşi" ritüeli gerekir.
  2. **Kaynak (Stage2_Kaynak):** 50.000 altın toplanıp demirciye teslim edilir (`TakeGold Amount="50000"`).
  3. **Karar (Stage3_Sonuc):** Kılıç dövülür ve oyuncuya yol ayrımı sunulur:
     - *Klan Mirası:* Kılıcı klanı için saklar → `AhitKilici_Efsanevi`, +150 Renown, +200 Influence.
     - *Krala Hediye:* Sadece oyuncu bir krallığın vasalıysa (`IsVassalOfTargetFaction`) görünür. Kılıç krala sunulur → +50 Renown, +500 Influence, +50 Kral İlişkisi.
- **Sonuç:** `DemirAhit4_Basarili=1`, seri başarıyla noktalanır.

---

## 2. Özel Eşyalar (items.xml)
- `YildizDemiri` (Goods, Value: 100)
- `GecicilAhitKilici` (OneHandedSword, Cut: 70, Thrust: 35)
- `KalfaMuhru` (Goods / Amulet, Value: 500)
- `KalbinAtesi` (Goods, Value: 5000)
- `AhitKilici_Efsanevi` (OneHandedSword, Cut: 90, Thrust: 45, Speed: 105)

---

## 3. Bekleyen İstekler (IR)
- **`IR_DisabledDialogHints.md`**: Şartları sağlanmayan diyalog seçeneklerinin tamamen gizlenmesi yerine gri/tıklanamaz olarak görünmesi ve üzerine gelindiğinde neden seçilemediğini belirten ipucu (`DisabledHint`) göstermesi için C# Framework motoruna iletilen özellik talebi.
