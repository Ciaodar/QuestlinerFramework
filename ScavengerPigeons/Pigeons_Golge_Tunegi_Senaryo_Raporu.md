# Scavenger Pigeons (Çapulcu Güvercinler) — Kapsamlı Senaryo ve Teknik Tasarım Raporu: Gölge Tüneği & Çifte Rota Mimarisi

Bu belge, Questliner Framework üzerinde çalışan **Scavenger Pigeons (Çapulcu Güvercinler)** senaryo paketinin hem kanuni/sadakat yolunu hem de karanlık "Kör Sadakat / Gölge Tüneği" rotasını kapsayan nihai senaryo ve teknik tasarım dökümüdür.

---

## 0. Genel Bakış ve Mod Felsefesi

Çapulcu Güvercinler, Calradia kıtasında asırlardır lordların, tüccarların ve kralların kirli işlerini yürüten, dışarıdan alelade çapulcular gibi görünen ancak derinlerde tüm krallıkları saran kadim bir gizli istihbarat, şantaj ve suikast tarikatıdır.

Oyuncu, sokak dedikoduları ve berber fısıltıları ile bu kültün içine çekilir. Senaryo, 4. Görevde (*Anavatanın İhaneti*) oyuncunun vereceği ahlaki ve stratejik kararla iki taban tabana zıt ana hikaye koluna ayrılır:

```
                                [GÖREV 1, 2, 3: İLK TEMAS & ŞÜPHE]
                                                 │
                                                 ▼
                                  [GÖREV 4: ANAVATANIN İHANETİ]
                                                 │
                        ┌────────────────────────┴────────────────────────┐
                        ▼                                                 ▼
             [ROTA 1: SADAKAT & KANUN YOLU]                    [ROTA 2: KÖR SADAKAT & GÖLGE TÜNEĞİ]
          Dost'a inanılır, Krallığa girilir.                Zanaatkar oracıkta hançerlenir.
                        │                                                 │
                        ▼                                                 ▼
           [GÖREV 5A: KOPUK BAĞLAR]                         [GÖREV 5B: GÖLGE TÜNEĞİ'NİN ÇAĞRISI]
        Firari Zanaatkarın 70 kişilik                                     │
        firari müfrezesi avlanır.                                         ▼
                                                            [GÖREV 6B: KANLI ŞİFRELER & TASFİYE]
                                                                          │
                                                                          ▼
                                                            [GÖREV 7B: CALRADIA'NIN KUKLACISI]
                                                                  (3 Farklı Radikal Son)
```

---

## 1. ROTA 1: Sadakat ve Anavatana Hizmet Kolu

### Görev 1: Hain Güvercin (QLF_HainGuvercin)
- **Tetiklenme:** Oyuncu suikast emrinden şüphelenip beklediğinde veya bağımsız olarak başlar.
- **Aşama 0 (Tahkikat):** 1 Günlük bekleme süresi (`TimeoutInDays="1"`). Oyuncu gizli soruşturma yürütür, sokak muhbirlerinden haber bekler.
- **Aşama 1 (Çapulcu Avı):** Kendi adını kullanan 3 provokatör çapulcu partisini imha etme.
- **Aşama 2 (Hücrede Sorgu):** 1 Günlük bekleme (`TimeoutInDays="1"`). Esir alınan çapulcular hücrede sorgulanır.
- **Aşama 3 & 4 (Elebaşı ile Yüzleşme):** Saha koordinatörü ve elebaşı ile görüşme. Elebaşı, Zanaatkar'ın ihanetini ve oyuncunun asıl görevinin Anavatan'a dönmek olduğunu açıklar.
- **Sonuç:** Görev tamamlanır, oyuncu doğrudan `QLF_AnavataninIhaneti` görevindeki `Stage4_Dost_Inandi` aşamasına aktarılır.

### Görev 2 & 3: Berber Teması (QLF_ItibarDersi & QLF_YaDogruysa)
- **Konsept:** Çapulcularla haritada karşılaşma (savaş veya serbest bırakma).
- **Berber Sorgusu:** Oyuncu ikinci en yakın şehre gidip berbere *"Çapulcunun ismi ne?"* diye sorar.
- **Şifre Cevabı:** *"Anubis."* Berber örgütün felsefesini açıklar ve oyuncuya 70 kişilik bölük kurması için altın verir.

