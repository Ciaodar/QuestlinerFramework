# Demir Ahit Serisi — Kapsamlı Senaryo ve Teknik Tasarım Raporu (Revize Edilmiş Sürüm)

Bu belge, Questliner Framework üzerinde çalışan, **klan Tier ilerlemesine bağlı** yeni bir ana görev zincirinin (Demir Ahit Serisi) ve ona çapraz etkileşen bağımsız bir yan görevin (Kervan Defteri) dökümünü içerir. 

*Not: Bu sürüm, Framework'ün mevcut teknik sınırlarına (XML kısıtlamalarına) tam uyum sağlayacak şekilde revize edilmiştir. Mantıksız veya Framework tarafından desteklenmeyen mekanikler (ör. günlük rastgele parti spawnları, tanımsız atölye hedefleri, kodda karşılığı olmayan düellolar) oyunu bozmamaları için doğal Bannerlord mekanikleriyle değiştirilmiştir.*

---

## 0. Kampanya İlerleme Sistemi (Kalıcı Hafıza)

Demir Ahit, oyuncunun klan seviyesi geliştikçe otomatik olarak tetiklenir. Görevler XML içerisinde `<Prerequisites>` sekmesinde `<Condition Type="ClanTier" Amount="2" />` ve `<Condition Type="GlobalFlag" ObjectId="DemirAhit1_Basarili" Amount="1" />` gibi geçmiş görev hafızalarına bakarlar.

