🚨 **[ACİL DURUM PROTOKOLÜ: KOD 5682 (KOTA) — GÜVENLİ DURAKLATMA VE OTONOM DEVAM EMRİ]** 🚨

Dikkat **Müdür (Ana Koordinatör)**!
Canlı Kota İzleme Sistemi (`QuotaWatchdog`), aktif model kotasının kritik eşiğin (**%10**) altına düştüğünü tespit etti:

- **Kritik Kotaya Giren Model:** `{{MODEL_LABEL}}` (`{{MODEL_ID}}`)
- **Kalan Kota Oranı:** **%{{REMAINING_PERCENT}}**
- **Kota Sıfırlanma Zamanı (UTC):** `{{RESET_TIME_UTC}}`
- **Kota Sıfırlanma Zamanı (Yerel):** `{{RESET_TIME_LOCAL}}`
- **Sıfırlanmaya Kalan Süre (+60sn Güvenlik Payı):** `{{WAIT_SECONDS}}` saniye (~`{{WAIT_MINUTES}}` dakika)

---

### 🛑 ZORUNLU KOD 5682 (KOTA) EYLEM PLANI (DERHAL UYGULA)

Projenin, kod dosyalarının, XML senaryolarının ve alt ajan (subagent) hafızalarının kota bitimi nedeniyle **yarım veya bozuk (corrupted) kalmasını engellemek** için aşağıdaki 4 adımı **bu tur içinde derhal ve sırasıyla** uygula:

#### 1. Alt Ajanları Güvenli Şekilde Durdur (Subagent Safe Interrupt)
- `manage_subagents` (`Action: "list"`) aracını çağırarak aktif alt ajanları kontrol et.
- Durumu `running` veya `waiting_for_input` olan her alt ajana `send_message` ile şu acil emri gönder:
  > `[ACİL DURUM: KOD 5682 (KOTA)] Model kotası %10'un altına düştü! Üzerinde çalıştığın dosya düzenlemesini yarım bırakmadan ATOMİK olarak hemen tamamla, yeni bir dosya düzenlemesine veya derlemeye BAŞLAMA. Mevcut durumunu tek paragrafta özetle ve DERHAL bekleme (idle) moduna geç.`
- Hiçbir alt ajanı `kill` etme (hafızaları korunacaktır); sadece güvenli noktada (checkpoint) durmalarını sağla.

#### 2. Üretimi Dondur ve Checkpoint Kaydet (`quota_checkpoint.md`)
- Yeni bir C# veya XML düzenlemesi, yeni bir `invoke_subagent` çağrısı veya ağır bir işlem **BAŞLATMA**.
- `write_to_file` (`Overwrite: true`) kullanarak `c:\Users\meteh\RiderProjects\QuestlinerFramework\.agents\quota_checkpoint.md` dosyasına:
  - Şu an üzerinde çalışılan görevi,
  - Hangi alt ajanların hangi aşamada duraklatıldığını,
  - Kota sıfırlandığında ilk yapılacak adımı (Devam Planı) net olarak kaydet.

#### 3. Kota Sıfırlanma Zamanına Otonom Zamanlayıcı (`schedule`) Kur
- `schedule` aracını **kesinlikle** şu parametrelerle çağır:
  - `DurationSeconds`: `{{WAIT_SECONDS}}`
  - `TimerCondition`: `"never"`
  - `Prompt`:
    `[KOD 5682 OTONOM DEVAM SİNYALİ] Model kotası ({{MODEL_LABEL}}) sıfırlandı! İlk iş olarak c:\Users\meteh\RiderProjects\QuestlinerFramework\.agents\scripts\QuotaWatchdog.ps1 -CheckOnce çalıştırarak kotanın yenilendiğini doğrula, ardından c:\Users\meteh\RiderProjects\QuestlinerFramework\.agents\quota_checkpoint.md dosyasını oku ve duraklatılan alt ajanlara devam talimatı vererek üretime kaldığın yerden devam et.`

#### 4. Patrona Bilgi Ver ve Turu Sonlandır
- Patrona **KOD 5682 (KOTA) Güvenli Duraklatma Raporu**'nu sun (kalan kota, kaydedilen checkpoint ve `{{RESET_TIME_LOCAL}}` saatine kurulan otomatik devam zamanlayıcısını belirt).
- Başka hiçbir araç (tool) çağırmadan turunu sonlandır ve zamanlayıcı ateşlenene kadar bekle.
