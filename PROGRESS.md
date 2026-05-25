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

### 실제 반영 사항
- `Wpf.Lib.Scheduler/SchedulerService.cs`
  - Task 런타임 상태 스냅샷 모델 `SchedulerTaskStatus` 추가.
  - 내부 상태 저장소(`_taskStates`) 추가.
  - `Register(...)` 시 상태 초기화 추가.
  - `RunLoopAsync(...)`에서 실행 시작/완료/실패 상태 갱신 로직 추가.
  - 외부 조회용 `GetTaskStatuses()` API 추가.
- `Wpf.Console.SchedulerTest/Program.cs`
  - 스케줄러 실행 중 1초 주기로 `GetTaskStatuses()`를 호출해 콘솔에 현재 상태 출력하도록 변경.
  - 출력 항목: 실행중 여부, 실행 수, 오류 수, 마지막 실행 결과, 마지막 오류.

### 동작 검증 결과
- 실행 명령: `dotnet run --project .\\Wpf.Console.SchedulerTest\\Wpf.Console.SchedulerTest.csproj`
- 결과: 100ms/1초 Task 실행 로그와 함께 1초마다 상태 스냅샷이 정상 출력됨.
- 비고: 기존 코드에 있던 미사용 필드(`_isStarted`, `_isGloballyPaused`) 경고가 출력되나, 이번 기능 동작에는 영향 없음.

### 추가 반영(시간 정보 출력)
- `Wpf.Console.SchedulerTest/Program.cs`
  - 상태 조회 출력에 `마지막시작(LastStartedAt)`, `마지막완료(LastCompletedAt)` 시각을 추가.
  - 시각 포맷은 `HH:mm:ss.fff`, 값이 없으면 `-`로 출력.
- 재검증 결과
  - 콘솔 상태 라인에 시간 정보가 정상 출력되는 것을 확인.
