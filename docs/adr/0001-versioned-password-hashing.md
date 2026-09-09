# ADR-001: Sürümlü PBKDF2 parola biçimi ve kademeli geçiş

- Durum: Kabul edildi
- Tarih: 2026-09-09

## Bağlam

İlk sürüm parolayı saltsız tek tur SHA-256 ile saklıyordu. Bu yöntem parola saklama için yetersizdir; ancak mevcut yerel kurulumların girişini bir anda bozmak da istenmez.

## Karar

Yeni parolalar rastgele 16 bayt salt, PBKDF2-HMAC-SHA256 ve 600.000 iterasyonla türetilecek. Saklanan metin algoritma, iterasyon, salt ve hash değerini taşıyan sürümlü bir biçim olacak. Eski 64 karakterlik SHA-256 kayıtları yalnızca geçiş amacıyla doğrulanacak ve başarılı girişte koşullu güncellemeyle PBKDF2'ye yükseltilecek.

Yeni veritabanında hazır kullanıcı/parola bulunmayacak; ilk yönetici yalnızca açıkça ayarlanmış ortam değişkenlerinden oluşturulacak.

## Sonuçlar

Yeni kayıtlar benzersiz salt ve maliyetli parola türetme kullanır. Eski kullanıcılar kesinti yaşamadan taşınabilir. Legacy doğrulama kodu tüm kurulumlar taşındıktan sonra kaldırılmalıdır; üretimde ayrıca merkezi kimlik sağlayıcı ve MFA değerlendirilmelidir.
