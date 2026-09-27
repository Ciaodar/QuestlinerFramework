# Kapsamlı Senaryo ve Teknik Tasarım Raporu: Gölgelerin Yükselişi (Shadows' Ascendancy)

Bu belge, Questliner Framework üzerinde çalışacak, oyuncunun Klan Seviyesine (Clan Tier) göre açılan ve ardışık olarak ilerleyen iki görevlik (Gölgelerin Fısıltısı ve Karaborsa Savaşları) senaryo yayını detaylandırmaktadır.

---

## 1. Görev: Gölgelerin Fısıltısı (QLF_GolgelerinFisiltisi)
**Başlangıç Şartları (Conditions):** 
- `<Condition Type="ClanTier" Operator="GreaterOrEqual" Value="2" />`
- Oyuncu herhangi bir büyük şehre girdiğinde rastgele tetiklenir.
**Süre Sınırı:** 45 Gün
**Konsept:** Oyuncu, ünü yeni yayılmaya başlayan (Tier 2) bir klan lideri olarak, yerel bir şehirdeki esrarengiz kervan kayıplarını araştırır ve yeraltı dünyasıyla ilk temasını kurar.

### Hedef Çözümlemeleri (Target Resolvers)
1. **GorevVeren (Hero):** Görevin tetiklendiği şehirdeki bir Tüccar (Merchant). (CurrentSettlement -> Notable).
2. **KacakciLideri (Hero):** Aynı şehirdeki bir Çete Lideri (GangLeader). (Nearest to GorevVeren).
3. **HedefKervan (MobileParty):** "Kaçakçı Kervanı" adıyla spawn edilecek özel bir parti. (Sistem tarafından sonradan yaratılacak).
4. **GizliSığınak (Settlement):** Şehre en yakın Haydut Sığınağı (Hideout).

### Aşama 1: Tüccarın Derdi (Stage1_TuccarDiyalog)
**Durum:** Şehre girildiğinde Tüccar oyuncuyu yanına çağırır. 
**Hedef:** Tüccar ile konuş.
**Özel Diyaloglar:**
- **Tüccar:** "Adın buralarda duyulmaya başladı dostum. Şehrin muhafızları kör ve sağır; kervanlarım şehre varmadan hemen önce buharlaşıyor. Çapulcu işi değil, içeriden birileri onlara haber uçuruyor. Bunu çözersen sana yüklü ödeme yaparım."
- **Oyuncu Cevap:** "Kervanlarının izini süreceğim. Eğer içeriden biri varsa, onu da bulurum."
**Tetiklenen Olay:** Diyalog bittiğinde, şehre yakın bir noktada "HedefKervan" spawn edilir. `<Action Type="SpawnBanditParty" TargetAlias="HedefKervan" Amount="30" />` ve partiye şehirden uzaklaşma emri verilir. Aşama `Stage2_Takip` olarak güncellenir.

### Aşama 2: Kaçakçıların İzi (Stage2_Takip)
**Durum:** Oyuncu haritada kaçan kervanı yakalamalıdır.
**Hedef:** Kaçakçı Kervanını (HedefKervan) yen.
**İzlenen Değişkenler:** {HedefKervan_State}
**Tetiklenen Olay:** Parti yenildiğinde, oyuncunun envanterine şifreli bir defter eklenir (`GiveItem`). Menüden çıkıldığında görev `Stage3_Kanit` aşamasına geçer.

### Aşama 3: Şifreli Kayıtlar ve Yüzleşme (Stage3_Kanit) (Dallanma Başlangıcı)
**Durum:** Kervandan çıkan defterde, malların Çete Lideri'ne (`KacakciLideri`) aktarıldığı yazmaktadır. Oyuncunun şehre dönüp Çete Lideri ile yüzleşmesi gerekir.
**Hedef:** Çete Lideri ile konuş.
**Özel Diyaloglar:**
- **Oyuncu:** "Kervanları vuran senin adamlarınmış. Defterim elimde. Bunu lordlara verirsem kellenden olursun."
- **Çete Lideri:** "Lordlar mı? O malların yarısı zaten bu şehrin asilzadelerine gidiyor aptal! Bana katıl, defteri yak, o tüccarı da ortadan kaldıralım. Kazancı kırışırız."
- **Oyuncu Cevap 1 (Kanun Yolu):** "Adaletten kaçamazsın. Kılıcını çek!" (Görev `Stage4_Kavga` aşamasına geçer).
- **Oyuncu Cevap 2 (Suç Yolu):** "Teklifin ilgimi çekti. Tüccarı halledeceğim." (Görev `Stage4_Ihanet` aşamasına geçer, Tüccar ile ilişki -20 düşer).

