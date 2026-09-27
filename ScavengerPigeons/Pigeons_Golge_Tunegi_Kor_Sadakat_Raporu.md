# Çapulcu Güvercinler: Gölge Tüneği & Kör Sadakat Raporu (The Shadow Roost & Blind Loyalty)

## 0. Genel Bakış ve Felsefi Konsept

**Çapulcu Güvercinler (Scavenger Pigeons)**, Calradia kıtasında asırlardır lordların, tüccarların ve kralların kirli işlerini yürüten, dışarıdan sıradan çapulcular gibi görünen ancak derinlerde kıtayı saran devasa bir gizli istihbarat, şantaj ve suikast şebekesidir.

Örgütün iki farklı yüzü vardır:
1. **Dış Çember (Anavatana Hizmet Yolu):** Krallık çıkarlarını ve dostluk bağlarını gözeten, kuralcı istihbarat kolu (Görev 4'te Dost'a inanıp Krallığa katılan ve Görev 5 *Kopuk Bağlar* ile firari zanaatkarı yakalayan yol).
2. **İç Çember / Gölge Tüneği (Kör Sadakat Yolu):** Hiçbir kanunu, kralı veya dostluğu tanımayan; "Çapulcunun emri güvercinin görevidir" dogmasına körü körüne biat eden, kanla yıkanmış suikastçılar ocağı.

Bu rapor; Görev 4'te (*Anavatanın İhaneti*) Zanaatkar'ın sahte ve şüpheli oyununu sezdiği anda kanuna veya krallığa başvurmadan, **Zanaatkar'ı kendi atölyesinde soğukkanlılıkla infaz eden** oyuncunun sürüklendiği **Kör Sadakat / Gölge Tüneği** alternatif ana hikaye kolunu (Görev 5, 6, 7 ve 3 farklı radikal sonu) tüm teknik detayları, XML şemaları, diyalogları ve görev logları ile ortaya koymaktadır.

---

## 1. Görev 4 Kırılma Noktası: Zanaatkar'ın İnfazı (The Divergence)

### A. Senaryo Anı ve Kırılma
`QLF_AnavataninIhaneti` görevinde oyuncu, Zanaatkar `{ArtisanTarget}`'a şifreli deriyi ulaştırdığında Zanaatkar oyuncudan `{Dost}`'u öldürmesini ister. 

- **Aydınlık / Kanun Yolu:** Oyuncu şüphelenir, Dost ile görüşür, Köy Büyüğü'ne döner, Zanaatkar'ı ihbar eder ve Krallığa katılır.
- **Kör Sadakat / Gölge Tüneği Yolu:** Oyuncu, Zanaatkar'ın sözlerindeki zehri ve örgüt içindeki çürümeyi sezer. Ancak krala şikayet etmek yerine örgütün kadim kanununu uygular: *"Yuvayı kirleten kuşun kanadı kırılmaz, boynu vurulur!"* Oyuncu Zanaatkar'ı oracıkta hançerler. 
- Zanaatkar'ın cansız bedenini aradığında cebinden sıradan bir deri değil, üzerinde kadim bir simge kazınmış siyah bir kemik mühür çıkar: **Kuzgun Kemik Şifresi (Raven Bone Cipher)**.
- Bu infaz, örgütün en derin kademesindeki **Gölge Tüneği (Shadow Roost)** efendilerinin dikkatini çeker. Oyuncuya açık bir tehdit değil, kanlı bir davet gelir.

```xml
<!-- Görev 4 Alternatif Kapanış Tetikleyicisi -->
<Action Type="KillHero" TargetAlias="ArtisanTarget" />
<Action Type="SetGlobalFlag" ObjectId="SP_Zanaatkar_Katledildi" Amount="1" />
<Action Type="GiveItem" ObjectId="raven_bone_cipher" Amount="1" />
<Action Type="SaveHeroToGlobal" TargetAlias="Dost" StringValue="SP_Dost" />
<Action Type="StartQuest" ObjectId="QLF_GolgeTunegi_Cagri" />
```

---

## 2. Görev 5: Gölge Tüneği'nin Çağrısı (QLF_GolgeTunegi_Cagri)

