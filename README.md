# PulseOps — Distributed Incident Management System

> **TR:** Modern mikroservis mimarisi ve event-driven tasarımı öğrenmek amacıyla geliştirilmiş .NET 8 projesi.
> **EN:** A .NET 8 project built to learn and practice modern microservice architecture and event-driven design.

---

## Table of Contents / İçindekiler

- [Overview / Genel Bakış](#overview--genel-bakış)
- [Architecture / Mimari](#architecture--mimari)
- [Services / Servisler](#services--servisler)
- [Tech Stack / Teknoloji Yığını](#tech-stack--teknoloji-yığını)
- [Getting Started / Başlarken](#getting-started--başlarken)
- [CI/CD Pipeline](#cicd-pipeline)
- [Testing / Testler](#testing--testler)
- [Architectural Decisions / Mimari Kararlar](#architectural-decisions--mimari-kararlar)

---

## Overview / Genel Bakış

**EN:** PulseOps tracks system incidents asynchronously. When a failure is detected, an event is published via RabbitMQ, consumed by a background worker, persisted to PostgreSQL, and (in future stages) processed by SLA and billing engines.

**TR:** PulseOps, sistem kesintilerini (Incident) asenkron olarak takip eder. Bir arıza tespit edildiğinde olay RabbitMQ üzerinden yayınlanır, bir arka plan işçisi tarafından tüketilir, PostgreSQL'e kaydedilir ve (ilerleyen aşamalarda) SLA ile faturalama motorları tarafından işlenir.

---

## Architecture / Mimari

```
┌─────────────────────────────────────────────────────────┐
│                        Client                           │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTP POST
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    ApiGateway                           │
│         POST /api/events/incident-started               │
│         POST /api/events/incident-ended                 │
└─────────────────────┬───────────────────────────────────┘
                      │ Publish (events.raw)
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    RabbitMQ                             │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ events.raw  │  │events.retry  │  │  events.dlq   │  │
│  │   (queue)   │  │   .5s (TTL) │  │  (poison msg) │  │
│  └──────┬──────┘  └──────────────┘  └───────────────┘  │
└─────────┼───────────────────────────────────────────────┘
          │ Consume
          ▼
┌─────────────────────────────────────────────────────────┐
│                   EventWorker                           │
│     Validate → Persist → ACK / NACK (retry/dlq)        │
└─────────────────────┬───────────────────────────────────┘
                      │ EF Core
                      ▼
┌─────────────────────────────────────────────────────────┐
│                   PostgreSQL                            │
│      IncidentEvents table  |  Incidents table           │
└─────────────────────────────────────────────────────────┘

  [ SlaEngine ]  — planned / planlanan
  [ BillingEngine ] — planned / planlanan
```

---

## Services / Servisler

### ApiGateway
**EN:** REST entry point. Receives incident events from clients and publishes them as JSON messages to the `events.raw` RabbitMQ queue. Each message carries an `event-type` header for downstream routing.

**TR:** REST giriş noktası. İstemcilerden gelen olayları alır ve JSON mesaj olarak RabbitMQ'daki `events.raw` kuyruğuna yayınlar. Her mesaj, yönlendirme için `event-type` başlığı taşır.

**Endpoints:**
| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/events/incident-started` | Report a new system failure / Yeni arıza bildir |
| `POST` | `/api/events/incident-ended` | Report incident resolution / Arıza çözümünü bildir |

---

### EventWorker
**EN:** Background worker service. Consumes messages from `events.raw`, validates and persists them to PostgreSQL. Implements a full reliability pattern: at-least-once delivery, manual ACK/NACK, retry queue with 5-second TTL, and a Dead Letter Queue for unrecoverable failures.

**TR:** Arka plan işçi servisi. `events.raw` kuyruğundan mesaj tüketir, doğrular ve PostgreSQL'e kaydeder. En az bir kez iletim, manuel ACK/NACK, 5 saniyelik TTL'li yeniden deneme kuyruğu ve kurtarılamaz hatalar için Dead Letter Queue içeren tam güvenilirlik modelini uygular.

**Message Reliability / Mesaj Güvenilirliği:**
```
Message received
    │
    ├─► Success → ACK
    │
    ├─► Transient error (DB/network) + retry < 3 → NACK → events.retry.5s → re-queue
    │
    └─► Validation error or retry >= 3 → NACK → events.dlq
```

---

### SlaEngine *(planned / planlanan)*
**EN:** Will monitor incident durations against configured SLA thresholds and publish `SlaViolated` events when breached.

**TR:** Konfigüre edilen SLA eşiklerine göre olay sürelerini izleyecek ve ihlal durumunda `SlaViolated` olayı yayınlayacak.

---

### BillingEngine *(planned / planlanan)*
**EN:** Will calculate incident-related costs based on duration and SLA violations.

**TR:** Süre ve SLA ihlallerine göre olayla ilgili maliyetleri hesaplayacak.

---

### Shared
**EN:** Class library containing shared event contracts used across services.

**TR:** Servisler arasında paylaşılan olay sözleşmelerini içeren sınıf kütüphanesi.

| Contract | Fields |
|----------|--------|
| `IncidentStarted` | IncidentKey, Source, ResourceId, StartTime, Region, CorrelationId |
| `IncidentEnded` | IncidentKey, Source, ResourceId, EndTime, Region, CorrelationId |
| `SlaViolated` | IncidentKey, ServiceName, Duration, RuleName, DetectedAt, CorrelationId |

---

## Tech Stack / Teknoloji Yığını

| Layer | Technology |
|-------|------------|
| Runtime | .NET 8 (ASP.NET Core Web API + Worker Service) |
| Message Broker | RabbitMQ 3 |
| Database | PostgreSQL 15 |
| ORM | Entity Framework Core 8 + Npgsql |
| Containerization | Docker & Docker Compose |
| CI/CD | GitLab CI/CD (local simulation) |
| Testing | xUnit + Moq |
| API Docs | Swagger / OpenAPI (Swashbuckle) |

---

## Getting Started / Başlarken

### Prerequisites / Gereksinimler
- Docker Desktop
- .NET 8 SDK

### 1. Start Infrastructure / Altyapıyı Başlatın

```bash
# From project root / Proje kökünden
docker-compose -f infra/docker-compose.yml up -d
```

This starts / Bu başlatır:
- **RabbitMQ** — `localhost:5672` (AMQP) | `localhost:15672` (Management UI — pulseops/pulseops)
- **PostgreSQL** — `localhost:5432` (pulseops/pulseops)

### 2. Run the Services / Servisleri Çalıştırın

```bash
# ApiGateway
dotnet run --project src/ApiGateway

# EventWorker (in a separate terminal / ayrı terminalde)
dotnet run --project src/EventWorker
```

### 3. Test the API / API'yi Test Edin

```bash
# Report a new incident / Yeni olay bildir
curl -X POST http://localhost:5000/api/events/incident-started \
  -H "Content-Type: application/json" \
  -d '{
    "incidentKey": "INC-001",
    "source": "monitoring",
    "resourceId": "server-01",
    "startTime": "2026-04-05T10:00:00Z",
    "region": "eu-west-1",
    "correlationId": "abc-123"
  }'
```

Swagger UI is available at / Swagger arayüzü: `http://localhost:5000/swagger`

### 4. (Optional) CI/CD Simulation / CI/CD Simülasyonu

```bash
# Start local GitLab / Yerel GitLab'ı başlat
docker-compose -f infra/gitlab-compose.yml up -d
```

---

## CI/CD Pipeline

The `.gitlab-ci.yml` defines three stages / `.gitlab-ci.yml` üç aşama tanımlar:

```
Build → Test → Package
```

| Stage | Action |
|-------|--------|
| **Build** | `dotnet restore` + `dotnet build` |
| **Test** | `dotnet test src/PulseOps.sln` |
| **Package** | `docker build` for ApiGateway image |

---

## Testing / Testler

Unit tests are in `src/ApiGateway.Tests/` using xUnit and Moq.

**TR:** Birim testler `src/ApiGateway.Tests/` dizininde xUnit ve Moq kullanılarak yazılmıştır.

```bash
dotnet test src/PulseOps.sln
```

Current test coverage / Mevcut test kapsamı:
- `EventsController` — verifies HTTP 202 response and single publish call

---

## Architectural Decisions / Mimari Kararlar

**EN:**
- **Infrastructure Abstraction:** RabbitMQ connection and topology setup (`RabbitMqTopology`) is isolated in the `Infrastructure` layer, keeping `Worker` focused solely on business logic.
- **Async I/O:** All I/O operations are fully asynchronous, eliminating sync-over-async anti-patterns.
- **Retry Strategy:** Transient failures (network, database) trigger up to 3 retries via a dedicated `events.retry.5s` queue using RabbitMQ TTL, avoiding busy-wait loops.
- **Dead Letter Queue:** Validation errors and exhausted retries are routed to `events.dlq` for manual inspection without data loss.
- **Shared Contracts:** Event schemas live in the `Shared` library, ensuring type-safe communication between producers and consumers without coupling their implementations.

**TR:**
- **Altyapı Soyutlaması:** RabbitMQ bağlantı ve topoloji kurulumu (`RabbitMqTopology`) `Infrastructure` katmanına izole edilmiş, `Worker` yalnızca iş mantığına odaklanmaktadır.
- **Asenkron I/O:** Tüm I/O işlemleri tam asenkron yapıya alınarak sync-over-async anti-pattern'ı giderilmiştir.
- **Yeniden Deneme Stratejisi:** Geçici hatalar (ağ, veritabanı) RabbitMQ TTL kullanan `events.retry.5s` kuyruğu üzerinden en fazla 3 kez yeniden denenir; meşgul bekleme döngüsü yoktur.
- **Dead Letter Queue:** Doğrulama hataları ve tükenen yeniden denemeler, veri kaybı olmadan manuel inceleme için `events.dlq` kuyruğuna yönlendirilir.
- **Paylaşılan Sözleşmeler:** Olay şemaları `Shared` kütüphanesinde tutularak üretici ve tüketiciler arasında tip güvenli iletişim, implementasyonları birbirine bağlamadan sağlanır.

---

## Contact / İletişim

**EN:** Feel free to open an issue for questions or suggestions.
**TR:** Sorularınız veya önerileriniz için issue açabilirsiniz.
![](https://tracker.edibesad.com/track?ref=PulseOps)
