# 진행상황 기록

## 2026-05-25

### 요청 배경
- `Wpf.Lib.Scheduler` 라이브러리에서 Task 스케줄러 구현 진행 중.
- 테스트용으로 `1초 주기 Task`, `100ms 주기 Task` 등록 및 실행 예시 필요.

### 현재 코드베이스 확인 결과
- `Wpf.Lib.Scheduler/ISchedulerTask.cs`
  - `Name`, `IsEnabled`, `Period`, `ExecuteAsync(...)` 인터페이스 정의 완료.
- `Wpf.Lib.Scheduler/SchedulerContext.cs`
  - 실행 시각(`Now`) 전달용 컨텍스트 정의 완료.
- `Wpf.Lib.Scheduler/SchedulerService.cs`
  - 현재는 생성자만 있는 초기 상태(실행 로직 미구현).
- `Wpf.Console.SchedulerTest/Program.cs`
  - 현재는 `SchedulerService` 생성 후 Hello World 출력만 수행.

### 대화에서 제안된 구현 방향(아직 파일 반영 전)
- SchedulerService 기능 확장 예시 제시:
  - Task 등록/시작/정지
  - `PauseAll` / `ResumeAll`
  - Task 단위 `PauseTask` / `ResumeTask`
  - 실패 재시도(`MaxRetryCount`, `RetryDelay`)
  - 실행시간 및 드리프트 출력(기준 tick 보정)
- 테스트 Task 예시 제시:
  - `Every1SecondTask` (1초 주기)
  - `Every100MsTask` (100ms 주기, 주기적 예외로 재시도 검증)

### 다음 진행 후보
- 예시 코드를 실제 프로젝트 파일에 반영.
- 콘솔 테스트 프로젝트에서 동작 확인(실행 로그 검증).
- 이후 WPF UI(Start/Pause/Resume/Stop)와 연동.