### A. Görev Özeti ve Konsept
- **Görev ID:** `QLF_GolgeTunegi_Cagri`
- **Başlık:** `{=sp_gt1_title}Call of the Shadow Roost` / `{=sp_gt1_title_tr}Gölge Tüneği'nin Çağrısı`
- **Süre Sınırı:** 45 Gün
- **Önkoşul:** `GlobalFlag(SP_Zanaatkar_Katledildi) == 1`
- **Konsept:** Zanaatkar'ın ölümünün ardından şehir muhafızları teyakkuza geçer. Oyuncu 1 gün boyunca gizlenir (Tahkikat / Soğuma evresi). Ardından gölgelerden bir "Kör Ulak" belirir ve oyuncuyu Calradia'nın ıssız dağlarındaki gizli bir sığınağa davet eder. Burada örgütün gölge liderlerinden biri olan "Kör Usta" (Blind Master) ile ilk temas kurulur.

### B. Hedef Çözümleyicileri (TargetResolvers)
```xml
<TargetResolvers>
  <Resolver Alias="Dost" TargetType="Hero" Filters="GlobalHero(SP_Dost)" />
  <Resolver Alias="GolgeUlagi" TargetType="Hero" Filters="Artisan, SameCultureAsPlayer" Selector="Nearest" />
  <Resolver Alias="GolgeSiginagi" TargetType="Settlement" Filters="Hideout" Selector="Nearest" />
  <Resolver Alias="RakipAjanlar" TargetType="MobileParty" Filters="BanditParty" Selector="Random" />
</TargetResolvers>
```

### C. Aşamalar (Stages) ve LogText'ler

#### Stage 0: Kan Lekeli Sessizlik (Stage0_Soguma)
- **Log (TR):** *"{ArtisanTarget_Link}'ı atölyesinde cansız bıraktın. Şehir muhafızları katili ararken bir süre kuytuda kalıp izini kaybettirmelisin. Sokakların sakinleşmesini bekle... (1 Gün)"*
- **Log (EN):** *"You left {ArtisanTarget_Link} lifeless in his workshop. With city guards scouring the streets, you must lay low and blend into the shadows. Wait for the heat to die down... (1 Day)"*
- **Süre:** `TimeoutInDays="1"`
- **OnTimeoutActions:** `<Action Type="ChangeStage" ObjectId="Stage1_UlakBulusma" />`

#### Stage 1: Ulağın Fısıltısı (Stage1_UlakBulusma)
- **Log (TR):** *"Sokaklar durulduğunda pelerininin altına siyah tüylü bir güvercin kondu. Zanaatkar'ın cesedinden aldığın kemik mührü bilen biri var. {GolgeSiginagi_Link} yakınlarındaki gizli temas noktasına git ve Gölge Ulağı ile konuş."*
- **Log (EN):** *"As the alert faded, a black-feathered pigeon landed under your cloak. Someone recognized the bone seal you claimed from the artisan's corpse. Travel near {GolgeSiginagi_Link} and speak with the Shadow Courier."*
- **OnEnterActions:** `<Action Type="MakeMet" TargetAlias="GolgeUlagi" />`

#### Stage 2: Sadakat İmtihanı (Stage2_AjanBaskini)
- **Log (TR):** *"Ulak, Zanaatkar'ın sıradan bir hain değil, örgütün sırlarını Calradia krallıklarına sızdıran bir köstebek olduğunu doğruladı. Ancak senin kör sadakatini kanıtlaman gerek: Zanaatkar'ın kaçırdığı şifreleri satın almak için bekleyen 50 kişilik yabancı muhafız müfrezesini {GolgeSiginagi_Link} eteklerinde pusuya düşür ve yok et."*
- **Log (EN):** *"The courier confirmed the artisan was leaking the Pigeon archives to rival kingdoms. Prove your blind loyalty: Ambush and eliminate the 50-man foreign guard detachment awaiting the ciphers near {GolgeSiginagi_Link}."*
- **Koşul:** `<Condition Type="DefeatedParties" TargetAlias="RakipAjanlar" Amount="1" />`

#### Stage 3: Gölge Tüneği'ne Kabul (StageSuccess)
- **Log (TR):** *"Yabancı casusları kılıçtan geçirdin. Gölge Tüneği senin acımasızlığını ve sorgusuz sadakatini onayladı. Artık örgütün dış kapısındaki bir çapulcu değil, gölgelerin içindeki bir cellatsın. Sana 'Gölge Kemik Hançeri' ve 30.000 altın sunuldu."*
- **Log (EN):** *"You butchered the foreign spies. The Shadow Roost approved your ruthlessness and blind devotion. You are no longer a mere pawn; you are now an executioner of the shadows. You received the 'Shadow Bone Dagger' and 30,000 denars."*
- **Ödüller:** `GiveItem ObjectId="shadow_bone_dagger" Amount="1"`, `AddGold Amount="30000"`, `ChangeRenown Amount="40"`.

