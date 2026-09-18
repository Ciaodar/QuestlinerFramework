# Questliner Framework - XML Yazar Ajanı Rehberi (Quest Creator AI Guide)

Bu doküman, Questliner Framework üzerinde Mount & Blade II: Bannerlord için görev (Quest) yazacak olan Yapay Zeka (AI) ajanları için hazırlanmıştır.

**KRİTİK UYARI:** Sen bir **XML YAZARI** ajansın. Görevin `ModuleData/Questlines/` klasörü altına `.xml` dosyaları oluşturmak, hikayeler yazmak ve görevleri birbirine bağlamaktır. **ASLA VE ASLA C# KODLARINA (Questliner.Framework içindeki .cs dosyalarına) MÜDAHALE ETMEMELİSİN.** Eğer XML altyapısında eksik bir Condition, Action veya özellik fark edersen, bunu kullanıcıya veya Framework'ü geliştiren diğer AI ajana bir "Feature Request" (Özellik İsteği) olarak bildir.

---

## 1. Temel Görev Yapısı (QuestModel)
Her XML dosyası `<Questline>` etiketi ile başlar.

```xml
<Questline Id="QLF_GorevID" Title="Görev Adı" TimeLimitInDays="30">
  <TargetResolvers>...</TargetResolvers>
  <Prerequisites>...</Prerequisites>
  <Triggers>...</Triggers>
  <Stages>...</Stages>
  <CustomDialogs>...</CustomDialogs>
  <GameMenuOptions>...</GameMenuOptions>
</Questline>
```

---

## 2. Target Resolvers (Hedef Çözümleyiciler)
Görevlerin hedef kişi ve yerleşimlerini dinamik seçmek için kullanılır.
- **TargetType:** `Hero` veya `Settlement` veya `MobileParty`.
- **Selector:** `Random`, `Nearest`, `Furthest`.
- **Filters:** Hedefin taşıması gereken özellikler. Virgülle ayrılır.
- **Örnek Filtreler:** `KingdomLeader`, `ClanLeader`, `Artisan`, `RuralNotable`, `Merchant`, `Headman`, `GangLeader`, `Female`, `Male`, `SameCultureAsPlayer`, `SameFactionAs(Alias)`, `Target(Alias)`
- **Negatif Filtreler (!):** Herhangi bir filtrenin başına `!` koyarak tersini alabilirsin. Örn: `!PlayerClan`, `!Target(Dost)`
- **RequiredSettlementAlias:** Karakterin bulunması gereken şehri zorunlu tutar.

Örnek:
```xml
<Resolver Alias="GorevVeren" TargetType="Hero" Filters="SameCultureAsPlayer, Artisan" Selector="Nearest" />
```

---

## 3. Triggers (Tetikleyiciler)
Görevin nasıl başlayacağını belirler. Eğer yazılmazsa veya `OnConversation` yazılırsa görev, NPC ile "Bana iş var mı?" diyaloğundan alınır.
```xml
<!-- Görevi konuşmadan, oyun içinde direkt başlatır -->
<Trigger EventType="AutoStart" />
<!-- Veya menüden başlatmak için bir GameMenuOption içine StartQuest eylemi koyabilirsin -->
```

---

## 4. Conditions (Koşullar)
Ön şartlar, Aşama ilerlemeleri ve Diyalog girişleri için kullanılır.
- **Type:** `HasGold`, `HasTroopCount`, `TalkedTo`, `CheckFlag`, `IsDead`, `DefeatedParties`, `IsStageActive`.
- Negasyon: `!` kullanarak şartı tersine çevirebilirsin. Örn: `!HasGold` (Altını yoksa).

---

## 5. Eylemler (Actions)
Diyalog sonuçları (Consequences) veya Aşama geçişlerinde (OnEnter/OnExit/OnTimeout) kullanılır.

- `ChangeStage` - `ObjectId`="StageId"
- `ChangeOtherQuestStage` - `ObjectId`="DigerGorevID", `TargetAlias`="StageId"
- `StartQuest` - `ObjectId`="DigerGorevID"
- `AddGold` - `Amount`="1000"
- `TakeGold` - `Amount`="500"
- `ChangeRelation` - `TargetAlias`="HeroAlias", `Amount`="10"
- `LeaveEncounter` - Menüyü kapatıp haritaya döner.
- `DestroyEncounteredParty` - Konuştuğun partiyi yok eder. (Spawn olan haydutları silmek için ideal).
- `SetFlag` - `StringValue`="BayrakIsmi" (Boolean bayrak açar).

### Parti Spawn ve AI Yönetimi
- **SpawnBanditParty:**
  ```xml
  <Action Type="SpawnBanditParty" TargetAlias="HedefKisim" Amount="10" StringValue="{=!}Haydutlar" PartyTemplateId="sea_raiders_template" SpawnDistance="3" SpawnRadius="5" />
  ```
  *(Not: `PartyTemplateId` verilmezse looters doğar. `sea_raiders_template`, `steppe_bandits_template` kullanılabilir.)*

- **SetPartyAi:**
  ```xml
  <Action Type="SetPartyAi" TargetAlias="SpawnEdilenPartiAlias" StringValue="IgnoreAi" />
  <!-- Diğer StringValue'lar: UnignoreAi, Hold, EngageParty, EscortParty, GoToSettlement, PatrolAroundSettlement -->
  <!-- Eğer EngageParty, Escort vs. kullanılacaksa ObjectId="..." ile hedef Hero/Yerleşim alias'ı verilmelidir. -->
  ```

### Değişken Atamaları (Tracked Variables)
Görev loglarında ve diyaloglarda kullanılacak özel değişkenler yaratmak için.
- `SetVariable` - `TargetAlias`="DegiskenAdi", `StringValue`="Yeni Deger"
- `IncrementVariable` - `TargetAlias`="SayacAdi", `Amount`="1"

---

## 6. Dinamik Yazılar (Metin ve Log)
Diyaloglar (`<Text>`) ve Aşama logları (`<LogText>`) içinde `{Alias}` yapısı kullanılır.

- `{Alias.Name}` - Hedefin ismi (Örn: `{KoyBuyugu.Name}`)
- `{Alias_Link}` - Tıklanabilir Encyclopedia linki (Örn: `{UzakSehir_Link}`)
- Ayrıca `SetVariable` ile oluşturduğunuz tüm değişkenleri süslü parantez içinde doğrudan yazabilirsiniz (Örn: `{OzelDurumMesaji}`)
- Dialog text'lerinin başına `{=ozel_id}` şeklinde lokalizasyon etiketi eklemeyi unutmayın!

---

Kolay gelsin! Yeni görevleri, ana hikayeleri ve rastgele olayları yazarken Framework'ün bu eşsiz dinamizmini kullanmayı unutma!