### Aşama 4A: Kanun Yolu (Stage4_PusuBekleyis ve Stage5_PusuSavas)
**Durum:** Oyuncu mafyaya boyun eğmeyi ve rüşveti reddeder. Çete lideri şehirden çıkışın tehlikeli olacağını fısıldayarak gizliden oyuncuyu tehdit eder.
**Tetiklenen Olay:** Şehir içindeki diyalog güvenle kapanır (`Stage4_PusuBekleyis`). Oyuncu yerleşkeden çıkıp açık haritaya adım attığında (`<Condition Type="!IsPlayerInSettlement" />`), 50 kişilik çete grubu aniden belirir (`SpawnBanditParty`) ve oyuncuya saldırır (`SetPartyAi EngageParty`).
**Özel Diyalog:** Çete mensupları saldırmadan önce oyuncuya çete lideriyle zıtlaşmanın bedelini ödeteceklerini söyler.
**Sonuç:** Pusu partisi yenildiğinde görev başarıyla tamamlanır.
- **Ödüller:** +5000 Denar, Tüccar ile +15 ilişki, Çete Lideri ile -30 ilişki.
- **Global Flag:** `<Action Type="SetGlobalFlag" ObjectId="GolgelerinFisiltisi_KanunAdami" Amount="1" />` ve `<Action Type="SetGlobalFlag" ObjectId="GolgelerinFisiltisi_Bitti" Amount="1" />`

### Aşama 4B: Suç Yolu - Soylu Şantajı (Stage4_TuccarSantaj)
**Durum:** Oyuncu bir lord olarak sokak ortasında suikast gibi ayak işleri yapmayı reddeder; ancak açgözlülüğüne yenik düşerek çete lideriyle anlaşır. Çete liderine tüccarı haraca bağlama sözü verir.
**Hedef:** Şehirdeki Tüccar ile konuşarak onu hem çeteye hem de oyuncuya pay vermeye zorla.
**Özel Diyalog:** Tüccar köşeye sıkışır, dehşet içinde oyuncunun mafyadan bile acımasız olduğunu söyler ve canını kurtarmak için ödemeyi kabul eder.
**Sonuç:** Tüccar sindirildiğinde görev tamamlanır.
- **Ödüller:** +10.000 Denar, Tüccar ile -50 ilişki, Çete Lideri ile +20 ilişki.
- **Global Flag:** `<Action Type="SetGlobalFlag" ObjectId="GolgelerinFisiltisi_KaranlikOrtak" Amount="1" />` ve `<Action Type="SetGlobalFlag" ObjectId="GolgelerinFisiltisi_Bitti" Amount="1" />`

---

