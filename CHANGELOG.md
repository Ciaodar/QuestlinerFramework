# Questliner Framework — Changelog

Tüm önemli güncellemeler, yeni eklenen özellikler, hata düzeltmeleri ve sistem değişiklikleri bu dosyada kronolojik olarak kayıt altına alınacaktır. Yapay zeka ajanları veya geliştiriciler, yeni bir "Build" (Geliştirme Paketi) tamamladıklarında bu dosyaya tarih ve sürüm düşerek notlarını eklemelidir.

## [v1.7.0] - 07 Ekim 2026 (Culture-Aware Dynamic Party Archetypes & Custom Inline Troop Rosters)

### Eklendi & Geliştirildi (Added & Improved)
- **Kültür Duyarlı Dinamik Parti Şablonları (`ActionExecutor.cs` - `SpawnBanditParty`):**
  - `PartyTemplateId` niteliği artık büyük/küçük harf duyarsız olarak ve `_template` sonekiyle veya soneksiz şekilde 5 yeni dinamik arketipi destekler:
    - **`deserters` / `deserters_template`**: Doğma konumuna en yakın şehir/kale/köyün kültürünü (`ResolveLocalCulture`) bularak o kültürün `BasicTroop` terfi ağacındaki (`UpgradeTargets`) **Tier 2 - Tier 4** düzenli piyade ve okçulardan kademe dengeli (`SpreadByTier`) gerçekçi bir firari (asker kaçağı) birliği oluşturur. Uzak bir hedef/sığınak (`ObjectId`/`SettlementAlias`) etrafında doğurulduğunda oyuncunun bulunduğu şehrin değil, doğrudan doğma noktasının yakınındaki yerleşkenin kültürü baz alınır.
    - **`mercenary` / `mercenaries` / `mercenary_template`**: Hanlarda bulunan profesyonel paralı askerlerden (`mercenary_swordsman`, `hired_blade`, `mercenary_crossbowman`, `hired_crossbow`, `mercenary_scout`, `veteran_borrowed_troop`) oluşan paralı asker müfrezesi kurar.
    - **`assassins` / `assassins_template` / `shadow_assassins_template`**: Kiralık kılıçlar (`hired_blade`, `mercenary_swordsman`, `hired_crossbow`) ve yerel kültürün çevik **Tier 3 - Tier 4** yaya savaşçılarından oluşan suikastçı timi kurar.
    - **`caravan_guards` / `caravan_guards_template` / `smuggler_caravan_template`**: Öncelikle yerel kültürün kervan muhafızları ve silahlı tüccarlarından (`localCulture.CaravanGuard` terfi ağacı, `caravan_guard_<culture>`, `veteran_caravan_guard_<culture>`, `armed_trader_<culture>`) oluşan, haritada kervan katırı/devesi görseliyle dolaşan kervan/kaçakçı birliği kurar.
    - **`garrison_guards` / `garrison_guards_template` / `elite_guards_template`**: Yerel kültürün **Tier 3 - Tier 5** ağır zırhlı düzenli garnizon piyade ve menzilli birliklerinden oluşan seçkin muhafız müfrezesi kurar.
- **Özel Satır-İçi Asker Dizilimi (`custom:troop1:count1,troop2:count2`):**
  - `PartyTemplateId` içerisinde `custom:` önekiyle veya doğrudan virgül/iki nokta üst üste formatıyla (`custom:imperial_legionary:20,imperial_palatine_guard:15` ya da adet belirtmeden `custom:vlandian_swordsman,vlandian_sharpshooter`) istenen askerlerin doğrudan `party.MemberRoster`'a eklenmesi sağlandı.
- **`SettlementAlias` ve `SetPartyAi` İyileştirmeleri (`QuestModel.cs` & `ActionExecutor.cs`):**
  - `ActionModel` içerisine `SettlementAlias` XML niteliği eklendi; hem `SpawnBanditParty` hem de `SetPartyAi` (`PatrolAroundSettlement`, `GoToSettlement`, `EngageParty`, `EscortParty`) aksiyonlarında `ObjectId` yerine `SettlementAlias` (veya doğrudan yerleşke `StringId`) yazıldığında hedef yerleşkenin sorunsuz çözümlenmesi sağlandı.
  - `SetPartyAi` içerisinde `StringValue="IgnoreAi"` verildiğinde partinin diğer partiler tarafından yok sayılmasının yanı sıra haritada olduğu yerde sabit beklemesi (`SetMoveModeHold`) garanti altına alındı.

