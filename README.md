# MonitoringSystem

공장 설비의 센서 데이터를 실시간으로 수집·저장·시각화하는 모니터링 시스템입니다.

## 아키텍처

```
Simulator → Kafka(sensor-topic) → Backend → SQL Server
                                          ↓ SignalR
                                       Frontend
```

| 프로젝트 | 역할 | 기술 |
|---------|------|------|
| `MonitoringSystem.Backend` | REST API + 실시간 브로드캐스트 | ASP.NET Core, EF Core, SignalR, Kafka |
| `MonitoringSystem.Frontend` | 웹 UI | Blazor Server |
| `MonitoringSystem.Simulator` | 가상 센서 데이터 발행 | Console App, Kafka |
| `MonitoringSystem.Shared` | 공통 모델 | Class Library |

## 사전 준비

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (`localhost:1433`, DB: `MonitoringDB`)
- Apache Kafka (`localhost:9092`, 토픽: `sensor-topic`)

> Docker로 빠르게 띄우는 경우 Kafka + Zookeeper, SQL Server 컨테이너를 사전에 실행해두세요.

## 실행 방법

**전체 실행 (PowerShell):**
```powershell
.\run-all.ps1
```

**개별 실행:**
```bash
dotnet run --project MonitoringSystem.Backend
dotnet run --project MonitoringSystem.Frontend
dotnet run --project MonitoringSystem.Simulator
```

**DB 마이그레이션** (Backend 첫 실행 시 자동 적용됨):
```bash
dotnet ef migrations add <MigrationName> --project MonitoringSystem.Backend
dotnet ef database update --project MonitoringSystem.Backend
```

## 환경 설정

DB 비밀번호 등 민감 정보는 `appsettings.Development.json`에 작성합니다 (`.gitignore`로 제외됨).

`MonitoringSystem.Backend/appsettings.Development.json` 예시:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MonitoringDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True"
  }
}
```

## 주요 설정값

| 항목 | 위치 | 기본값 |
|------|------|--------|
| Kafka bootstrap | 각 `appsettings.json` | `localhost:9092` |
| Kafka 토픽 | 각 `appsettings.json` | `sensor-topic` |
| Kafka Consumer GroupId | Backend `appsettings.json` | `monitoring-backend-group` |
| Backend URL (Frontend에서) | Frontend `appsettings.json` | `https://localhost:7280` |

## 주요 기능

- **실시간 대시보드** — SignalR을 통해 센서 데이터 실시간 수신 및 표시
- **이력 조회** — 페이지네이션 기반 센서 데이터 이력 검색
- **시스템 상태** — DB·Kafka·SignalR 헬스 체크 (`GET /health`)
- **알림** — 이상 감지 시 `IsAlert` 플래그 및 `AlertCode` 기록