---

## 3. Görev 6: Kanlı Şifreler ve Büyük Tasfiye (QLF_GolgeTunegi_Tasfiye)

### A. Görev Özeti ve Konsept
- **Görev ID:** `QLF_GolgeTunegi_Tasfiye`
- **Başlık:** `{=sp_gt2_title}Blood Ciphers & The Great Purge` / `{=sp_gt2_title_tr}Kanlı Şifreler ve Büyük Tasfiye`
- **Süre Sınırı:** 60 Gün
- **Önkoşul:** `IsQuestCompleted ObjectId="QLF_GolgeTunegi_Cagri" Amount="1"`
- **Konsept:** Zanaatkar'ın ölümü örgüt içindeki ılımlı/muhalif kanadı dehşete düşürmüştür. Eski dostun sayılan `{SP_Dost}`, senin bir ölüm makinesine dönüştüğünü anlar ve krallık garnizonlarından topladığı birliklerle peşine düşer. Gölge Tüneği ise sana tek bir emir verir: *"Sırrı bilen hiç kimse nefes alamaz. Dostunu ve şantajcı muhbirleri temizle!"*

### B. Hedef Çözümleyicileri (TargetResolvers)
```xml
<TargetResolvers>
  <Resolver Alias="Dost" TargetType="Hero" Filters="GlobalHero(SP_Dost)" />
  <Resolver Alias="Muhbir1" TargetType="Hero" Filters="Merchant, SameKingdomAs(Dost)" Selector="Nearest" />
  <Resolver Alias="Muhbir2" TargetType="Hero" Filters="GangLeader, SameKingdomAs(Dost)" Selector="Random" />
  <Resolver Alias="DostOrdusu" TargetType="MobileParty" Filters="LordParty" Selector="NearestTo(Dost)" />
</TargetResolvers>
```

### C. Aşamalar ve Karakter Diyalogları

#### 1. Aşama: İlk Muhbirin Susturulması (Stage1_Muhbir1)
- **Diyalog (Muhbir Tüccar):**
  > **Muhbir:** *"Sen... Zanaatkar'ı öldüren o uğursuz gölgesin! Benden ne istiyorsun? Bütün defterleri yaktım, yemin ederim!"*
  > 
  > **Oyuncu Seçenek A (Soğukkanlı İnfaz):** *"Kör Sadakat af tanımaz. Gölge Tüneği selamını iletti."* *(Tüccar infaz edilir, suç izi bırakılmaz).*
  > 
  > **Oyuncu Seçenek B (Altınlarını Alıp Öldür):** *"Önce kasanın anahtarlarını ver, sonra ecelinle tanış."* *(+15.000 Altın, Roguery +10).*

#### 2. Aşama: Yeraltı Muhbirinin Tasfiyesi (Stage2_Muhbir2)
- Çete Lideri arka sokaklarda 30 muhafızıyla pusu kurar.
- Oyuncu çeteyi darmadağın eder ve ikinci şifre parçasını ele geçirir.

#### 3. Aşama: Eski Dost ile Kanlı Hesaplaşma (Stage3_DostYuzlesme)
- **Konsept:** `{SP_Dost}` 70 kişilik seçkin şövalye müfrezesiyle oyuncunun karşısına dikilir.
- **Özel Diyalog (Dost):**
  > **{SP_Dost}:** *"{PLAYER.NAME}... Sana baktığımda eski bir müttefik değil, kana susamış bir tarikat canavarı görüyorum! Zanaatkar bir haindi ama mahkemede yargılanacaktı. Sen onu bir köpek gibi boğazladın ve o iğrenç Gölge Tüneği'ne kul oldun! Seni bu topraklardan sileceğim!"*
  > 
  > **Oyuncu:** *"Sen zayıfsın Dost. Mahkemeleriniz, krallarınız ve yalanlarınız sadece zayıfları korur. Güvercinler göklerde değil, mezarların üzerinde uçar. Kılıcını çek!"*
- **Savaş Sonucu:** Oyuncu galip geldiğinde Dost esir alınır veya idam edilir (`KillHero TargetAlias="Dost"`).
- **Ödüller:** `StageSuccess` -> +50.000 Altın, +50 Renown, `SetGlobalFlag ObjectId="SP_Dost_Ortadan_Kalkti" Amount="1"`.