---

## [v1.6.1] - 05 Ekim 2026 (Case-Insensitive Text Binding, Native CustomParty Spawning, Quest Loop Prevention & True Quest Item Protection)

### Düzeltildi & Geliştirildi (Fixed & Improved)
- **Metin Değişkenleri & Diyalog Harf Duyarlılığı (`DynamicQuest.cs`):**
  - TaleWorlds `StringHelpers.SetCharacterProperties` ve `SetSettlementProperties` çağrılarının `MBTextManager` nesnesini ezerek değişkenleri yalnızca büyük harfli (`NAME`, `LINK`, `GENDER`) bırakması sorunu çözüldü.
  - `{Dost.Name}`, `{Target.Name}`, `{Target_Link}`, `{Target_Name}` ve tüm küçük/büyük harf kombinasyonları (`Name`, `name`, `NAME`, `Link`, `link`, `LINK`) hem üst `TextObject` özelliklerine hem de düz metin değişkenlerine eksiksiz enjekte edildi.
  - Diyaloglarda ve günlüklerde `{Dost.Name}` veya `{ArtisanTarget_Link}` boş kalma/silinme sorunu tamamen giderildi.
- **Haydut Partisi Doğma ve Harita Takibi (`ActionExecutor.cs` - `SpawnBanditParty`):**
  - Önceki çift mesafe öteleme (double-offsetting: `NavigationHelper` + `InitializeMobilePartyAroundPosition`) hatası kaldırıldı; tek geçişli erişilebilir konum hesabı yapıldı.
  - TaleWorlds standart `CustomPartyComponent.CreateCustomPartyWithPartyTemplate` altyapısına geçildi, parti görseli güncellendi (`SetVisualAsDirty()`) ve parti görünürlüğü garantilendi.
  - Doğurulan görev partileri anında `quest.AddTrackedObject(party)` ile harita izleme çemberine (quest tracker) alındı; aşama geçişlerinde (`TransitionToStage`) koşul veya aksiyonla ilişkili tüm partilerin haritada işaretlenmesi otomatikleştirildi.
- **Sonsuz Görev Yeniden Başlama Döngüleri (`DynamicQuest.cs` & `QuestlinerCampaignBehavior.cs`):**
  - `QuestBase` sınıfının tüm yaşam döngüsü tamamlama metodları (`OnCompleteWithSuccess`, `OnFinalize`, `OnFailed`, `OnCanceled`, `OnTimedOut`, `OnBetrayal`) override edilerek `MarkQuestAsCompleted` ile bağlandı.
  - `QuestlinerCampaignBehavior` içerisine `CampaignEvents.OnQuestCompletedEvent` dinleyicisi eklenerek konsol komutları (`completestage`, `campaign.complete_quest`) veya Native zaman aşımları dahil herhangi bir yolla tamamlanan görevlerin `_completedQuestIds` listesine işlenmesi garantiye alındı.
  - `TryAutoStartQuest` metodunda görev oluşturulmadan önce önkoşul (`Prerequisites`) kontrolü yapılarak yetersiz klan seviyesinde (`ClanTier < 3`) erken başlama engellendi.
- **Tüccarlara 1 Dinar Satış Koruması (`ActionExecutor.cs` & `QuestlinerCampaignBehavior.cs`):**
  - `GiveItem` aksiyonunda görev eşyaları `new EquipmentElement(..., isQuestItem: true)` olarak eklendi.
  - `TakeItem` aksiyonunda envanterdeki quest item öğeleri indekslenerek güvenle düşüldü.
  - Yerleşkeye girişte ve saatlik tick'te oyuncunun envanteri taranarak `IsQuestItem` olan eşyalar Native ticaret engelli `EquipmentElement(..., isQuestItem: true)` yapısına otomatik dönüştürüldü.
  - `QuestlinerTradeItemPriceFactorModel.IsQuestItem` içerisindeki aşırı genel `ring` ve `ledger` filtreleri kaldırıldı, spesifik görev eşyalarıyla sınırlandırıldı.
