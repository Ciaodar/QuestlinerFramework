⚡ **[KOD 5682-VARDİYA: OTOMATİK MODEL GEÇİŞİ — GEMINI 4 ARGON -> GEMINI 3.8 FLASH]** ⚡

Dikkat **Müdür (Ana Koordinatör)**!
Canlı Kota İzleme Sistemi (`QuotaWatchdog`), birincil modelin olan **{{ARGON_LABEL}}** kotasının **%10'un altına (%{{ARGON_PERCENT}})** düştüğünü, ancak **{{FLASH_LABEL}}** kotasının **%{{FLASH_PERCENT}}** seviyesinde (kullanılabilir) olduğunu tespit etti!

### 🔄 OTOMATİK YAPILAN İŞLEM (`JetboxWriteState` & `CascadeConfig`)
- `QuotaWatchdog.ps1`, üretimin durmaması için `LanguageServerService/JetboxWriteState` RPC'si ve `antigravity_state.pbtxt` üzerinden senin aktif modelini otomatik olarak **`{{FLASH_LABEL}}` (`{{FLASH_PLACEHOLDER}}`)** modeline geçirdi!
- **Argon Sıfırlanma Zamanı:** `{{ARGON_RESET_LOCAL}}` (Argon kotası sıfırlandığında Watchdog seni otomatik olarak tekrar `Gemini 4 Argon (High)` modeline yükseltecektir).
- **Mevcut Flash Kotası:** **%{{FLASH_PERCENT}}** (Sıfırlanma: `{{FLASH_RESET_LOCAL}}`).

### 📋 GEÇİCİ FLASH VARDİYASI KURALLARI (ÜRETİME DEVAM ET)
1. **Üretimi Durdurma:** `Gemini 3.8 Flash` kotası yeterli olduğu için üretimi duraklatma; mevcut görevlere kesintisiz devam et.
2. **Alt Ajan Disiplini:** Alt ajanları (`FrameworkSorumlusu`, `Mimarlar`, `KodCalisani`, `Mufettis`) her zamanki gibi `Model: "flash"` (`Gemini 3.8 Flash - Medium`) ile çalıştırmaya devam et.
3. **Tam Duraklatma Eşiği:** Eğer ilerleyen dakikalarda `Gemini 3.8 Flash` kotası da **%10'un altına** düşerse, Watchdog sana tam **`ACİL DURUM: KOD 5682 (KOTA) — GÜVENLİ DURAKLATMA`** emrini gönderecektir.