## 2. Görev: Karaborsa Savaşları (QLF_KaraborsaSavaslari)
**Başlangıç Şartları (Conditions):** 
- `<Condition Type="ClanTier" Amount="3" />`
- `<Condition Type="GlobalFlag" Key="GolgelerinFisiltisi_KanunAdami" Operator="==" Value="true" />` VEYA Karanlık Ortak Flag'i.
**Tetikleyici:** Oyuncu Tier 3'e ulaştığında ve haritada hareket halindeyken (HourlyTick) bir ulağın gelmesiyle `<Action Type="StartQuest" ... />` şeklinde başlar.
**Konsept:** İlk görevdeki seçimler (Global Flag'ler) şimdi oyuncunun karşısına büyük bir politik kriz olarak çıkar. Oyuncu artık krallık siyasetine girecek kadar güçlenmiştir (Tier 3).

### Hedef Çözümlemeleri (Target Resolvers)
1. **YozlasmisLord (Hero):** İlk görevin geçtiği şehrin sahibi olan veya o krallıkta bulunan Lord. *(Geliştirici Notu: Artık motorumuz Trait filtrelemeyi destekliyor. `Filters="Lord, Trait(Cruel)"` veya `Filters="Lord, Trait(Devious)"` kullanarak zalim veya sinsi lordları otomatik hedefleyebilirsiniz!)*
2. **KiralikKatiller (MobileParty):** Sadece oyuncuyu avlamak için spawn olacak paralı asker grubu.

### Aşama 1: Geçmişin Hayaletleri (Stage1_Suikast)
**Durum:** Oyuncunun yanına bir ulak gelir. (Ulağın metni, ilk görevdeki seçime göre dinamik değişir).
- **Condition (KanunAdami == True):** Ulak: "Lord {YozlasmisLord.Name}, çeteyi çökertip gelirlerine mani olduğunuz için başınıza ödül koydu! Dikkatli olun!"
- **Condition (KaranlikOrtak == True):** Ulak: "Lord {YozlasmisLord.Name}, çeteyle yaptığınız anlaşmada kendi payını alamadığı için sizi ortadan kaldırmak istiyor!"
**Tetiklenen Olay:** `<Action Type="SpawnBanditParty" TargetAlias="KiralikKatiller" Amount="60" />` ve `SetPartyAi` ile `EngageParty` emri verilir. Kiralık katiller oyuncuya doğru hızla ilerler.
**Hedef:** Kiralık Katiller partisinden sağ kurtul.

### Aşama 2: Sessizliği Bozmak (Stage2_Sorgu)
**Durum:** Kiralık katiller yenildikten sonra oyuncu, katillerin komutanını sorguya çeker.
**Özel Diyalog:**
- **Komutan:** "Canımı bağışla! Emri {YozlasmisLord.Name} verdi. Karaborsanın asıl patronu odur. Kanıt istersen, mühürlü emri zırhımın içindedir."
**Sonuç:** Komutan serbest bırakılır. Oyuncu envanterine `Lordun_Muhurlu_Emri` eklenir. Hedef `Stage3_Intikam` olarak güncellenir.

### Aşama 3: Lord ile Yüzleşme (Stage3_Intikam) (Dallanma B)
**Durum:** Yozlaşmış Lord ile konuşma.
**Hedef:** {YozlasmisLord} karakterini bul ve konuş.
**Özel Diyaloglar:**
- **Oyuncu:** "Gönderdiğin köpekleri kestim. Mühürlü emrin elimde. Bunu krallığın geri kalanına gösterirsem onurun yerle bir olur!"
- **Yozlaşmış Lord:** "(Güler) Ben bir asilzadeyim, sen ise yeni palazlanmış bir paralı askersin. Kral bana mı inanacak sana mı? Yine de baş ağrısı istemiyorum. Ne istiyorsun?"
- **Oyuncu Cevap 1 (Şantaj):** "Bana 20.000 Denar öde, bu konu kapansın." 
  - *Sonuç:* +20.000 Denar, Honor (Onur) trait'i düşer. Görev biter.
- **Oyuncu Cevap 2 (Meydan Okuma):** "Seni keseceğim!" *(Geliştirici Notu: ForceBattle veya doğrudan savaş açmak için `DeclareWar` Action'ı kullanılabilir).*
  - *Sonuç:* Anında savaş başlar. Lord öldürülürse/yenilirse krallıkla ilişki devasa oranda eksiye düşer ama oyuncu çok büyük bir nam (Renown) kazanır. Görev biter.
- **Oyuncu Cevap 3 (Politik Hamle):** "Bu belgeyi doğrudan hükümdara götüreceğim."
  - *Sonuç:* Görev `Stage4_Hukumdar` aşamasına geçer. Lord ile ilişki -50 olur.

### Aşama 4: Hükümdarın Adaleti (Stage4_Hukumdar) (Opsiyonel Son)
**Hedef:** Krallığın Lideri (Faction Ruler) ile konuş.
**Durum:** Oyuncu, yozlaşmış lordun ihanetini kanıtlamak zorundadır.
**Özel Diyalog:**
- **Hükümdar:** "Bir asilzadeyi suçlamak büyük bir cürettir. Kanıtın nerede?"
**Tetiklenen Olay:** `Lordun_Muhurlu_Emri` eşyası envanterden silinir. 
**Sonuç:** Hükümdar, oyuncunun sadakatini takdir eder.
- **Ödüller:** Devasa İlişki artışı (Hükümdar ile +30), +30 Nüfuz (Influence), Yozlaşmış Lord'un krallıktan sürülmesi veya mülklerinden birine el konulması. 

---

### Quest Motoru Geliştiricisine Notlar:
1. **Zaman Akışı (Persistence):** Bu senaryoda ilk görevdeki eylemler (Örn: Çeteye yardım etmek veya yok etmek) `Global Flags` ile saklanarak, oyuncu aylar sonra (Tier 3'e ulaştığında) ikinci göreve başladığında oyunun hafızasından çağırılmaktadır.
2. **Karakter Traitleri:** Yozlaşmış lordu seçerken `Trait(Devious)` veya `Trait(Cruel)` filtresi kullanarak sistemin en güvenilmez/zalim karakteri bulması sağlanmıştır.
3. **Rol Yapma Derinliği:** 2. Görevin 3. Aşamasında oyuncuya; ekonomik (Şantaj), şiddet (Meydan Okuma) ve politik (Hükümdara Gitme) olmak üzere 3 farklı oyun stili sunulmuştur.