- **Global Karakter Çözümleme Katılığı (`TargetResolverService.cs`):**
  - `GlobalHero(Key)` filtresi veya `SpecificId` hedefi bulunamadığında haritadan rastgele bir lord/notable seçip global hafızaya atayan hatalı fallback kaldırıldı. Bulunamayan global karakterler için `null` dönmesi sağlandı, senaryo hafızasının rastgele krallarla zehirlenmesi önlendi.
  - `SaveHeroToGlobal` aksiyonu çalıştırıldığı anda ilgili NPC `DynamicQuest.BindHeroToMBTextManager` ile hemen metin yöneticisine bağlandı.

---

## [v1.6.0] - 05 Ekim 2026 (Global Variable Engine, Bandit Party Spawning, Quest Restart Prevention & Non-Sellable Quest Items)

### Düzeltildi & Geliştirildi (Fixed & Improved)
- **Görev Eşyaları Satılamazlık Modeli (`QuestlinerTradeItemPriceFactorModel`):**
  - TaleWorlds Native ticaret modelindeki 1 Dinar alt sınır zorlaması (`MathF.Max(price, 1)`) geçersiz kılındı.
  - Değeri 0 olan, `NotMerchandise` işaretli veya hikaye görev eşyaları (`raven_bone_cipher`, `black_ledger`, `shadow_master_ring` vb.) tüccarlara satılırken 0 Dinar fiyatlandırılarak satılması engellendi.
- **`SpawnBanditParty` ve Şablon/Navmesh Desteği:**
  - `deserters_template`, `assassins_template` vb. şablonların Native MBObjectManager'da bulunamaması durumunda bölge kültürünün şablonlarına veya `looters_template`'e kademeli fallback eklendi.
  - `NavigationHelper.FindReachablePointAroundPosition` ile doğma konumu doğrulanarak su veya erişilemez dağ alanlarına takılma önlendi.
  - Oluşturulan parti `party.SetPartyUsedByQuest(true)` ile korumaya alındı, birlikler şablon yığınlarına orantılı dağıtıldı ve `quest.TargetParties[TargetAlias]` sözlüğüne doğrudan kaydedildi.
- **Metin Değişkenleri & Ansiklopedi Linkleri (`DynamicQuest.cs`):**
  - `{Alias}`, `{Alias.Name}`, `{Alias_Link}`, `{Alias_Name}` değişkenleri `StringHelpers.SetCharacterProperties` ve `SetSettlementProperties` ile eksiksiz bağlandı.
  - `GlobalStateManager.Instance.GlobalHeroes` içindeki tüm kalıcı karakterler aşama günlüklerine (`LogText`) ve diyaloglara otomatik enjekte edilir hale getirildi.
  - Oyuncu değişkenleri (`Player`, `PLAYER`, `Player.Name`, `PlayerName`, `PLAYER.NAME`, `Player_Link`) eksiksiz tanımlandı.
- **Sonsuz Görev Döngüsü & Otomatik Başlatma Güvenliği (`QuestlinerCampaignBehavior.cs`):**
  - Tamamlanmış (`_completedQuestIds`) görevlerin saatlik tick'te veya yerleşkeye girişte tekrar başlaması kalıcı olarak engellendi.
  - `questModel.StartType == "AutoStart"` ve `Triggers.EventType == "AutoStart"` şartları senkronize edildi.
  - `CheckQuestStartCondition` içerisine `_completedQuestIds` ve önkoşul (`Prerequisites`) kontrolleri eklendi.
- **Karşılaşma Sonlandırma (`LeaveEncounter`):**
  - `ActionExecutor` ve `OnConversationEnded` entegrasyonuyla `PlayerEncounter.LeaveEncounter` bayrağı işlendiğinde karşılaşma ve menü temizliği güvenle tamamlanır.
- **Global Karakter Çözümleme (`TargetResolverService.cs`):**
  - `SpecificId` veya `Filters` içinde `GlobalHero(Key)` kullanıldığında yerleşke kısıtları ve `usedHeroes` filtresi atlanarak doğrudan hedef global kahramanın döndürülmesi sağlandı.
- **Diyalog Performansı Temizliği:**
  - Her diyalog koşul kontrolünde diske senkron log yazan `File.AppendAllText("QuestlinerDebug.txt")` çağrıları temizlendi.

---