---

## 4. Görev 7: Calradia'nın Kuklacısı / Tahtların Altındaki Kan (QLF_GolgeTunegi_NihaiSon)

### A. Görev Özeti ve Konsept
- **Görev ID:** `QLF_GolgeTunegi_NihaiSon`
- **Başlık:** `{=sp_gt3_title}Puppeteer of Calradia` / `{=sp_gt3_title_tr}Calradia'nın Kuklacısı`
- **Süre Sınırı:** 90 Gün
- **Önkoşul:** `IsQuestCompleted ObjectId="QLF_GolgeTunegi_Tasfiye" Amount="1"`
- **Konsept:** Calradia çapındaki tüm engeller kalkmıştır. Artık oyuncu, doğrudan örgütün kurucusu ve beyni olan **Kör Usta (The Blind Master)** ile Gölge Tüneği'nin kalbinde yüzleşir. Kör Usta, kıtadaki tüm krallıkların hükümdarlarını birbirine kırdıracak sahte fermanları ve suikast şebekesini tamamlamıştır. Ancak zirvede yalnızca bir kuşa yer vardır!

### B. Hedef Çözümleyicileri (TargetResolvers)
```xml
<TargetResolvers>
  <Resolver Alias="KorUsta" TargetType="Hero" Filters="Artisan, SameCultureAsPlayer" Selector="First" />
  <Resolver Alias="HedefKral" TargetType="Hero" Filters="KingdomLeader, !PlayerClan" Selector="Nearest" />
  <Resolver Alias="KraliyetKonvScope" TargetType="Settlement" Filters="Town" Selector="Furthest" />
</TargetResolvers>
```

### C. Üç Radikal Final Seçeneği (The Three Final Paths)

```
                            [GÖLGE TÜNEĞİ'NİN ZİRVESİ]
                                      │
            ┌─────────────────────────┼─────────────────────────┐
            ▼                         ▼                         ▼
    [SON 1: GÖLGE BAŞEFENDİ]    [SON 2: KIRIK KUKLA]    [SON 3: KÜLLERİN İMPARATORLUĞU]
   Kör Usta'yı zehirle & tahtı    Kültü yak & yalnız      3 Krallığa sahte ferman saç;
   ele geçir. Calradia'nın        cellat olarak Calradia  topyekün kıta savaşı başlat;
   görünmez hükümdarı ol.         dağlarında kaybol.      küllerin zenginliğini topla.
```

---

### Detaylı Son Senaryoları ve Diyaloglar

#### SON 1: Gölge Başefendi (The Shadow Sovereign)
- **Seçim Mantığı:** Oyuncu, Kör Usta'nın yaşlandığını ve kendisinin örgütü çok daha kusursuz yöneteceğini söyler.
- **Diyalog:**
  > **Kör Usta:** *"Bunca kan, bunca sadakat... Şimdi bana mı kılıç çekiyorsun yavru güvercin?"*
  > 
  > **Oyuncu:** *"Bana öğrettiğin ilk kuralı unuttun Usta: 'En keskin gaga, en yaşlı tüneği parçalar.' Artık emirleri sen değil, ben vereceğim."*
- **Mekanik & Aksiyon:**
  - `<Action Type="KillHero" TargetAlias="KorUsta" />`
  - `<Action Type="GiveItem" ObjectId="shadow_master_ring" Amount="1" />`
  - `<Action Type="AddGold" Amount="100000" />`
  - `<Action Type="ChangeRenown" Amount="100" />`
  - `<Action Type="ChangeStage" ObjectId="StageSon_GolgeBasEfendi" />`
- **Bitiş Günlüğü (Log):**
  > *"Kör Usta'nın nefesi senin ellerinde tükendi. Kuzgun Mührü artık senin parmağında. Calradia kralları tahtlarında gururla otururken, her gece yastıklarının altına bıraktığın siyah tüylerle kimin gerçek efendi olduğunu bilecekler. Sen Gölge Tüneği'nin mutlak hükümdarısın."*

---

