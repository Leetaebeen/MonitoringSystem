# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

공장 설비의 센서 데이터를 실시간으로 수집·저장·시각화하는 모니터링 시스템.

```
Simulator → Kafka(sensor-topic) → Backend → SQL Server
                                          ↓ SignalR
                                       Frontend
```

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

**외부 인프라 (Docker 등으로 로컬 실행 필요):**
- SQL Server: `localhost:1433` / DB: `MonitoringDB` / SA 비밀번호: `SuperStrong!Pass1234`
- Kafka: `localhost:9092` / 토픽: `sensor-topic`

**DB 마이그레이션** (Backend 첫 실행 시 자동 적용됨):
```bash
dotnet ef migrations add <MigrationName> --project MonitoringSystem.Backend
dotnet ef database update --project MonitoringSystem.Backend
```

## 아키텍처

### Backend (`MonitoringSystem.Backend`)
- **`KafkaConsumerBackgroundService`**: `sensor-topic`을 구독해 메시지 수신 → EF Core로 DB 저장 → SignalR로 브로드캐스트. `EnableAutoCommit = false`이므로 저장 성공 후 수동 commit.
- **`MonitoringRealtimePublisher`**: `IHubContext<MonitoringHub>`를 통해 모든 클라이언트에 `"ReceiveSensorData"` 이벤트 전송.
- **`MonitoringHub`**: 빈 허브 클래스. 클라이언트 → 서버 방향 메서드 없음.
- **`MonitoringController`**: REST API 3개 — `GET /api/monitoring/latest`, `GET /api/monitoring/history` (페이지네이션), `GET /api/monitoring/alerts`.
- **`GET /health`**: DB·Kafka·SignalR 상태를 JSON으로 반환.

### Frontend (`MonitoringSystem.Frontend`)
Blazor Server (Interactive Server Render Mode).
- **`MonitoringApiClient`**: Named HttpClient `"BackendApi"`를 사용해 REST API 호출.
- **`MonitoringHubClient`**: SignalR 연결 관리, `ReceiveSensorData` 이벤트 수신.
- **페이지 3개**: `LiveDashboard.razor` (실시간), `History.razor` (이력 조회), `SystemStatus.razor` (헬스 체크).
- Backend URL: `appsettings.json`의 `BackendApi:BaseUrl` (기본값 `https://localhost:7280`).

### Shared (`MonitoringSystem.Shared`)
Backend·Frontend·Simulator가 공통으로 참조하는 모델 라이브러리.
- **`SensorData`**: 핵심 엔티티. `PlantId`, `LineId`, `EquipmentId`, `EquipmentType`, `Temperature`, `PressureBar`, `VibrationMmS`, `Rpm`, `Status`, `IsAlert`, `AlertCode`, `LogTime`.
- **`HistoryQueryResult`**: 페이지네이션 결과 래퍼.

### Simulator (`MonitoringSystem.Simulator`)
콘솔 앱. `SensorDataGenerator`로 가짜 센서 데이터 생성 후 `KafkaSensorProducer`로 발행.
- 설정은 `appsettings.json`의 `Simulator` 섹션에서 제어 (인터벌, PlantId 목록, 설비 목록 등).
- 기본 인터벌: 5초.

## 주요 설정값

| 항목 | 위치 | 기본값 |
|------|------|--------|
| Kafka bootstrap | 각 `appsettings.json` | `localhost:9092` |
| Kafka 토픽 | 각 `appsettings.json` | `sensor-topic` |
| Kafka Consumer GroupId | Backend `appsettings.json` | `monitoring-backend-group` |
| DB 연결 | Backend `appsettings.json` | `DefaultConnection` |
| Backend URL (Frontend에서) | Frontend `appsettings.json` | `https://localhost:7280` |