## [v1.5.0] - 28 Eylül 2026 (Effective Kingdom Resolving, Chained Quest Timing & War Fallback)

### Düzeltildi & Geliştirildi (Fixed & Improved)
- **Şehir ve Kasaba Eşrafı Krallık Çözümleyicisi (Effective Kingdom Resolving):**
  - `TargetResolverService.cs` içerisine `GetEffectiveKingdom` metodu eklendi. TaleWorlds Native yapısında soylu olmayan eşraf karakterlerinin (`Merchant`, `GangLeader`, `Artisan`, `RuralNotable`) klanı krallık taşımadığından `SameKingdomAs(Alias)` filtresinin bu NPC'leri eleyip 0 adaya düşürme ve görevi başlatamama sorunu giderildi. Artık eşrafın ikamet ettiği yerleşkenin bağlı olduğu krallık (`HomeSettlement.MapFaction as Kingdom`) okunarak filtre kusursuz çalışmaktadır.
- **Zincirleme Görev Tetikleme Zamanlaması (Success Node Quest Chaining):**
  - `DynamicQuest.cs` içinde `TransitionToStage` metodu güncellendi. Başarı aşamasındaki (`IsSuccessNode`) `OnEnterActions` çalıştırılmadan önce görev kimliği `_completedQuestIds` listesine işlenir. Böylece `OnEnterActions` içinde çağrılan `StartQuest` aksiyonu, önkoşulundaki `IsQuestCompleted` testini anında geçerek ardışık görevleri güvenle başlatır.
- **Kıtasal Savaş Garantisi (DeclareWar Robustness):**
  - `ActionExecutor.cs` içindeki `DeclareWar` aksiyonunda, `targetFaction` ile `sourceFaction` aynı krallığa denk geldiğinde (örneğin oyuncu hedef kralın kendi krallığını hedeflediğinde) savaşı iptal etmek yerine haritadaki diğer aktif bir krallık seçilerek kıtasal savaşın patlak vermesi garanti altına alındı.
- **Mobil Birlik Çözümleyici Yedek Desteği (`PartyOf` Fallback):**
  - Hedef lord esir düşmüşse veya haritada kişisel ordusu yoksa `PartyOf(Hero)` çözümleyicisinin başarısız olmasını önlemek için ilgili lordun klanına ait aktif birlikler güvenli yedek olarak tanındı.

---

## [v1.4.0] - 28 Eylül 2026 (Dialogue Priority, AI vs AI Warfare & MobileParty Resolving)

### Eklendi & İyileştirildi (Added & Improved)
- **Diyalog Öncelik Sistemi (Dialogue Priority):**
  - `CustomDialogModel` içine `Priority` (`[XmlAttribute("Priority")]`) niteliği eklendi.
  - `starter.AddPlayerLine` ve `starter.AddDialogLine` metodlarına XML'den gelen `dialogNode.Priority` değeri bağlandı. Artık XML'de tanımlanan `Priority="200"` değeri Native TaleWorlds diyaloglarının (varsayılan 100) önüne geçerek görev diyaloglarının öncelikle açılmasını garantiler.
- **Yapay Zeka Krallıkları Arası Savaş/Barış (AI vs AI Diplomacy):**
  - `DeclareWar` ve `MakePeace` aksiyonları geliştirildi. `TargetAlias` ve `ObjectId` aynı anda verildiğinde artık iki yapay zeka krallığı/fraksiyonu birbiriyle savaşa sokulabilir veya barıştırılabilir (`sourceFaction` ve `targetFaction`).
- **Mobil Parti Dinamik Hedef Çözümleyici (MobileParty Resolver):**
  - `ResolveMobileParty` metoduna `PartyOf(HeroAlias)` filtresi ve seçicisi eklendi; belirtilen bir lordun ordusu doğrudan parti hedefi olarak çözümlenebilir.
  - `NearestTo(Alias)` seçicisi MobileParty için de aktif edildi.
- **Suç Puanı Güvenliği (Crime Rating Fallback):**
  - `ChangeCrimeRating` aksiyonunda hedef fraksiyon belirtilmediğinde açık haritada veya sığınakta çökme/etkisizlik yaşanmaması için oyuncunun o anki yerleşkesi veya ana fraksiyonu güvenli yedek olarak atandı.

---

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
