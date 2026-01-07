# BlogApp

Modern bir blog uygulaması - ASP.NET Core MVC ile geliştirilmiş, kullanıcı yönetimi, blog yazıları, yorumlar, beğeniler ve admin paneli içeren tam özellikli bir platform.

## 🚀 Özellikler

### Kullanıcı Özellikleri
- ✅ Kullanıcı kaydı ve girişi (JWT Authentication)
- ✅ Profil yönetimi (güncelleme, şifre değiştirme)
- ✅ Blog yazısı oluşturma, düzenleme, silme
- ✅ Kategori bazlı yazı filtreleme
- ✅ Yazılara yorum yapma
- ✅ Yazıları beğenme
- ✅ Kullanıcı takip sistemi
- ✅ Şifre sıfırlama (email ile)
- ✅ Cover image yükleme (URL veya dosya)

### Admin Özellikleri
- ✅ Admin paneli (Dashboard, Kullanıcılar, Yazılar, Kategoriler)
- ✅ Blog yazılarını onaylama/yayından kaldırma
- ✅ Kullanıcı durumu yönetimi (Aktif, Askıya Alınmış, Yasaklanmış)
- ✅ Kategori yönetimi (oluşturma, silme)
- ✅ Aktivite logları (tüm işlemlerin kaydı)
- ✅ İstatistikler (kullanıcı sayıları, yazı sayıları)

### Teknik Özellikler
- ✅ RabbitMQ ile asenkron email gönderimi
- ✅ Otomatik kullanıcı aktivasyonu (5 gün sonra suspended kullanıcılar aktif olur)
- ✅ Action Filter ile otomatik loglama sistemi
- ✅ Email bildirimleri (yeni yazı, onay, yasaklama vb.)
- ✅ IP adresi takibi
- ✅ Session ve JWT tabanlı kimlik doğrulama

## 🛠️ Teknolojiler

- **Backend:** ASP.NET Core 10.0 (MVC + Web API)
- **Veritabanı:** PostgreSQL
- **ORM:** Entity Framework Core
- **Authentication:** JWT Bearer Tokens
- **Message Queue:** RabbitMQ
- **Email:** MailKit (SMTP)
- **Password Hashing:** BCrypt
- **Frontend:** Bootstrap 5, jQuery

## 📋 Gereksinimler

- .NET 10.0 SDK
- PostgreSQL 12+
- RabbitMQ Server
- SMTP sunucu (Gmail vb.)

## 🔧 Kurulum

### 1. Repository'yi klonlayın
```bash
git clone <repository-url>
cd BlogApp
```

### 2. Veritabanını oluşturun
```bash
# PostgreSQL'de veritabanı oluştur
createdb BlogDatabase
```

### 3. Environment değişkenlerini ayarlayın
`.env` dosyası oluşturun (root dizinde):
```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=BlogDatabase
DB_USER=postgres
DB_PASS=your_password

# Application
APP_PORT=5055

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE_NAME=email_queue

# SMTP (Gmail örneği)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your_email@gmail.com
SMTP_PASSWORD=your_app_password
SMTP_FROM_EMAIL=your_email@gmail.com
SMTP_FROM_NAME=BlogApp

# JWT
JWT_KEY=your_secret_key_here_min_32_characters
JWT_ISSUER=BlogApp
JWT_AUDIENCE=BlogAppUsers
```

### 4. Migration'ları uygulayın
```bash
cd src/BlogApp
dotnet ef database update
```

### 5. RabbitMQ'yu başlatın
```bash
# Docker ile
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# veya sistem servisi olarak
sudo systemctl start rabbitmq-server
```

### 6. Uygulamayı çalıştırın
```bash
cd src/BlogApp
dotnet run
```

Uygulama `http://localhost:5055` adresinde çalışacaktır.

## 📁 Proje Yapısı

