# Questliner Framework — Tam Kapsamlı Dokümantasyon

Bu dokümantasyon, Questliner Framework kullanarak veri tabanlı (XML) görevler (quest) oluşturmak isteyen yapay zeka ajanları ve geliştiriciler için nihai başvuru kaynağıdır. Motorun desteklediği tüm koşullar (Conditions), aksiyonlar (Actions), hedef seçiciler (TargetResolvers) ve özel mekanikler detaylarıyla listelenmiştir.

---

## 1. Temel Mantık ve Dinamik Olumsuzlama (Negation)

Questliner Engine, çoğu durumda **dinamik olumsuzlama** (`!`) sistemini destekler. 
Örneğin, `<Condition Type="HasGold" Amount="1000" />` oyuncunun 1000 altını olup olmadığını kontrol ederken; `<Condition Type="!HasGold" Amount="1000" />` oyuncunun **1000 altından az parası olmasını** şart koşar.
Hedef filtrelerinde `IsFemale` (Kadın) filtresinin başına `!` koyarsanız `!IsFemale` (Kadın olmayan / Erkek) anlamına gelir.

---

## 2. Conditions (Koşullar)

Koşullar `<Prerequisites>` (Görevin başlama şartı), Aşama (Stage) ilerlemeleri, özel diyalog görünürlükleri (`<CustomDialog>`) ve menü butonları için kullanılır.