### Görev 4: Anavatanın İhaneti (QLF_AnavataninIhaneti)
- **Konsept:** Köy Büyüğü oyuncuya gizli şifreli bir deri verir. Oyuncu bunu `{UzakSehir}` şehrindeki `{ArtisanTarget}`'a götürür.
- **Sınav:** Zanaatkar, oyuncuya eski dostu `{Dost}`'u öldürmesini emreder.
- **Kanuni Rota Seçimi:** Oyuncu emri reddeder veya Dost ile yüzleşir. Dost, örgütte bir köstebek olduğunu söyler. Oyuncu Krallığa bağlılık yemini eder (`HasPledgedAllegiance`).
- **Ödül:** Dost oyuncuya 50.000 altın hediye eder. `SP_Dost` ve `SP_HainZanaatkar` hafızaya kaydedilir (`SaveHeroToGlobal`).

### Görev 5A: Kopuk Bağlar (QLF_KopukBaglar)
- **Konsept:** Finalden 7 gün sonra Dost'tan kanlı bir mektup gelir (`Stage0_Bekleme TimeoutInDays="7"`).
- **Tehdit:** Hain Zanaatkar, Güvercin ana şifrelerini çalarak firar etmiştir.
- **Dinamik Müfreze:** 100 kişilik hantal çapulcular yerine 70 kişilik seçkin firari müfrezesi (`PartyTemplateId="deserters_template"`, `Amount="70"`).
- **Zaman Kısıtı:**
  - Aşama 2 (Yem Partileri): 10 Gün içinde 3 parti yok edilmelidir.
  - Aşama 3 (Konvoy Yüzleşmesi): 5 Gün içinde müfreze durdurulmalıdır. Süre dolarsa konvoy sınırı aşar ve görev başarısız olur (`StageFail_Kacti`).
- **İki Farklı Bitiş:**
  1. *İdam ve Sadakat:* Müfreze yok edilir, Zanaatkar idam edilir (`KillHero`), şifreler kurtarılır, +5 Dost ilişkisi, +30 Renown ve `pigeon_secret_dagger`.
  2. *Yozlaşma ve Rüşvet:* Zanaatkar'ın 25.000 altınlık rüşveti kabul edilir, kaçmasına izin verilir (`MakeFugitive`), Renown -20, Gizli Hançer alınır.

---

## 2. ROTA 2: Kör Sadakat & Gölge Tüneği Kolu (Karanlık Rota)

### Kırılma Anı (Görev 4 İçi)
Oyuncu, Zanaatkar `{ArtisanTarget}`'ın huzurunda dururken onun entrikalarından ve örgütün yozlaşmasından iğrenir; ancak kralın kanunlarına başvurmak yerine örgütün mutlak infaz yasasını uygular. Zanaatkar'ı kendi tezgahı başında hançerler.

Zanaatkar'ın cesedinden çıkan **Kuzgun Kemik Şifresi (raven_bone_cipher)**, oyuncunun kapısını çalacak olan karanlık tarikat hücresi **Gölge Tüneği**'nin dikkatini çeker.

---

### Görev 5B: Gölge Tüneği'nin Çağrısı (QLF_GolgeTunegi_Cagri)
- **Önkoşul:** `GlobalFlag(SP_Zanaatkar_Katledildi) == 1`
- **Süre Sınırı:** 45 Gün
- **Aşama 0 (Soruşturma & Soğuma):** Şehir muhafızları cinayeti soruştururken 1 gün kuytuda gizlenme (`TimeoutInDays="1"`).
- **Aşama 1 (Kör Ulak):** Dağ sığınağında Gölge Ulağı ile buluşma. `MakeMet` ile ilk tanışma pürüzsüz gerçekleşir.
- **Aşama 2 (Sadakat Sınavı):** Zanaatkar'ın kaçırdığı şifreleri satın almak için bekleyen 50 kişilik yabancı ajan müfrezesini imha etme.
- **Aşama 3 (Kabul & Ödül):** Gölge Tüneği'ne kabul; `shadow_bone_dagger`, 30.000 altın ve +40 Renown.

---

### Görev 6B: Kanlı Şifreler ve Büyük Tasfiye (QLF_GolgeTunegi_Tasfiye)
- **Önkoşul:** `QLF_GolgeTunegi_Cagri` tamamlanması
- **Süre Sınırı:** 60 Gün
- **Kriz:** Zanaatkar'ın öldürülmesiyle paniğe kapılan `{SP_Dost}`, oyuncunun peşine düşer. Gölge Tüneği, tüm muhalif hücrelerin ve tanıkların tasfiyesini emreder.
- **Aşama 1 & 2 (Muhbirlerin Susturulması):**
  - İki farklı şehirdeki muhbirler (Tüccar ve Çete Lideri) köşeye sıkıştırılır. Ya doğrudan hançerlenir ya da altınları alınıp infaz edilir.
