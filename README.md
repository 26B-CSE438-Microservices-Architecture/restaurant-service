# Restaurant Service — Trendyol GO Clone

> **Mikroservis Mimarisi Projesi** · CSE 438  
> Ekosistem içindeki tüm restoran, menü ve ürün verilerinin tek yetkili kaynağıdır.

Platformda ayrı bir arama servisi bulunmadığından, **Arama & Keşif** mantığı da bu servis tarafından yönetilmektedir — isim ile arama, mutfak filtreleme ve konum bazlı sıralama işlemleri burada gerçekleştirilir.

---

## İçindekiler

- [Temel Özellikler](#temel-özellikler)
- [Tech Stack](#tech-stack)
- [Database Şeması](#database-şeması)
- [API Endpointleri](#api-endpointleri)
- [Servisler Arası İletişim](#servisler-arası-iletişim)
- [Domain Eventler](#domain-eventler)
- [Hızlı Başlangıç](#hızlı-başlangıç)

---

## Temel Özellikler

| Özellik | Açıklama |
|---|---|
| **Restoran Yönetimi** | Yeni partner ekleme, profil bilgilerinin yönetimi (konum, iletişim, çalışma saatleri) |
| **Arama & Keşif** | İsme göre arama, mutfak türü filtreleme, mesafeye göre sıralama |
| **Dinamik Menüler** | Her restoran için hiyerarşik kategori → ürün yapısı |
| **Anlık Durum Yönetimi** | Mağaza durumunu (`Open` / `Closed` / `Busy`) ve ürün stoğunu değiştirme |
| **Coğrafi Konum Desteği** | PostGIS ile enlem/boylam koordinatları üzerinden yakınlık sorguları |
| **Operasyonel Kurallar** | Restoran bazında minimum sipariş tutarı ve teslimat ücreti |

---

## Tech Stack

| Katman | Teknoloji |
|---|---|
| Çalışma Ortamı | .NET 9 / ASP.NET Core Web API |
| Veritabanı | PostgreSQL 16 + PostGIS 3.4 |
| ORM | Entity Framework Core + NetTopologySuite |
| Konteynerizasyon | Docker & Docker Compose |
| API Dokümantasyonu | Swagger / OpenAPI v1 |
| Veritabanı Yönetimi | pgAdmin 4 |

---

## Database Şeması

**Database:** `restaurantdb` (PostgreSQL 16 + PostGIS)

### Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────┐
│                      Restaurant                         │
├─────────────────────────────────────────────────────────┤
│ PK │ id              │ UUID  (gen_random_uuid())        │
│    │ name            │ VARCHAR(200)  NOT NULL            │
│    │ description     │ VARCHAR(500)                      │
│    │ address_text    │ VARCHAR(500)                      │
│    │ latitude        │ DOUBLE PRECISION                  │
│    │ longitude       │ DOUBLE PRECISION                  │
│    │ logo_url        │ VARCHAR(500)                      │
│    │ min_order_amount│ DECIMAL(10,2)                     │
│    │ delivery_fee    │ DECIMAL(10,2)                     │
│    │ is_active       │ BOOLEAN  (default: true)          │
│    │ status          │ VARCHAR(20) [Open/Closed/Busy]    │
│    │ opening_time    │ TIME                              │
│    │ closing_time    │ TIME                              │
│    │ created_at      │ TIMESTAMP                         │
│    │ updated_at      │ TIMESTAMP                         │
└──────────────────┬──────────────────────────────────────┘
                   │ 1 : N
                   ▼
┌─────────────────────────────────────────────────────────┐
│                     MenuCategory                        │
├─────────────────────────────────────────────────────────┤
│ PK │ id              │ UUID  (gen_random_uuid())        │
│ FK │ restaurant_id   │ UUID → Restaurant.id (CASCADE)   │
│    │ name            │ VARCHAR(200)  NOT NULL            │
│    │ display_order   │ INTEGER                           │
└──────────────────┬──────────────────────────────────────┘
                   │ 1 : N
                   ▼
┌─────────────────────────────────────────────────────────┐
│                       Product                           │
├─────────────────────────────────────────────────────────┤
│ PK │ id              │ UUID  (gen_random_uuid())        │
│ FK │ category_id     │ UUID → MenuCategory.id (CASCADE) │
│    │ name            │ VARCHAR(200)  NOT NULL            │
│    │ description     │ VARCHAR(500)                      │
│    │ price           │ DECIMAL(10,2)                     │
│    │ is_available    │ BOOLEAN  (default: true)          │
│    │ image_url       │ VARCHAR(500)                      │
└─────────────────────────────────────────────────────────┘
```

## API Endpointleri

**Base URL:** `http://localhost:5001/api/v1`  
**Swagger UI:** `http://localhost:5001/swagger`

### Discovery (Müşteri Tarafı)

| Metot | Endpoint | Açıklama |
|---|---|---|
| `GET` | `/restaurants` | Restoranları listele / ara |
| `GET` | `/restaurants/{id}` | Restoran detayını getir |
| `GET` | `/restaurants/nearby` | Yakındaki restoranları bul |
| `GET` | `/restaurants/{restaurantId}/menu` | Tam menüyü getir (kategoriler + ürünler) |

<details>
<summary>Örnek Yanıt — <code>GET /restaurants</code></summary>

```json
[
  {
    "id": "a1b2c3d4-...",
    "name": "Burger King",
    "description": "Fast food",
    "logoUrl": "https://...",
    "minOrderAmount": 50.00,
    "deliveryFee": 9.99,
    "status": "Open",
    "distanceKm": 2.4
  }
]
```

</details>

<details>
<summary>Örnek Yanıt — <code>GET /restaurants/{id}/menu</code></summary>

```json
{
  "restaurantId": "a1b2c3d4-...",
  "restaurantName": "Burger King",
  "categories": [
    {
      "id": "cat-001",
      "name": "Burgers",
      "displayOrder": 1,
      "products": [
        {
          "id": "prod-001",
          "name": "Classic Whopper",
          "description": "Flame-grilled beef patty",
          "price": 89.90,
          "isAvailable": true,
          "imageUrl": "https://..."
        }
      ]
    }
  ]
}
```

</details>

---

### Restoran Yönetimi (İşletme Sahibi Tarafı)

| Metot | Endpoint | Açıklama | Request Body |
|---|---|---|---|
| `POST` | `/restaurants` | Yeni restoran kaydet | `CreateRestaurantDto` |
| `PUT` | `/restaurants/{id}` | Restoran profilini güncelle | `UpdateRestaurantDto` |
| `PATCH` | `/restaurants/{id}/status` | Durum değiştir (Open/Closed/Busy) | `UpdateStatusDto` |
| `DELETE` | `/restaurants/{id}` | Restoranı sil | — |

<details>
<summary>Request Body — <code>POST /restaurants</code></summary>

```json
{
  "name": "Burger King",
  "description": "Fast food restaurant",
  "addressText": "Kadıköy, İstanbul",
  "latitude": 40.9907,
  "longitude": 29.0230,
  "logoUrl": "https://example.com/logo.png",
  "minOrderAmount": 50.00,
  "deliveryFee": 9.99,
  "openingTime": "09:00",
  "closingTime": "23:00"
}
```

</details>

<details>
<summary>Request Body — <code>PATCH /restaurants/{id}/status</code></summary>

```json
{
  "status": "Busy"    // "Open" | "Closed" | "Busy"
}
```

</details>

---

### Menü & Kategori Yönetimi

| Metot | Endpoint | Açıklama | Request Body |
|---|---|---|---|
| `POST` | `/restaurants/{restaurantId}/categories` | Menü kategorisi ekle | `CreateCategoryDto` |
| `PUT` | `/categories/{id}` | Kategoriyi güncelle | `UpdateCategoryDto` |
| `DELETE` | `/categories/{id}` | Kategoriyi sil | — |

<details>
<summary>Request Body — <code>POST /restaurants/{restaurantId}/categories</code></summary>

```json
{
  "name": "Burgers",
  "displayOrder": 1
}
```

</details>

---

### Ürün Yönetimi

| Metot | Endpoint | Açıklama | Request Body |
|---|---|---|---|
| `POST` | `/categories/{categoryId}/products` | Kategoriye ürün ekle | `CreateProductDto` |
| `PUT` | `/products/{id}` | Ürün bilgilerini güncelle | `UpdateProductDto` |
| `PATCH` | `/products/{id}/stock` | Stok durumunu değiştir | `UpdateStockDto` |
| `DELETE` | `/products/{id}` | Ürünü sil | — |

<details>
<summary>Request Body — <code>POST /categories/{categoryId}/products</code></summary>

```json
{
  "name": "Classic Whopper",
  "description": "Flame-grilled beef patty with fresh veggies",
  "price": 89.90,
  "imageUrl": "https://example.com/whopper.png"
}
```

</details>

<details>
<summary>Request Body — <code>PATCH /products/{id}/stock</code></summary>

```json
{
  "isAvailable": false
}
```

</details>

---

## Servisler Arası İletişim

### İletişim Protokolleri

```
                          ┌──────────────┐
                          │  API Gateway │
                          └──────┬───────┘
                                 │  REST (HTTP)
                                 ▼
                     ┌───────────────────────┐
                     │  Restaurant Service   │
                     │    (bu servis)        │
                     └───┬──────────┬────────┘
                         │          │
            gRPC (sync)  │          │  Event (async)
                         │          │
              ┌──────────▼──┐   ┌──▼──────────────┐
              │Order Service│   │  Message Broker  │
              │             │   │ (RabbitMQ/Kafka) │
              └─────────────┘   └──┬─────┬────────┘
                                   │     │
                            ┌──────▼┐  ┌─▼────────┐
                            │Mobil  │  │ Frontend  │
                            │Uygulama│  │ Uygulama │
                            └───────┘  └──────────┘
```

### 1. REST API — API Gateway → Restaurant Service

| Yön | Çağıran | Amaç |
|---|---|---|
| **Gelen** | API Gateway | Mobil ve Frontend uygulamalardan gelen tüm HTTP isteklerini yönlendirir |

### 2. gRPC — Order Service → Restaurant Service (Senkron)

| Yön | Çağıran | Amaç |
|---|---|---|
| **Gelen** | Order Service | Ödeme sırasında **fiyat ve stok doğrulaması** yapar |

Order Service, sipariş oluşturulmadan önce sepetteki ürünlerin **hâlâ mevcut** olduğunu ve **fiyatların doğru** olduğunu bu servis üzerinden doğrular.

```protobuf
// Planlanan gRPC sözleşmesi
service RestaurantGrpc {
  rpc ValidateBasketItems (ValidateBasketRequest) returns (ValidateBasketResponse);
  rpc GetProductDetails   (ProductIdList)         returns (ProductDetailsList);
}
```

### 3. Event-Driven — Restaurant Service → Message Broker (Asenkron)

Restoran durumu veya stok değiştiğinde tüm sistemi asenkron olarak bilgilendirir.

---

## Domain Eventler

### Bu Servisin Yayınladığı Eventler (Outbound)

Aşağıdaki eventler bu servis tarafından **yayınlanır** ve diğer servisler tarafından tüketilir:

| Event Adı | Topic/Queue | Tetikleyici | Payload | Tüketiciler |
|---|---|---|---|---|
| `RestaurantStatusChanged` | `restaurant.status.changed` | Owner durum değişikliği yaptığında (PATCH `/status`) | `{ restaurantId, oldStatus, newStatus, timestamp }` | **Order Service** · **Mobil Uygulama** · **Frontend** |
| `RestaurantCreated` | `restaurant.created` | Yeni restoran kaydedildiğinde (POST `/restaurants`) | `{ restaurantId, name, latitude, longitude, timestamp }` | **API Gateway** · **Frontend** |
| `RestaurantUpdated` | `restaurant.updated` | Restoran bilgileri güncellendiğinde (PUT `/restaurants/{id}`) | `{ restaurantId, changedFields[], timestamp }` | **Frontend** · **Mobil Uygulama** |
| `RestaurantDeleted` | `restaurant.deleted` | Restoran silindiğinde (DELETE `/restaurants/{id}`) | `{ restaurantId, timestamp }` | **Order Service** · **API Gateway** |
| `ProductStockChanged` | `product.stock.changed` | Ürün stok durumu değiştiğinde (PATCH `/products/{id}/stock`) | `{ productId, restaurantId, isAvailable, timestamp }` | **Order Service** · **Mobil Uygulama** |
| `MenuUpdated` | `menu.updated` | Kategori veya ürün eklendiğinde / güncellendiğinde / silindiğinde | `{ restaurantId, action, entityType, entityId, timestamp }` | **Frontend** · **Mobil Uygulama** |
| `PriceChanged` | `product.price.changed` | Ürün fiyatı güncellendiğinde (PUT `/products/{id}`) | `{ productId, restaurantId, oldPrice, newPrice, timestamp }` | **Order Service** |

### Bu Servisin Dinlediği Eventler (Inbound)

Aşağıdaki eventleri diğer servislerden **dinler**:

| Event Adı | Kaynak Servis | Amaç | Yapılan İşlem |
|---|---|---|---|
| `OrderCompleted` | Order Service | Sipariş tamamlandığında bilgi almak | İleride istatistik / analitik verisi toplanabilir |
| `UserRoleChanged` | User Service | Kullanıcı rolü değiştiğinde (yeni owner atanması) | Restoran yetkilendirmesini güncelleme |

---

### Diğer Servislerden Ne Bekliyoruz?

| Servis | Ne Bekliyoruz | Protokol | Neden |
|---|---|---|---|
| **API Gateway** | İstek yönlendirme, rate limiting, kimlik doğrulama | REST | Tüm dış trafiğin güvenli şekilde yönlendirilmesi |
| **Order Service** | `OrderCompleted` event | Async (Pub/Sub) | Sipariş istatistiklerinin toplanması |
| **DevOps** | Docker registry, CI/CD pipeline, izleme | Altyapı | Servisin deploy ve monitor edilmesi |

### Diğer Servisler Bizden Ne Bekliyor?

| Servis | Ne Bekliyorlar | Protokol | Amaç |
|---|---|---|---|
| **Order Service** | Ürün fiyat & stok doğrulaması | gRPC (sync) | Sipariş oluşturmadan önce sepet validasyonu |
| **Order Service** | `RestaurantStatusChanged`, `ProductStockChanged` eventleri | Async (Pub/Sub) | Kapalı restorana sipariş engelleme, stoksuz ürün kontrolü |
| **Mobil Uygulama** | Restoran listesi, menü verisi, durum güncellemeleri | REST + Event | Müşteri arayüzünün güncel tutulması |
| **Frontend** | Restoran CRUD, menü yönetimi, anlık durum | REST + Event | Admin paneli & müşteri web arayüzü |
| **Order Service** | Minimum sipariş tutarı, teslimat ücreti bilgisi | gRPC (veya REST) | (Order Service tarafından) Sepet alt limitinin doğrulanması ve nihai tutarın hesaplanması |
| **API Gateway** | Servis sağlık kontrolü, endpoint kaydı | REST | Routing tablosunun oluşturulması |

---

## Hızlı Başlangıç

### Gereksinimler

- Docker & Docker Compose

### Çalıştırma

```bash
docker compose up --build
```

### Servisler

| Servis | URL |
|---|---|
| Restaurant API | http://localhost:5001 |
| Swagger UI | http://localhost:5001/swagger |
| PostgreSQL | localhost:5432 |
| pgAdmin | http://localhost:5051 |

### pgAdmin Kimlik Bilgileri

| Alan | Değer |
|---|---|
| E-posta | admin@restaurant.com |
| Şifre | admin123 |

### Veritabanı Bağlantısı (pgAdmin üzerinden)

| Alan | Değer |
|---|---|
| Host | postgres |
| Port | 5432 |
| Veritabanı | restaurantdb |
| Kullanıcı Adı | postgres |
| Şifre | postgres123 |

---

## Proje Yapısı

```
restaurant-service/
├── Dockerfile
├── docker-compose.yml
├── README.md
└── RestaurantService.API/
    ├── Controllers/
    │   ├── RestaurantsController.cs    # Restoran CRUD + Arama
    │   └── MenuController.cs          # Menü, Kategori & Ürün yönetimi
    ├── DTOs/
    │   └── Dtos.cs                    # İstek / Yanıt modelleri
    ├── Entities/
    │   ├── Restaurant.cs              # Restoran varlığı
    │   ├── MenuCategory.cs            # Kategori varlığı
    │   ├── Product.cs                 # Ürün varlığı
    │   └── RestaurantStatus.cs        # Enum: Open, Closed, Busy
    ├── Data/
    │   └── AppDbContext.cs            # EF Core DbContext + Fluent yapılandırma
    ├── Services/
    │   ├── IRestaurantService.cs      # Arayüz
    │   ├── RestaurantServiceImpl.cs   # Uygulama
    │   ├── IMenuService.cs            # Arayüz
    │   └── MenuServiceImpl.cs         # Uygulama
    ├── Migrations/                    # EF Core migration dosyaları
    └── Program.cs                     # Uygulama giriş noktası + DI yapılandırma
```


