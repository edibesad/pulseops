# PulseOps - Distributed Incident Management System

PulseOps, modern mikroservis mimarisi ve event-driven tasarımı öğrenmek ve uygulamak amacıyla geliştirilmiş bir .NET 8 projesidir.

## 🚀 Proje Hakkında
Bu proje, sistem kesintilerini (Incident) takip eden ve bu olayları asenkron olarak işleyip SLA (Service Level Agreement) hesaplamaları ve faturalandırma yapan dağıtık bir sistemdir.

### Öne Çıkan Özellikler
*   **Microservices Architecture:** ApiGateway, EventWorker, BillingEngine, SlaEngine gibi ayrıştırılmış servisler.
*   **Reliable Messaging:** RabbitMQ kullanarak "At-least-once delivery" garantisi, manuel ACK/NACK yönetimi, Dead Letter Queue (DLX) ve Retry mekanizmaları.
*   **Infrastructure as Code:** Local geliştirme için Docker Compose ile RabbitMQ ve PostgreSQL kurulumu.
*   **CI/CD Simulation:** Local GitLab ve GitLab Runner kullanılarak oluşturulan Build -> Test -> Dockerize pipeline'ı.
*   **Clean Code:** SOLID prensiplerine uygun, sorumlulukların ayrıldığı (SoC) katmanlı mimari.

## 🛠 Teknoloji Yığını
*   **.NET 8** (ASP.NET Core Web API, Worker Service)
*   **RabbitMQ** (Event Bus)
*   **PostgreSQL** (Veritabanı)
*   **Docker & Docker Compose**
*   **GitLab CI/CD** (Pipeline Otomasyonu)
*   **xUnit & Moq** (Unit Testler)

## 🏗 Kurulum ve Çalıştırma

### Gereksinimler
*   Docker Desktop
*   .NET 8 SDK

### 1. Altyapıyı Ayağa Kaldırın
Veritabanı ve RabbitMQ servisini başlatın:
```powershell
docker-compose up -d
```

### 2. (Opsiyonel) CI/CD Ortamını Simüle Edin
Local GitLab sunucusunu başlatmak için:
```powershell
docker-compose -f infra/gitlab-compose.yml up -d
```

## 🧪 Mimari Kararlar
*   **Worker Refactoring:** RabbitMQ bağlantı ve topoloji tanımları `Infrastructure` katmanına soyutlanarak, Worker servisi sadece iş mantığına odaklanacak şekilde tasarlandı.
*   **Async/Sync:** Tüm I/O işlemleri asenkron yapıya dönüştürülerek "Sync-over-Async" hataları giderildi.

## 👨‍💻 İletişim
Proje hakkında sorularınız veya önerileriniz için iletişime geçebilirsiniz.