**Kritik İhtiyaç (C# Geliştiricisi İçin):**
Görevler arası karakter tutarlılığı sağlamak (örneğin ilk görevdeki `EfsaneviDemirci`'nin, ikinci görevde de aynı NPC olmasını garantilemek) için XML motorunun `SetGlobalTarget` gibi bir aksiyona veya Alias'ları global olarak hatırlayan bir sisteme ihtiyacı vardır. Aksi takdirde Framework, her görev başlangıcında filtreye uyan *farklı* bir NPC seçebilir.

---

## 1. Görev Zinciri: Demir Ahit Serisi

### Görev 1: Çırağın Yemini (QLF_DemirAhit_Cirak)
**Önkoşul:** `ClanTier >= 2` ve `GlobalFlag DemirAhit_Baslatildi == false`
**Süre Sınırı:** 45 Gün
**Konsept:** Klan Tier 2'ye ulaştığında efsanevi bir demirci, klanın kurucusuna dövdüğü kılıcı hatırlar ve yeni bir kılıç teklif eder. 5 adet Yıldız Demiri istenir.

**Hedef Çözümlemeleri (Target Resolvers):**
1. **EfsaneviDemirci (Hero):** `Artisan` (Nearest)
2. **HammadeKaynagi (Settlement):** `Village` (Nearest)
3. **RakipTuccar (Hero):** `Merchant` (Random)

#### Aşama 1: Teklif (Stage1_Teklif)
**Hedef:** 5x Yıldız Demiri topla.
**Mekanik:** Aşama `TimeoutInDays="2"` süresine sahiptir. Oyuncu eşyaları hemen toplayıp teslim edemezse, 2 günün sonunda `OnTimeoutActions` tetiklenir ve Rakip Tüccar'ın adamları oyuncuya pusu kurar (`Stage2_Pusu` aşamasına geçilir).

#### Aşama 2: Pusu (Stage2_Pusu) 
**Durum:** Kiralık Katiller partisi oyuncuya saldırır.
**Mekanik:** 
- `TimeoutInDays="1"` ve `<Condition Type="TargetIsDead" TargetAlias="KiralikKatiller" />` koşulu aranır.
- Düşman ölürse: Başarı aşamasına geçilir, rakip tüccarla ilişki düşer ve tekrar Stage 1'e dönülür.
- 1 gün geçer ve düşman ölmezse (oyuncu yenilirse veya kaçarsa): `OnTimeoutActions` ile oyuncudan 2 Yıldız Demiri silinir ve Stage 1'e dönülür.

#### Aşama 3: Teslim (Stage3_Teslim)
**Durum:** 5 Maden Efsanevi Demirci'ye teslim edilir.
**Sonuç:** `GiveItem(GecicilAhitKilici)`, `ChangeRenown`, `SetGlobalFlag(DemirAhit1_Basarili=1)`, `CompleteQuest`.

---

### Görev 2: Kalfa Sınavı (QLF_DemirAhit_Kalfa)
**Önkoşul:** `ClanTier >= 3` + `GlobalFlag DemirAhit1_Basarili == 1`
**Süre Sınırı:** 60 Gün
**Konsept:** RakipTuccar, EfsaneviDemirci'nin köyündeki operasyonları sabote etmektedir. Demirci oyuncudan sorunu çözmesini ister.

**Hedef Çözümlemeleri:**
1. **EfsaneviDemirci (Hero):** `Artisan` (İlk görevdeki aynı NPC olması hedeflenir).
2. **RakipTuccar (Hero):** `Merchant` (İlk görevdeki aynı NPC olması hedeflenir).
*(Not: Desteklenmediği ve işlevsiz olduğu için RakipAtolyesi kaldırılmıştır.)*

#### Aşama 1: Uyarı (Stage1_Uyari)
**Durum:** Demirci, gece baskınları planlandığını haber alır. 
**Seçimler:**
- "Köyde nöbet tutarım." → `Stage2A_Nobet`
- "Tüccarın peşine düşerim." → `Stage2B_Takip`

#### Aşama 2A: Nöbet (Stage2A_Nobet)
**Durum:** 5 gün boyunca köyde savunma hazırlığı yapılır.
**Mekanik:** `TimeoutInDays="5"` ayarlanır. Süre dolduğunda (`OnTimeoutActions`), devasa bir `Sabotajcilar` haydut partisi oyuncuya saldırır. Oyuncu bu partiyi yenerse (`TargetIsDead` kontrolü) görev başarılır.

#### Aşama 2B: Takip (Stage2B_Takip)
**Durum:** Oyuncu doğrudan Rakip Tüccar ile yüzleşir.
**Seçimler:**
- **Rüşvet Ver:** `<Condition Type="HasGold" Amount="2000" />` → Anlaşma sağlanır, altın silinir, görev başarılır.
- **Tehdit Et:** `<Condition Type="SkillCheck" ObjectId="Roguery" Amount="50" />`
  - Başarılıysa: Tüccar korkar ve geri çekilir. Görev başarılır.
  - Başarısızsa: Tüccarın Korumaları (SpawnBanditParty) oyuncuya saldırır. Korumalar yenilirse görev başarılır.

#### Aşama 3: Sonuç (Stage3_Basari)
**Sonuç:** `ChangeRenown(80)`, `GiveItem(KalfaMuhru)`, `SetGlobalFlag(DemirAhit2_Basarili=1)`, `CompleteQuest`.

---

### Görev 3: Ustanın Yükü (QLF_DemirAhit_Usta)
**Önkoşul:** `ClanTier >= 4` + `GlobalFlag DemirAhit2_Basarili == 1`
**Süre Sınırı:** 90 Gün
**Konsept:** EfsaneviDemirci'nin kardeşi usta **Kalan**, düşman krallıkta esir düşmüştür.

**Hedef Çözümlemeleri:**
1. **Kalan (Hero):** `IsPrisoner` (Esir durumunda).
2. **DusmanKale (Settlement):** Kalan'ın esir tutulduğu yer (`SameSettlementAs(Kalan)`).

#### Aşama 1: Haber & Aşama 2: Kurtarma
**Durum:** Oyuncu Kalan'ı kurtarmak zorundadır.
**Seçimler:**
- **Kuşatma/Doğrudan Saldırı:** Kale ele geçirilirse Kalan kurtarılır.
- **Sızma Operasyonu:** `<Condition Type="SkillCheck" ObjectId="Roguery" Amount="80" />`
  - Başarısızlık Durumu: Motor doğrudan oyuncuyu hapse atamayacağı için, başarısızlık anında oyuncunun etrafında devasa ve yenilmesi çok zor bir "Garnizon Muhafızları" (`Amount="150"`) partisi doğar ve saldırır. Oyuncu yenilirse **Bannerlord'un doğal mekanikleri gereği** zaten o fraksiyona esir düşer. Böylece ekstra bir "Hapishane Aşamasından Kaçış" yazmaya gerek kalmadan oyunun kendi döngüsü kullanılır.

#### Aşama 3: Zafer
**Sonuç:** `ChangeRenown(120)`, `SetGlobalFlag(DemirAhit3_Basarili=1)`.

---

### Görev 4: Mirasın Dövülüşü (QLF_DemirAhit_Miras)
**Önkoşul:** `ClanTier >= 5` + `GlobalFlag DemirAhit3_Basarili == 1`
**Konsept:** Son kılıç için gerekli olan **Kalbin Ateşi**'ni elde etmek adına büyük bir fedakarlık veya askeri zafer gerekir.

**Hedef Çözümlemeleri:**
1. **IttifakKrali (Hero):** `KingdomLeader` ve `SameFactionAs(Player)`.

#### Aşama 1: Savaş Meydanı (Stage1_Savas)
**Hedef:** Düşman fraksiyonlardan 5 Lord partisini mağlup et.
**Mekanik:** `<Condition Type="DefeatedParties" TargetAlias="EnemyCulture" Amount="5" />` (Veya savaşta toplanan devasa ganimetleri satarak `HasGold="50000"` koşulunu sağla).
**Sonuç:** Koşullar sağlandığında `GiveItem(KalbinAtesi)` tetiklenir.

#### Aşama 2: Seçim (Stage3_Secim)
**Dallanma:**
- "Bu kılıç klanımızın mirası olacak." → `GiveItem(AhitKilici_Efsanevi)`, `ChangeRenown(200)`.
- "Bu kılıcı Kralımıza hediye edeceğim." → `ChangeRelation(IttifakKrali, 40)`, `ChangeInfluence(300)`.
**Ortak Sonuç:** `SetGlobalFlag(DemirAhit_Tamamlandi=1)`, `CompleteQuest`.

---

## 2. Bağımsız Yan Görev: Kervan Defteri (QLF_KervanDefteri)
*(Bu görevin yapısı zaten motor sınırlarına uygundur. Standart taşıma görevidir.)*

---

### AI Quest Yazarının Gözden Geçirme Notları:
- Motorun desteklemediği hayali XML etiketleri (`<Action Type="StartDuel">`, hayali hedefler vb.) tamamen temizlenmiştir.
- Pusu ve bekleme dinamikleri, oyunu "sonsuz döngüye" sokmamak için `TimeoutInDays` ile sınırlandırılmış, başarılı/başarısız olma koşulları motorun yerleşik `TargetIsDead` özelliğiyle sağlanmıştır.
- Oyuncunun zindana atılması gibi framework'te komutu olmayan durumlar, oyuncunun üzerine Bannerlord'un doğal savaş ve esaret mekaniklerini tetikleyecek düşmanlar gönderilerek (SpawnBanditParty) çözülmüştür.
