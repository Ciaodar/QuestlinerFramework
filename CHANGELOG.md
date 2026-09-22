# Questliner Framework — Changelog

Tüm önemli güncellemeler, yeni eklenen özellikler, hata düzeltmeleri ve sistem değişiklikleri bu dosyada kronolojik olarak kayıt altına alınacaktır. Yapay zeka ajanları veya geliştiriciler, yeni bir "Build" (Geliştirme Paketi) tamamladıklarında bu dosyaya tarih ve sürüm düşerek notlarını eklemelidir.

---

## [v1.1.0] - 18 Eylül 2026 (RPG Engine Upgrade)

### Eklendi (Added)
- **GlobalStateManager (Kalıcı Hafıza):** Kampanya boyunca silinmeyen, save/load dosyalarına otomatik işlenen `GlobalFlag` altyapısı C# motoruna dahil edildi. `QuestlinerCampaignBehavior.SyncData` üzerinden senkronizasyon sağlandı.
- **Yeni Action'lar (Aksiyonlar):**
  - `SetGlobalFlag`: Kalıcı hafızaya True/False değer atamak için motor komutu eklendi.
  - `ForceConversation`: Oyuncunun karşısına aniden (UI'ı keserek) bir diyalog penceresi fırlatma eylemi eklendi.
  - `KillHero`: Belirtilen karakteri senaryo gereği öldürür (Bannerlord Native yapısı üzerinden çalışır).
  - `MakeFugitive`: Belirtilen karakteri öldürmeden haritadan silerek "Kayıp/Sürgün" statüsüne alır.
  - `SaveHeroToGlobal`: Görev içindeki bir NPC'yi diğer görevlerde çağırabilmek için StringValue adıyla kalıcı hafızaya alır.
  - `DestroyParty`: `TargetAlias` ile belirlenen bir MobileParty'yi güvenli bir şekilde (eğer halen aktifse) haritadan siler. Başarısızlık durumunda (FailNode) kalan çeteleri temizlemek için idealdir.
  - `MakeMet`: Hedef NPC ile oyuncuyu daha önce tanışmış (HasMet = true) olarak işaretler. Pusu veya ani olay diyaloglarında Native motorun araya girip "Sen kimsin?" diye sormasını (tanışma faslını) bypass etmek için eklenmiştir.
- **Yeni Trigger (Tetikleyiciler):**
  - `OnSettlementEnter`: Oyuncu bir yerleşkeye (şehir/köy) girdiğinde, gereksinimler (Prerequisites) sağlanıyorsa arka planda görevi anında başlatan tetikleyici C# motoruna dahil edildi. `ForceConversation` aksiyonu ile birleştiğinde anlık pusu/karşılama diyalogları yaratılabilir.
- **Yeni UI / Diyalog Özellikleri:**
  - `ShowWhenDisabled="true"` ve `DisabledHint`: Oyuncu diyalog (DialogNode) seçeneklerinde koşul başarısız olduğunda seçeneğin tamamen gizlenmesi yerine, Bannerlord'un native UI'ı kullanılarak gri (tıklanamaz) hale getirilmesi ve fareyle üzerine gelindiğinde `DisabledHint` metninin (örn: "Yetersiz Altın") çıkması sağlandı.
- **Yeni Condition'lar (Koşullar):**
  - `IsPlayerInSettlement`: Oyuncunun şu anda herhangi bir yerleşkede (Şehir, Kale, Köy vb.) olup olmadığını kontrol eder. Olayların (pusu vb.) sadece açık haritada gerçekleşmesini garantiye almak için `!IsPlayerInSettlement` şeklinde olumsuzlanarak kullanıma uygundur.
  - `BattleResolvedAs`: `TargetAlias` ile belirlenen bir MobileParty'nin sonucunu kontrol eder. (Victory, Defeat, Retreat, DestroyedByAI). Auto-Resolve dahil tüm savaş sonuçlarını dinler.
  - `GlobalFlag`: Kalıcı hafızadaki durumu (True/False) kontrol eden koşul eklendi.
  - `ItemCountInInventory`: Envanterdeki belirli bir eşyanın miktarını kontrol eden koşul eklendi (`HasItem` ile eşdeğer çalışır).
  - `SkillCheck`: Ana karakterin (MainHero) özel yeteneklerinin (Charm, Roguery vs.) seviyesini zar atar gibi kontrol etmeye yarayan koşul eklendi.
- **Yeni TargetResolver Filtreleri ve Özellikleri:**
  - `IsOptional` (Attribute): Hedef bulucu uyan kimseyi bulamazsa görevin başlamasını engelleyen kısıtlama kaldırıldı. Hedefe XML üzerinden `IsOptional="true"` atanırsa, karakter/şehir bulunamasa bile görev başlatılır ve hedef güvenli bir şekilde `null` olarak pas geçilir.
  - `GlobalHero(X)` filtresi eklendi. Başka bir görevde `SaveHeroToGlobal` ile kaydedilmiş hedefi direkt olarak yeni göreve atar.
  - `IsPrisoner` ve `IsPrisonerInSettlement` filtreleri eklendi. `IsPrisonerInSettlement` sadece şehir/kale zindanındaki esirleri ayıklayarak `SameSettlementAs` çakışmalarını (MobileParty kilitlenmelerini) önler.
  - `SameClanAs(Alias)` filtresi eklendi. (SameKingdomAs ile arasındaki ayrım belirginleştirildi).
  - `Trait(X)` filtresi motorun Hedef Bulucu sistemine (TargetResolverService) eklendi.
  - Desteklenen spesifik çeviriler: `Cruel` (Mercy < 0), `Devious` (Honor < 0), `Calculating`, `Impulsive`, `Mercy`, `Honor`, `Generosity`, `Closefisted`, `Valor`, `Cautious`.
- **Takım Kuralları ve Prensipler:**
  - `AGENTS.md` içerisine "Team Roles & Strict Boundaries" bölümü eklendi. Yapay zeka ajanlarının (XML Yazarı vs C# Geliştiricisi) çapraz dosya müdahalesi engellendi.
  - Yeni senaryolar için Mod Klasör Yapıları (`DemirAhit/` ve `ShadowsAscendancy/`) oluşturuldu.

### Düzeltildi (Fixed)
- Hedef Benzersizliği (Uniqueness): Aynı NPC'nin aynı anda iki farklı DynamicQuest tarafından hedeflenmesini önlemek için, müsaitlik kontrolü `TargetResolverService.cs` içerisine eklendi. Sadece çaresiz kalındığında eski hedefler listeye düşecek.

### Önemli Geliştirici Notu
- Motor, "ForceBattle" sistemini atlamıştır. Şehir içi sokak kavgaları (AlleyFight) ve anında battle spawn'ları doğrudan oyunu çökertebileceği için, senaryo yazarlarından bunun yerine `SpawnBanditParty` ve ardından `SetPartyAi Target="Player" Mode="Engage"` komutlarını kullanarak haritada dinamik pusu yaratmaları istenmiştir.

---

*Şablon: Gelecek buildlerde üstteki formatı kopyalayıp güncel tarihi yazarak listeyi genişletin.*
