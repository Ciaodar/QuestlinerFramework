# Questliner Framework — Changelog

Tüm önemli güncellemeler, yeni eklenen özellikler, hata düzeltmeleri ve sistem değişiklikleri bu dosyada kronolojik olarak kayıt altına alınacaktır. Yapay zeka ajanları veya geliştiriciler, yeni bir "Build" (Geliştirme Paketi) tamamladıklarında bu dosyaya tarih ve sürüm düşerek notlarını eklemelidir.

## [v1.3.0] - 28 Eylül 2026 (Battle Tracking, Dialogue Isolation & MobileParty Engine Polish)

### Düzeltildi & İyileştirildi (Fixed & Improved)
- **Diyalog İzolasyonu & Duplicate Berber Hatası Giderildi:**
  - `_currentConversationQuestId` oturum bazlı takip sistemi eklendi. `ql_` ile başlayan dâhili diyalog token'larının diğer aktif veya tamamlanmış görevlerin diyaloglarıyla çakışması ve berberde çift seçenek çıkması engellendi.
  - Tamamlanmış (`_completedQuestIds`) görevlerin diyaloglarının haritada tekrar tetiklenmesi tamamen engellendi.
- **Savaş Takibi & Hedef Parti İlerlemesi (`DefeatedParties`):**
  - `OnMapEventEnded` içerisinde oyuncu savaşı kazandığında `quest.TargetParties` taranarak `Defeated_{alias}_Parties` sayaçları otomatik artırılır hale getirildi. Artık `<Condition Type="DefeatedParties" TargetAlias="HedefKervan" Amount="1" />` harita savaşlarında sorunsuz çalışmaktadır.
- **Konsol / Aşama Atlama (`ForceCompleteCurrentStage` & `ForceSuccess`):**
  - Aşama zorla geçildiğinde veya tamamlandığında atlanan `OnExitActions` listesi (eşya verme, bayrak atama vs.) artık eksiksiz çalıştırılır.
- **Mobil Parti Harita Takibi & Günlük Değişkenleri:**
  - `TargetParties` içindeki hedefler artık `{Alias}`, `{Alias.Name}` ve `{Alias_Link}` olarak log ve diyaloglarda çözümlenir.
  - Haritada `AddTrackedObject` ile görev işareti (mavi ünlem) partilerin üzerine otomatik olarak yerleştirilir.
- **`SpawnBanditParty` & `SetPartyAi` Geliştirmeleri:**
  - Parti spawn edildiğinde `action.StringValue` "Hold", "PatrolAroundSettlement", "GoToSettlement" değerlerini destekler; haritada oyuncuyu körlemesine kovalamak yerine durması veya devriye gezmesi sağlanabilir.
  - Şablon (`PartyTemplateId`) belirtildiğinde birlik tipi şablondan otomatik seçilir (sadece çapulcu basma zorunluluğu kalktı).
  - `SetPartyAi` eylemi `ObjectId="Player"` hedefini destekleyecek şekilde güncellendi.
- **Bozuk Bildirim Temizliği:**
  - UTF-8 bozulması içeren ve Native bildirimlerle çakışan `MBInformationManager.AddQuickInformation` çağrıları temizlendi.

---

## [v1.2.0] - 27 Eylül 2026 (Environment, Spatial Selectors & Political Engine)

### Eklendi (Added)
- **Yeni Action'lar (Aksiyonlar):**
  - `ChangeCrimeRating`: Belirtilen hedef fraksiyonunda ve/veya oyuncunun o an içinde bulunduğu yerleşkede suç puanını artırır/azaltır. Yeraltı dünyası ve haydutluk senaryoları için eklenmiştir.
  - `DeclareWar` & `MakePeace` genişletildi: Artık sadece oyuncu ile hedef arasında değil, `TargetAlias` ve `ObjectId` kullanılarak hedef fraksiyonlar arasında da savaş ve barış başlatılabilir.
- **Yeni Condition'lar (Koşullar):**
  - `Environment`: Zaman (Gece/Gündüz) ve Mevsim (Winter/Spring/Summer/Autumn) kontrollerini tek bir koşulda birleştiren esnek çevre kontrolü (`Time="Night" Season="Winter"`).
  - `IsNight` / `IsDay`: Zamanın gece veya gündüz olduğunu sorgulayan pratik koşullar.
  - `IsSeason`: Mevcut mevsimi sorgulayan koşul (`Season="Winter"` veya `StringValue="Winter"`).
  - `IsAtWarWith`: Oyuncunun belirtilen hedefle (veya `ObjectId` ile verilen fraksiyonla) savaşta olup olmadığını sorgulayan diplomasi koşulu.
- **Yeni TargetResolver Seçicileri (Selectors) & Filtreleri:**
  - `Selector="NearestTo(Alias)"`: Belirtilen Alias'a (bir şehir, lord veya parti) coğrafi olarak en yakın olan adayı seçer.
  - `Selector="WithinDistance(Alias, Mesafe)"`: Belirtilen Alias'a verilen harita mesafesi içindeki adaylar arasından seçim yapar (örn: `WithinDistance(HedefSehir, 50)`).
  - `Filters="Hideout"`: Haydut sığınaklarını Settlement hedefi olarak çözümler.
  - `DynamicQuest.GetTargetParty(Alias)` metodu motor mimarisine dahil edildi.

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
