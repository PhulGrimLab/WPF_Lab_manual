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

### 추가 기록(스케줄러 적용 사례 정리)
- 질문: WPF 기반 앱에서 별도 Task 스케줄러가 실제로 필요한 사례 요청.
- 정리한 대표 유형:
  - 산업 설비 모니터링(HMI/SCADA): 100ms~1s 주기 폴링, Heartbeat, 재시도/복구.
  - 트레이딩/시세 모니터링: 초단위 갱신, 세션 기준 작업, 실패 복구.
  - 키오스크/오프라인 업무앱: 백그라운드 동기화, 업로드 재시도, 야간 배치.
  - 디지털 사이니지/플레이어: 시간표 기반 콘텐츠 전환, 사전 캐시.
  - 백업/에이전트형 데스크톱 앱: 증분 백업, 상태 점검, 네트워크 복구 후 재실행.
- 판단 기준: 1초 이하 주기, Pause/Resume/Retry, UI 상태 연동, 오프라인 내결함성이 중요하면 앱 내부 스케줄러가 유리.
### 추가 기록(스냅샷 상태 조회 부하 검토)
- 질문: GetTaskStatuses()를 UI에서 자주 호출하면 부하가 큰지 검토.
- 판단:
  - 현재 구현은 호출마다 상태 리스트/레코드를 새로 생성하므로 호출 빈도가 너무 높으면 할당/GC 비용이 증가.
  - _sync 락을 사용하므로 UI 폴링이 과도하면 스케줄러 실행 루프와 락 경합 가능.
  - 다만 Task 수가 적고(예: 수~수십 개) 갱신 주기를 200~500ms 정도로 두면 일반적으로 부담이 크지 않음.
- 권장:
  - UI 갱신 주기 기본 250ms~500ms(모니터링 화면만 100~200ms)로 스로틀.
  - 변경 없으면 그리드 리렌더 생략(변경분 반영 방식).
  - 고빈도 실시간이 필요하면 폴링 대신 이벤트/채널 기반 푸시 모델 고려.

## 2026-05-26

### Nullable 경고 수정
- 경고: `Lib.Common/ViewModelBase.cs`의 `ViewModelBase.PropertyChanged` 이벤트 null 허용 여부가 `INotifyPropertyChanged.PropertyChanged`와 일치하지 않아 CS8612 발생.
- 수정:
  - `PropertyChanged` 이벤트 타입을 `PropertyChangedEventHandler?`로 변경.
  - `[CallerMemberName]` 기본값으로 `null`을 사용하는 `OnPropertyChanged`, `SetProperty`의 인자 타입을 `string?`로 변경.
- 검증:
  - `dotnet build .\Lib.Common\Lib.Common.csproj`
  - 결과: 경고 0개, 오류 0개.
