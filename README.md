# Regression Studio

C# / .NET 8 Windows Forms ile hazırlanmış, hazır makine öğrenmesi kütüphanesi kullanmadan çalışan regresyon ödevi.

## Modeller

- Linear Regression — Z-Score + batch gradient descent + gradient clipping
- MLP Regression — 1 → Hidden(ReLU) → 1, manual forward/backpropagation, momentum, gradient clipping

## Regression Studio UI

- Grafik üzerinde mouse ile nokta ekleme, sürükleme ve sağ tıkla silme
- DataGridView üzerinden X/Y düzenleme
- CSV import/export
- Linear ve MLP eğrilerini aynı grafikte karşılaştırma
- Train/Test split
- Residual çizgileri ve residual plot
- MSE, RMSE, MAE, R²
- Linear model denklemi / MLP network kartı
- Learning curve
- Tek X değeri için iki modelden tahmin
- Linear, Quadratic, Sinusoidal, Noisy Linear, Nonlinear veri üreticileri
- Gaussian noise slider
- Learning-rate sweep
- Epoch sweep
- Test MSE / R² deney tablosu

## Çalıştırma

1. Visual Studio 2022 ile `RegressionStudio.sln` açın.
2. `.NET Desktop Development` workload ve .NET 8 SDK kurulu olmalı.
3. `RegressionApp` startup project seçin.
4. F5 ile çalıştırın.

Bu proje eğitim amaçlıdır. ML.NET, TensorFlow, Accord.NET gibi hazır model kütüphaneleri kullanılmaz.

## GitHub repo bilgileri

- **Depo adı:** `RegressionStudio_ModernDashboard`
- **Platform:** Windows, .NET 8, Windows Forms
- **Solution:** `RegressionStudio.sln`
- **Yapı:** Uygulama projesi + bağımsız `ML.Core` kaynak kodu

### Komut satırından derleme (Windows)

```powershell
dotnet restore RegressionStudio.sln
dotnet build RegressionStudio.sln --configuration Release
```

GitHub Actions, her push ve pull request için Windows runner üzerinde derleme kontrolü yapacak şekilde yapılandırılmıştır. **Bu depoya henüz otomatik test projesi eklenmemiştir.**

> Not: Lisans dosyası kaynak pakette bulunmadığı için otomatik olarak bir açık kaynak lisansı eklenmedi. Lisans koşulları depo sahibi tarafından belirlenmelidir.