#### SON 2: İhanetin Kanlı Döngüsü / Kırık Kukla (The Broken Marionette)
- **Seçim Mantığı:** Oyuncu, Zanaatkar'ın, Dost'un ve yüzlerce masumun sadece bu bunak ihtiyarın paranoyası için öldüğünü fark eder. Kör sadakatin bir kölelik olduğunu anlar.
- **Diyalog:**
  > **Oyuncu:** *"Sen bir deha değilsin. Sadece karanlık deliklerde yaşayan bir lağım faresisin! Ne Anavatan var ne de onur. Sadece kan ve yalanlar. Bu tünek bu gece yanacak!"*
  > 
  > **Kör Usta:** *"Aptal çocuk! Bizi yakarsan seni kim koruyacak? Bütün Calradia peşine düşecek!"*
  > 
  > **Oyuncu:** *"Bırak gelsinler. Çapulcuların efendisi öldü, şimdi avcı başlıyor."*
- **Mekanik & Aksiyon:**
  - Tünel Muhafızları ile açık savaş (40 kişilik elit fanatik grubu).
  - `<Action Type="KillHero" TargetAlias="KorUsta" />`
  - `<Action Type="ChangeCrimeRating" Amount="60" />`
  - `<Action Type="GiveItem" ObjectId="pigeon_blood_claymore" Amount="1" />`
  - `<Action Type="ChangeStage" ObjectId="StageSon_KirikKukla" />`
- **Bitiş Günlüğü (Log):**
  > *"Gölge Tüneği alevler içinde çökerken arkana bile bakmadın. Şifreler küle döndü, efendiler can verdi. Artık hiçbir tarikata, hiçbir krala boyun eğmeyen; Calradia bozkırlarında adı sadece fısıltıyla anılan yalnız bir hayaletsin."*

---

#### SON 3: Calradia'yı Ateşe Veren Kaos (Harbinger of Ruin)
- **Seçim Mantığı:** Oyuncu ne efendi olmak ister ne de pişmanlık duyar. Kaosun kendisini bir silah olarak kullanır. Zanaatkar'ın çaldığı şifreleri ve sahte suikast fermanlarını kıtanın en büyük 3 krallığının saraylarına aynı gece fırlatır.
- **Diyalog:**
  > **Oyuncu:** *"Dünya düzen istemiyor Usta. Dünya yangın istiyor! Bütün krallara diğerinin kendisini öldürmek istediğini gösteren sahte mektupları gönderdim. Şafakta borazanlar çalacak."*
  > 
  > **Kör Usta:** *(Kahkahalar atar)* *"Korkunçsun... Calradia bin yıldır böyle bir delilik görmedi. Al bu altınları ve yangını seyret!"*
- **Mekanik & Aksiyon:**
  - `<Action Type="DeclareWar" TargetAlias="HedefKral" ObjectId="Player" />`
  - `<Action Type="AddGold" Amount="250000" />`
  - `<Action Type="ChangeRenown" Amount="150" />`
  - `<Action Type="ChangeStage" ObjectId="StageSon_KitalarSavasi" />`
- **Bitiş Günlüğü (Log):**
  > *"Aynı gece üç kraliyet sarayında da sahte güvercinler öttü. Sabah olduğunda imparatorluklar birbirine savaş ilan etti. Şehirler kuşatıldı, köyler yakıldı. Sen ise servetin ve ordunla bu devasa yangının ortasında tahtları dilediğin gibi deviriyorsun."*

---

## 5. Teknik Mimari ve Motor Uyumluluğu

1. **`MakeMet` Güvencesi:**
   - Görev 5, 6 ve 7'deki tüm NPC temaslarında (`GolgeUlagi`, `Muhbir1`, `Muhbir2`, `KorUsta`), diyalog açılmadan önce aşamanın `OnEnterActions` bloğunda `<Action Type="MakeMet" TargetAlias="..." />` zorunlu tutulmuştur. Bu sayede Bannerlord Native tanışma penceresi görev akışını asla kesemez.

2. **`GlobalHero` ve `SaveHeroToGlobal` Tutarlılığı:**
   - Görev 4'te hafızaya alınan `{SP_Dost}` ve Zanaatkar'ın ölüm bayrağı (`SP_Zanaatkar_Katledildi`), görevler arası sıfırlanma riski olmadan `GlobalHero` referansıyla korunur.

3. **Parti Hız ve Şablon Standartları:**
   - Firari ve suikastçı partilerinde `looters_template` yerine v1.0.0.109 motor sürümümüzün tam desteklediği `deserters_template` kullanılmıştır.

4. **Çift Dilli Yerelleştirme (EN/TR):**
   - Tüm yeni görev stringleri, butr/native standartlarına uygun olarak `std_ScavengerPigeons_tr.xml` ve İngilizce ana tanımlarla %100 örtüşmektedir.