| Type | Açıklama | Örnek Kullanım |
| :--- | :--- | :--- |
| **HasItem** | Oyuncunun envanterinde belirtilen eşyadan istenen miktarda var mı? (`ObjectId`: item string id) | `<Condition Type="HasItem" ObjectId="iron" Amount="5" />` |
| **ItemCountInInventory** | `HasItem` ile tamamen aynı işlevi görür. | `<Condition Type="ItemCountInInventory" ObjectId="grain" Amount="10" />` |
| **StageTimeout** | Mevcut aşamada belirtilen *gün* sayısı kadar zaman geçti mi? | `<Condition Type="StageTimeout" Amount="3" />` |
| **HasTroopCount** / **PartySize** | Oyuncunun partisinde en az belirtilen sayıda asker var mı? | `<Condition Type="PartySize" Amount="40" />` |
| **HasGold** | Oyuncunun en az belirtilen miktarda altını var mı? | `<Condition Type="HasGold" Amount="5000" />` |
| **TalkedTo** | Belirtilen Hedef (Alias) ile "About the task..." diyaloğu üzerinden konuşuldu mu? | `<Condition Type="TalkedTo" TargetAlias="TavernKeeper" />` |
| **HasPledgedAllegiance** | Oyuncu, belirtilen hedefin krallığına/fraksiyonuna katıldı mı? | `<Condition Type="HasPledgedAllegiance" TargetAlias="QuestLord" />` |
| **TargetIsDead** | Belirtilen hedef öldü mü? (Veya görev sırasında biz mi öldürdük?) | `<Condition Type="TargetIsDead" TargetAlias="EnemyLord" />` |
| **TargetIsAlive** | Belirtilen hedef hayatta mı? | `<Condition Type="TargetIsAlive" TargetAlias="QuestGiver" />` |
| **TargetIsPrisoner** | Belirtilen hedef şu an esir mi? | `<Condition Type="TargetIsPrisoner" TargetAlias="Hostage" />` |
| **TargetRelation** | Oyuncunun belirtilen hedefle olan ilişkisi belirtilen miktara eşit veya büyük mü? | `<Condition Type="TargetRelation" TargetAlias="Merchant" Amount="10" />` |
| **PlayerHasFief** | Oyuncunun klanının en az belirtilen sayıda tımarı (fief) var mı? | `<Condition Type="PlayerHasFief" Amount="1" />` |
| **ClanTier** / **PlayerClanTier** | Oyuncunun klan seviyesi en az belirtilen miktarda mı? | `<Condition Type="ClanTier" Amount="2" />` |
| **CheckFlag** | Belirtilen Quest-İçi değişken (Flag) Doğru(1) veya Yanlış(0) mı? | `<Condition Type="CheckFlag" ObjectId="FoundClue" Amount="1" />` |
| **IsStageActive** | Görev şu anda belirtilen aşamada mı? | `<Condition Type="IsStageActive" ObjectId="Stage2_Pusu" />` |
| **IsQuestActive** | Belirtilen ID'li görev şu an devam ediyor mu? (1: Evet, 0: Hayır) | `<Condition Type="IsQuestActive" ObjectId="QLF_DemirAhit_Cirak" Amount="1" />` |
| **IsQuestCompleted** | Belirtilen ID'li görev başarıyla tamamlanmış mı? | `<Condition Type="IsQuestCompleted" ObjectId="QLF_DemirAhit_Cirak" Amount="1" />` |
| **IsVassalOfTargetFaction** | Oyuncu, belirtilen hedefin krallığında *paralı asker olmadan* direkt Vassal mı? | `<Condition Type="IsVassalOfTargetFaction" TargetAlias="King" Amount="1" />` |
| **IsPlayerInSettlement** | Oyuncunun haritada rastgele dolaşmak yerine herhangi bir yerleşkenin (Şehir, Kale, Köy) içinde olup olmadığını kontrol eder. | `<Condition Type="!IsPlayerInSettlement" />` |
| **DefeatedParties** | Belirtilen kültürden/fraksiyondan kaç parti mağlup edildi? (`TargetAlias` kültür ID'si olur) | `<Condition Type="DefeatedParties" TargetAlias="looters" Amount="3" />` |
| **BattleResolvedAs** | SpawnBanditParty vb. ile yaratılan hedefin savaşı nasıl sonuçlandı? (Victory, Defeat, Retreat, DestroyedByAI) | `<Condition Type="BattleResolvedAs" TargetAlias="KiralikKatiller" StringValue="Victory" />` |
| **HasPrisoner** | Oyuncunun esirleri arasında belirtilen karakterden istenen sayıda var mı? | `<Condition Type="HasPrisoner" ObjectId="looter" Amount="5" />` |
| **GlobalFlag** | Kampanya boyunca kalıcı olarak kaydedilen değişkene bakar (1: True, 0: False). | `<Condition Type="GlobalFlag" ObjectId="DemirAhit1_Basarili" Amount="1" />` |
| **SkillCheck** | Oyuncunun belirtilen yeteneği (Skill) en az belirtilen miktarda mı? | `<Condition Type="SkillCheck" ObjectId="Roguery" Amount="50" />` |
| **Environment** | Zaman (Gece/Gündüz) ve Mevsim kontrolünü tek koşulda birleştirir. (`Time`: "Night" veya "Day", `Season`: "Winter", "Spring", "Summer", "Autumn") | `<Condition Type="Environment" Time="Night" Season="Winter" />` |
| **IsNight** / **IsDay** | Zamanın gece veya gündüz olduğunu denetler. | `<Condition Type="IsNight" />` |
| **IsSeason** | Mevsimi denetler. (`Season` veya `StringValue`: "Winter", "Spring", "Summer", "Autumn") | `<Condition Type="IsSeason" StringValue="Winter" />` |
| **IsAtWarWith** | Oyuncunun hedefle (veya belirtilen fraksiyonla) savaşta olup olmadığını denetler. | `<Condition Type="IsAtWarWith" TargetAlias="DüşmanLord" />` |

---

## 3. Actions (Aksiyonlar / Eylemler)

Aksiyonlar, diyalogların sonunda (`Consequences`), aşamalar tamamlandığında veya menü seçimlerinde tetiklenir.

| Type | Açıklama | Örnek Kullanım |
| :--- | :--- | :--- |
| **GiveItem** | Oyuncuya belirtilen eşyadan verir. (`ObjectId`: item string id) | `<Action Type="GiveItem" ObjectId="iron" Amount="1" />` |
| **TakeItem** | Oyuncudan belirtilen eşyayı siler. | `<Action Type="TakeItem" ObjectId="iron" Amount="1" />` |
| **AddGold** | Oyuncuya altın verir. | `<Action Type="AddGold" Amount="1000" />` |
| **TakeGold** | Oyuncudan altın siler. | `<Action Type="TakeGold" Amount="500" />` |
| **LoseGoldPercentage** | Oyuncunun mevcut altınının % X'ini siler. | `<Action Type="LoseGoldPercentage" Amount="10" />` |
| **RemovePrisoner** | Belirtilen mahkumu oyuncunun esirlerinden siler. | `<Action Type="RemovePrisoner" ObjectId="looter" Amount="5" />` |
| **ChangeRelation** | Oyuncunun belirtilen hedefle olan ilişkisini artırır/azaltır. | `<Action Type="ChangeRelation" TargetAlias="QuestGiver" Amount="15" />` |
| **ChangeStage** | Görevin aşamasını değiştirir. `ObjectId` gidilecek aşamanın ID'sidir. | `<Action Type="ChangeStage" ObjectId="Stage2_Pusu" />` |
| **FailQuest** | Görevi başarısız olarak sonlandırır. | `<Action Type="FailQuest" />` |
| **CompleteQuest** | Görevi başarılı olarak sonlandırır. | `<Action Type="CompleteQuest" />` |
| **ChangeRenown** | Oyuncunun klanına Renown (Ün) ekler/siler. | `<Action Type="ChangeRenown" Amount="50" />` |
| **ChangeInfluence** | Oyuncunun klanına Influence (Nüfuz) ekler/siler. | `<Action Type="ChangeInfluence" Amount="20" />` |
| **ChangeCrimeRating** | Belirtilen hedef fraksiyonunda ve/veya oyuncunun o an bulunduğu yerleşkede suç puanını artırır/azaltır. | `<Action Type="ChangeCrimeRating" TargetAlias="HedefSehir" Amount="30" />` |
| **DeclareWar** | Oyuncunun fraksiyonu ile hedefin fraksiyonu arasında savaş başlatır. | `<Action Type="DeclareWar" TargetAlias="EnemyLord" />` |
| **MakePeace** | Oyuncunun fraksiyonu ile hedefin fraksiyonu arasında barış ilan eder. | `<Action Type="MakePeace" TargetAlias="EnemyLord" />` |
| **SetPrisoner** | Belirtilen hedefi direkt oyuncunun esiri yapar. | `<Action Type="SetPrisoner" TargetAlias="QuestTarget" />` |
| **DamageHero** | Hedefin canını azaltır (suikast senaryoları için). | `<Action Type="DamageHero" TargetAlias="Victim" Amount="50" />` |
| **KillHero** | Belirtilen hedefi oyun dünyasında öldürür (Cinayet/Murder). Bannerlord, ölen Notable'ların yerine otomatik yenisini üretir. | `<Action Type="KillHero" TargetAlias="HainZanaatkar" />` |
| **MakeFugitive** | Belirtilen hedefi oyun haritasından siler ve ansiklopedide "Kayıp/Sürgün" (Lost) statüsüne geçirir. Öldürmekten daha yumuşak bir yok etme yöntemidir. | `<Action Type="MakeFugitive" TargetAlias="KacanTuccar" />` |
| **SetFlag** | Sadece bu göreve özel bir Boolean (True/False) değişken ayarlar. | `<Action Type="SetFlag" ObjectId="FoundClue" Amount="1" />` |
| **SetGlobalFlag** | Kampanya boyunca kalıcı kalacak (Save/Load destekli) bir GlobalFlag ayarlar. | `<Action Type="SetGlobalFlag" ObjectId="DemirAhit_Basarili" Amount="1" />` |
| **SaveHeroToGlobal** | Görev içindeki bir NPC'yi diğer görevlerde de çağırabilmek için kalıcı hafızaya "StringValue" adıyla kaydeder. | `<Action Type="SaveHeroToGlobal" TargetAlias="EfsaneviDemirci" StringValue="DemirAhit_AnaDemirci" />` |
| **SetVariable** | Görev metinlerinde kullanılmak üzere String veya Sayısal bir değişken kaydeder. | `<Action Type="SetVariable" TargetAlias="KatilinAdi" StringValue="Ahmet" />` |
| **IncrementVariable** | Görev-içi bir sayacı belirtilen miktar kadar artırır. | `<Action Type="IncrementVariable" TargetAlias="Oluler" Amount="1" />` |
| **SpawnBanditParty** | Haritada yeni bir Haydut/Çete partisi doğurur. (Bkz: Alt Başlık 3.1) | `<Action Type="SpawnBanditParty" TargetAlias="PusuPartisi" Amount="30" />` |
| **SetPartyAi** | Hedef partinin yapay zekasına emir verir (Saldır, Bekle, Koruma yap vb). | `<Action Type="SetPartyAi" TargetAlias="PusuPartisi" StringValue="EngageParty" ObjectId="Player" />` |
| **DestroyParty** | Belirtilen TargetAlias partisini (MobileParty) haritadan kalıcı olarak siler. Zaten silinmişse güvenli bir şekilde pas geçer. | `<Action Type="DestroyParty" TargetAlias="PusuPartisi" />` |
| **DestroyEncounteredParty** | O an oyuncunun savaş/diyalog menüsünde muhatap olduğu partiyi (EncounteredMobileParty) yok eder. Genellikle görev çapulcuları ile diyalog bittikten sonra onları barışçıl veya hikaye gereği haritadan silmek için kullanılır. | `<Action Type="DestroyEncounteredParty" />` |
| **StartQuest** | Belirtilen XML görevini otomatik olarak başlatır. (`ObjectId`: Görev ID'si) | `<Action Type="StartQuest" ObjectId="QLF_DemirAhit_Kalfa" />` |
| **MakeMet** | Oyuncunun hedefle daha önce tanışmış (HasMet = true) sayılmasını sağlar. Native oyunun "Tanışma" diyaloglarının araya girmesini engellemek için ForceConversation'dan hemen önce kullanılır. | `<Action Type="MakeMet" TargetAlias="Tüccar" />` |
| **ForceConversation** | Oyun ekranını keserek doğrudan hedefle bir diyalog penceresi açar. Genellikle "OnSettlementEnter" tetikleyicisiyle oyuncuyu aniden karşılamak için kullanılır. Öncesinde MakeMet kullanmanız önerilir. | `<Action Type="ForceConversation" TargetAlias="Tüccar" />` |

### 3.1. SpawnBanditParty ve SetPartyAi Detayları
`SpawnBanditParty` oyuncuya düşman partiler üretmek için harikadır.
- `TargetAlias`: Üretilen partinin Alias'ı. Daha sonra bu partiye emir vermek için kullanılır.
- `Amount`: Partideki asker sayısı.
- `PartyTemplateId`: Doğacak partinin şablonu (Örn: `looters_template`, `sea_raiders_template`).
- `ObjectId`: (Opsiyonel) Partinin hangi Settlement veya diğer Party etrafında doğacağı. Boş bırakılırsa oyuncunun etrafında doğar.
- `StringValue`: (Opsiyonel) Haritada görünecek özel parti ismi.

`SetPartyAi` ile bu partiye ne yapacağını söyleyebilirsiniz:
- `StringValue="EngageParty"` ve `ObjectId="Player"`: Doğrudan oyuncuya saldırması emrini verir.
- `StringValue="Hold"`: Olduğu yerde beklemesini sağlar.

---

## 4. TargetResolvers (Hedef Çözümleyiciler) ve Filtreler

Görev başladığında kiminle, nerede, hangi partiyle muhatap olacağımızı belirleyen motordur. Motor `Filters="A, B, C"` şeklindeki filtreleri okur ve uyan birini Alias'a atar.
Başına `!` konursa tam tersini seçer (Örn: `!IsFemale` -> Sadece Erkekleri seçer).
Özel filtre: `Trait(X)` (Örn: `Trait(Cruel)` -> Mercy puanı eksi olan birini seçer).

**Desteklenen Filtreler:**
- **Kimlik ve Durum:** `ClanLeader`, `KingdomLeader`, `Artisan`, `RuralNotable`, `Merchant`, `Headman`, `GangLeader`, `IsLord`, `IsHero`, `IsWanderer`, `IsNotable`, `IsPrisoner` (Herhangi bir esir), `IsPrisonerInSettlement` (Sadece şehir/kale zindanındaki esirler).
- **Cinsiyet:** `IsFemale`, `IsMale`.
- **Yerleşke Tipleri:** `Town` (veya `IsTown`), `Village` (veya `IsVillage`), `Castle` (veya `IsCastle`), `Hideout` (Haydut sığınağı).
- **Mesafe, Seçiciler (Selectors) ve Rastgelelik:** 
  - `Selector="Random"`: Adaylar arasından rastgele birini seçer.
  - `Selector="Nearest"`: Oyuncuya en yakın adayı seçer.
  - `Selector="NearestTo(Alias)"`: Belirtilen Alias'a (bir şehir, lord veya parti) haritada en yakın adayı seçer (Örn: `NearestTo(HedefSehir)`).
  - `Selector="WithinDistance(Alias, Mesafe)"`: Belirtilen Alias'a verilen harita mesafesi içindeki adaylar arasından seçim yapar (Örn: `WithinDistance(HedefSehir, 50)`).
  - `Selector="SecondNearest"`: En yakın 2. adayı seçer.
  - `Selector="Furthest"`: En uzak adayı seçer.
  - `Selector="HighestRelation"`: İlişkisi en yüksek olanı seçer.
- **Siyasi ve Sosyal Bağlar:** `PlayerFaction`, `RandomFaction`, `RandomKingdom`, `SameCultureAs(Alias)`.
  - `SameKingdomAs(Alias)`: Hedef ile tamamen aynı krallıkta olan kişileri bulur.
  - `SameClanAs(Alias)`: Hedef ile tamamen aynı ailede/klanda olan kişileri bulur.
  - `SameFactionAs(Alias)`: Bannerlord motorunda "Faction" (taraf) duruma göre değişir. Hedef bağımsız bir krallık kurmamışsa onun Klanını taraf sayar, krallığa bağlıysa Krallığını taraf sayar. Bu yüzden karışıklığı önlemek için mümkün mertebe `SameKingdomAs` veya `SameClanAs` kullanılması önerilir.
- **Lokasyon:** `SameSettlementAs(Alias)`, `RequiredSettlementAlias` (Alias'ın belirtilen mekanda olma zorunluluğu).
- **Özel Hedefleme:** `Target(Alias)` (Aynı görev içindeki spesifik bir alias'ı seçmek için), `GlobalHero(Key)` (Başka bir görevde kalıcı hafızaya kaydedilmiş karakteri geri çağırmak için).
- **Karakter Özellikleri (Traits):** `Trait(Cruel)` (Mercy<0), `Trait(Devious)` (Honor<0), `Trait(Calculating)`, `Trait(Impulsive)`, `Trait(Mercy)`, `Trait(Honor)`, `Trait(Generosity)`, `Trait(Closefisted)`, `Trait(Valor)`, `Trait(Cautious)`.

*(Not: Aynı hedefin birden fazla göreve atanmasını engellemek için sistem "Uniqueness" kontrolü yapar. Eğer bir NPC zaten bir görevde kullanılıyorsa, mümkün mertebe onu listeye almaz.)*

**Önemli TargetResolver Parametreleri:**
- `IsOptional="true"`: Normalde bir TargetResolver uyan kimseyi bulamazsa (null dönerse) görev hiç başlatılmaz. Eğer bu parametreyi kullanırsanız hedef bulunamasa bile görev başarıyla başlar (değer null olarak kalır). Diyaloglarda veya aksiyonlarda bu opsiyonel hedefe atıfta bulunursanız motor bunu güvenle (oyunu çökertmeden) atlar. (Örn: Krallığı olmayan oyuncu için `IsOptional="true"` atanmış Kral hedefi).

---

## 5. Triggers (Tetikleyiciler)

Görevlerin oyun dünyasında nasıl başlayacağını belirleyen ana mekanizmalardır. `<Quest>` etiketinin altında `<Triggers>` bloğunda tanımlanırlar.

| EventType | Açıklama |
| :--- | :--- |
| **OnConversation** | Görev, belirlenen `TargetAlias` hedefine gidip manuel olarak "Bir iş var mı?" dendiğinde başlar. Normal diyalog sistemiyle entegredir. |
| **AutoStart** | Görev, hiçbir NPC ile konuşmaya gerek kalmadan, `<Prerequisites>` koşulları (örneğin ClanTier=2) sağlandığında saatlik oyun döngüsünde arka planda gizlice başlar. Genelde bir olayın habercisi olarak veya gün sayacı başlatmak için kullanılır. |
| **OnSettlementEnter** | Oyuncu, belirtilen `TargetAlias` (Settlement) yerleşkesinden içeri adım attığı an tetiklenir. Koşullar sağlanıyorsa görev anında başlar. Görevin ilk aşamasında (`<OnEnterActions>`) bir `<Action Type="ForceConversation" TargetAlias="Tüccar" />` kullanılarak oyuncunun yüzüne anında diyalog fırlatılması sağlanabilir! |

---

## 6. UI ve Dinamik Takip Sistemi (Tracked Objects)

- **Aşama Logları (`LogText`):** Aşamada yazan LogText ekranda görünür. Eğer text içerisinde süslü parantez ile `{HedefAlias_Link}` yazarsanız, oyun otomatik olarak bu ismi mavi yapar ve haritada/ansiklopedide o hedefi **Quest Marker (Mavi Ünlem)** ile işaretler! Manuel bir tracking koduna gerek yoktur.

---

## 7. Özel Diyaloglar (Custom Dialogs) ve UI Özellikleri

Görevlerin normal "Teslim etme" diyalogları dışında, tamamen özel konuşma ağaçları oluşturmak için `<CustomDialogs>` kullanılır.

### ShowWhenDisabled ve DisabledHint
Eğer oyuncunun yetersiz bir koşulu (örneğin yeterli altını veya skilli olmaması) durumunda diyalog seçeneğinin *tamamen gizlenmesi* yerine **gri renkte (tıklanamaz)** olarak ekranda görünmesini istiyorsanız şu özellikleri kullanın:
- `ShowWhenDisabled="true"`: Bu ayar açıkken, altındaki `<Condition>`'lardan biri bile başarısız olsa diyalog gizlenmez, Bannerlord UI'ında gri (disabled) olarak belirir.
- `DisabledHint="{=hint_key}Metin"`: Oyuncu fareyle grileştirilmiş diyaloğun üzerine geldiğinde çıkacak olan ipucu metnidir.

**Örnek Kullanım:**
```xml
<DialogNode Type="Player" Id="Rusvet_Ver" InputToken="ql_q1_start" OutputToken="ql_q1_rusvet" ShowWhenDisabled="true" DisabledHint="{=hint1}500 Altın Gerekiyor">
  <Text>{=text1}İşte sana 500 altın, bu işi sessizce halledelim.</Text>
  <Conditions>
    <Condition Type="HasGold" Amount="500" />
  </Conditions>
</DialogNode>
```

---
`Bu belge Questliner Framework C# Motor Geliştiricisi tarafından hazırlanmıştır ve her zaman en güncel sürümü temsil eder.`
