# Scavenger Pigeons (Çapulcu Güvercinler) - Senaryo Raporu

## Mod Özeti
Çapulcu Güvercinler, Calradia'daki asilzadelerin kirli işlerini yapan, kendilerini sıradan çapulcular gibi gizleyen gizli bir istihbarat ve suikast kültüdür. Oyuncu, bu örgütün derinliklerine çekilir ve sadakati çeşitli komplolarla test edilir.

## Görev Serisi (Questline)

### 1. Hain Güvercin (QLF_HainGuvercin)
- **Hikaye:** Oyuncu tavernada şüpheli bir figürle karşılaşır. Kendini "Kurye" olarak tanıtan bu kişi, Çapulcu Güvercinler'den kaçmaktadır ve oyuncudan yardım ister. 
- **Mekanik:** Oyuncu kuryeyi korumayı veya satmayı seçebilir. Bu görev örgütle ilk teması sağlar.

### 2. İtibar Dersi (QLF_ItibarDersi)
- **Hikaye:** Örgüt, oyuncunun yeteneklerini test etmek için onu zengin bir tüccarı "korkutmaya" gönderir.
- **Mekanik:** Tüccarı dövmek veya ondan rüşvet alıp örgütü kandırmak oyuncunun seçimidir.

### 3. Ya Doğruysa? (QLF_YaDogruysa)
- **Hikaye:** Oyuncu, sıradan görünen bir suikast emri alır. Ancak hedefin masum bir rahip/bilgin olduğu ortaya çıkar.
- **Mekanik:** Hedefi öldürmek veya gerçeği araştırmak. Örgütün her emrinin sorgulanmadan yerine getirilip getirilmemesi gerektiği test edilir.

### 4. Anavatanın İhaneti (QLF_AnavataninIhaneti)
- **Hikaye:** Oyuncu, örgüt içinde büyük bir ihanetle suçlanır. Bir "Dost", oyuncuyu uyararak kaçmasını söyler. Gerçekte bu, oyuncunun anavatana ve örgüte olan nihai sadakat testidir.
- **Mekanik:** `SaveHeroToGlobal` kullanılarak "Dost" ve sahte emri veren "Zanaatkar" hafızaya alınır. Görev, oyuncunun masumiyetini kanıtlaması (veya testin bir oyun olduğunu öğrenmesi) ile biter ve oyuncu yüklü miktarda altınla ödüllendirilir.

### 5. Kopuk Bağlar (QLF_KopukBaglar)
- **Hikaye:** Önceki görevin finalinden 1 hafta sonra başlar. "Dost", oyuncuya ulaşarak sahte emri veren Zanaatkar'ın aslında gerçekten bir hain olduğunu ve gizli şifreleri çalıp kaçtığını söyler.
- **Mekanik:** 
  - Görev `AutoStart` ile başlar ancak ilk aşama `Stage0_Bekleme` 7 günlük bir Timeout süresine sahiptir.
  - Oyuncu 3 sahte çapulcu grubunu avlar (`DefeatedParties`).
  - Hainin 100 kişilik ana konvoyu pusuya düşürülür. 
  - Zanaatkar ile diyalogda oyuncu, rüşveti kabul edip Zanaatkar'ın kaçmasına izin verebilir (`MakeFugitive`) ya da onu savaşta yenip idam edebilir (`BattleResolvedAs="Victory"` ve `KillHero`).

## Teknik Notlar
- Tüm senaryo, görevler arası NPC tutarlılığını sağlamak için Framework'ün `GlobalHero` ve `SaveHeroToGlobal` yapılarına sıkı sıkıya bağlıdır.
- Görevler İngilizce (Base) ve Türkçe (TR XML) olarak tamamen yerelleştirilmiştir.
- Otomatik versiyonlama altyapısı `Build/UpdateVersion.ps1` üzerinden sağlanmıştır.