```
BlogApp/
├── src/BlogApp/
│   ├── Controllers/          # MVC ve API Controller'ları
│   ├── Models/               # Veritabanı modelleri
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Services/             # İş mantığı servisleri
│   ├── Filters/              # Action Filter'lar (Loglama)
│   ├── Data/                 # DbContext
│   ├── Views/                # Razor View'lar
│   ├── Migrations/           # EF Core migrations
│   └── wwwroot/              # Static dosyalar
```

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/register` - Kullanıcı kaydı
- `POST /api/auth/login` - Giriş yap
- `POST /api/auth/forgot-password` - Şifre sıfırlama talebi
- `POST /api/auth/reset-password` - Şifre sıfırla

### Blog Posts
- `GET /api/blogpost/published` - Yayınlanmış yazıları getir
- `GET /api/blogpost/{id}` - Yazı detayı
- `POST /BlogPost/Create` - Yeni yazı oluştur
- `PUT /api/blogpost/{id}` - Yazı güncelle
- `DELETE /api/blogpost/{id}` - Yazı sil
- `POST /api/blogpost/{id}/comment` - Yorum ekle
- `POST /api/blogpost/{id}/like` - Beğeni ekle/kaldır
- `POST /api/blogpost/upload-image` - Cover image yükle

### Profile
- `GET /api/profile` - Profil bilgilerini getir
- `PUT /api/profile/update` - Profil güncelle
- `PUT /api/profile/password` - Şifre değiştir
- `GET /api/profile/followers` - Takipçileri getir
- `GET /api/profile/following` - Takip edilenleri getir

### User
- `POST /api/user/{id}/follow` - Kullanıcıyı takip et/takipten çık
- `GET /api/user/{id}/is-following` - Takip durumunu kontrol et

### Admin
- `GET /api/admin/draft-posts` - Taslak yazıları getir
- `POST /api/admin/approve-post/{id}` - Yazıyı onayla
- `POST /api/admin/unpublish-post/{id}` - Yazıyı yayından kaldır
- `DELETE /api/admin/delete-post/{id}` - Yazıyı sil
- `GET /api/admin/users` - Kullanıcıları listele
- `PUT /api/admin/update-user-status` - Kullanıcı durumu güncelle
- `POST /api/admin/create-admin` - Yeni admin oluştur
- `GET /api/admin/stats` - İstatistikler
- `GET /api/admin/activity-logs` - Aktivite logları
- `GET /api/admin/activity-logs/filters` - Log filtreleri

### Category
- `GET /api/category` - Kategorileri listele
- `POST /api/category` - Kategori oluştur (Admin)
- `DELETE /api/category/{id}` - Kategori sil (Admin)

## 🔐 Kullanıcı Rolleri

- **User:** Normal kullanıcı - yazı oluşturabilir, yorum yapabilir
- **Admin:** Yönetici - tüm işlemleri yapabilir, yazıları onaylayabilir

## 📊 Veritabanı Modelleri

- **User:** Kullanıcı bilgileri, durum yönetimi
- **BlogPost:** Blog yazıları, draft/Published durumu
- **Category:** Yazı kategorileri
- **Comment:** Yazı yorumları
- **PostLike:** Yazı beğenileri
- **UserFollower:** Kullanıcı takip sistemi
- **EmailQueue:** Email gönderim logları
- **PasswordResetToken:** Şifre sıfırlama token'ları
- **ActivityLog:** Sistem aktivite logları

## 🔄 Background Services

### EmailConsumerService
- RabbitMQ'dan email mesajlarını dinler
- Takipçilere yeni yazı bildirimi gönderir
- Arka planda sürekli çalışır

### UserAutoActivationService
- Her saat suspended kullanıcıları kontrol eder
- 5 gün önce suspended olanları otomatik aktif eder

## 📝 Loglama Sistemi

Tüm önemli işlemler otomatik olarak loglanır:
- Kullanıcı işlemleri (giriş, kayıt, profil güncelleme)
- Blog yazısı işlemleri (oluşturma, güncelleme, silme)
- Admin işlemleri (onaylama, durum değiştirme)
- IP adresi, kullanıcı bilgisi, işlem açıklaması kaydedilir

Admin panelinden `/Admin/ActivityLogs` sayfasından görüntülenebilir.

## 🎨 Frontend

- Bootstrap 5 ile responsive tasarım
- jQuery ile dinamik içerik
- AJAX ile API çağrıları
- Form validasyonu
- Modern ve kullanıcı dostu arayüz

## 🚀 Deployment

### Production için öneriler:
1. Environment değişkenlerini production değerleriyle güncelleyin
2. HTTPS kullanın
3. PostgreSQL connection pooling yapılandırın
4. RabbitMQ cluster kurulumu
5. SMTP servisini production için yapılandırın
6. Static dosyalar için CDN kullanın

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

## 👤 Geliştirici

Proje hakkında sorularınız için issue açabilirsiniz.

## 🙏 Teşekkürler

- ASP.NET Core ekibine
- Entity Framework Core ekibine
- RabbitMQ ekibine
- Tüm açık kaynak topluluğuna