- **Aşama 3 (Eski Dost ile Kanlı Hesaplaşma):**
  - `{SP_Dost}` 70 kişilik kraliyet birliğiyle oyuncuya pusu kurar.
  - Meydan savaşında Dost yenilir ve idam edilir (`KillHero TargetAlias="Dost"`).
  - Ödül: 50.000 altın, +50 Renown ve mutlak örgüt yetkisi.

---

### Görev 7B: Calradia'nın Kuklacısı / Tahtların Altındaki Kan (QLF_GolgeTunegi_NihaiSon)
- **Önkoşul:** `QLF_GolgeTunegi_Tasfiye` tamamlanması
- **Süre Sınırı:** 90 Gün
- **Zirve Yüzleşmesi:** Gölge Tüneği'nin kalbinde, örgütün kadim kurucusu **Kör Usta (Blind Master)** ile buluşma. Kör Usta, Calradia'daki tüm tahtları sahte fermanlarla birbirine kırdıracak son planı hazırlar.

#### 3 Alternatif Radikal Son:

| Final Adı | Oyuncunun Seçimi | Mekanik & Aksiyonlar | Felsefi & Dünyevi Sonuç |
|---|---|---|---|
| **Son 1: Gölge Başefendi** *(The Shadow Sovereign)* | Kör Usta'yı zehirler/öldürür. Tahtı bizzat ele geçirir. | `KillHero TargetAlias="KorUsta"`<br>`GiveItem ObjectId="shadow_master_ring"`<br>`AddGold Amount="100000"`<br>`ChangeRenown Amount="100"` | Oyuncu Gölge Tüneği'nin yeni efendisi olur. Kralların yastıkları altına siyah tüyler bırakarak tüm Calradia'yı perde arkasından yönetir. |
| **Son 2: Kırık Kukla** *(The Broken Marionette)* | Kör sadakatin kölelik olduğunu anlar. Gölge Tüneği sığınağını ateşe verir. | Sığınak muhafızlarıyla savaş (40 elit birlik).<br>`KillHero TargetAlias="KorUsta"`<br>`ChangeCrimeRating Amount="60"`<br>`GiveItem ObjectId="pigeon_blood_claymore"` | Örgüt çöker. Oyuncu ne krallara ne de tarikata boyun eğmeyen, Calradia bozkırlarının yalnız ve lanetli celladı haline gelir. |
| **Son 3: Küllerin İmparatorluğu** *(Harbinger of Ruin)* | Sahte fermanları 3 büyük krallığın sarayına aynı anda sızdırır. | `DeclareWar TargetAlias="HedefKral"`<br>`AddGold Amount="250000"`<br>`ChangeRenown Amount="150"` | Üç imparatorluk birbirine savaş ilan eder. Calradia alevler içindeyken oyuncu kaostan beslenen en büyük savaş lorduna dönüşür. |

---

## 3. Teknik ve Altyapı Standartları (v1.0.0.109+)

1. **`MakeMet` İzolasyonu:**
   - Native tanışma pencerelerinin görev akışını ezmesini engellemek için tüm özel temaslarda (`KoyBuyugu`, `ArtisanTarget`, `Dost`, `GolgeUlagi`, `KorUsta`), diyalogdan önce aşama girişinde (`OnEnterActions`) `<Action Type="MakeMet" TargetAlias="..." />` çalıştırılır.
2. **Birlik Şablonu:**
   - 100 kişilik hantal çapulcular kaldırılmış; v1.0.0.109 motorunun tam desteklediği `deserters_template` (70 kişilik firari müfrezesi) entegre edilmiştir.
3. **Zaman Aşımı ve Gerilim:**
   - Tahkikat aşamaları (`TimeoutInDays="1"`) ve firarilerin sınırı geçme süresi (`TimeoutInDays="5"`) eklenerek dinamik süre kısıtları sağlanmıştır.
4. **Çift Dilli Yerelleştirme:**
   - Tüm yeni ve mevcut stringler hem İngilizce ana dosyada hem de `ModuleData/Languages/TR/std_ScavengerPigeons_tr.xml` dosyasında tam karşılık bulmuştur.
