# 📈 StockOrderBook

`StockOrderBook`은 실시간 주식 호가 데이터를 시뮬레이션하고 시각화하는 시스템으로,

- **Client**는 WPF (.NET 8) 기반의 데스크톱 애플리케이션이고,
- **Server**는 ASP.NET Core (.NET 8) 기반의 SignalR 서버로 구성되어 있습니다.

**전체 구조도**
![image](https://github.com/user-attachments/assets/ee681b74-692f-4b68-b499-cee6b977f639)

---

## Client (WPF, .NET 8)

### 📌 개요

- 사용자에게 다중 종목의 실시간 호가 패널을 제공
- 체결 내역 실시간 표시 (최대 100개 유지)
- SignalR을 통한 서버와의 WebSocket 통신
- MVVM 패턴 기반
- DI 컨테이너를 직접 구성하여 ViewModel과 Service 분리

### 📐 아키텍처 다이어그램

- MVVM 기반이며, Server 측과 상호작용하는 부분을 IRealtimeConnectionService 라는 인터페이스를 구현한 SignalRClientService 클래스를 설계
  ![image](https://github.com/user-attachments/assets/494c41b5-303b-45ca-8930-7efb2bb5f3db)

### 📁 폴더 구조

```
Client/
├── Views/                  # WPF XAML 화면 구성
│   └── MainWindow.xaml
│   └── OrderBookPanelView.xaml
├── ViewModels/             # MVVM ViewModels
│   └── MainViewModel.cs    # Server 측 Simulator의 Depth 정보 이벤트 구독
│   └── OrderBookPanelViewModel.cs # Server측 Simulator의 Print 정보 이벤트 구독 및 매수/매도 주문
├── Services/               # Server 측 Realtime데이터 수신 서비스 및 인터페이스
│   └── IRealtimeConnectionService.cs  # 특정 실시간 데이터 처리 기술에 종속되지 않기 위한 인터페이스
│   └── SignalRClientService.cs        # SignalR Client으로, 실시간 데이터 Subscribe/Unsubscribe 및 매수/매도 주문 기능 Invoke
├── Models/                 # 데이터 모델
│   └── DepthEntry.cs
│   └── PrintEntry.cs
├── Converters/             # UI 값 처리 및 변환기
│   └── UtcToKSTConverter.cs   # 타임존 변경 (UST -> KST)
├── App.xaml.cs             # DI 컨테이너 구성 및 진입점
└── Client.csproj           # WPF 프로젝트 설정
```

## Server (ASP.NET Core SignalR, .NET 8)

### 📌 개요

- SignalR 기반의 실시간 통신 서버
- 시뮬레이션된 Depth, Print 데이터를 클라이언트에 전송
- 각 패널(panelId), 연결(connectionId)을 기준으로 구독 관리
- 다수의 클라이언트/패널이 동일한 종목을 구독해도 하나의 시뮬레이터 인스턴스로 처리

### 📐 아키텍처 다이어그램

![image](https://github.com/user-attachments/assets/d4c874a3-4bea-4af4-95e4-6b5f40cfe960)

### 📁 폴더 구조

```
Server/
├── Hubs/
│   └── OrderBookHub.cs # SignalR Hub 정의
├── Services/
│   └── OrderBookSimulator.cs # 호가/체결 데이터 시뮬레이터 및 매수/매도 주문 요청 시, 즉시 체결하여 File에 저장 및 Client에 체결 데이터 전송
│   └── IOrderBookSimulator.cs
│   └── ITradeStorage.cs      # Trade 데이터 저장 서비스의 인터페이스
│   └── FileTradeStorage.cs   # Trade 데이터를 File 형태로 저장하는 서비스
│   └── TradeHistoryPersistenceService.cs  # ITradeStorage 와 IOrderBookSimulator 을 참조하여, Simulator로 부터 제공받은 Trade 데이터를 백그라운드에서 저장하는 작업을 하는 서비스
├── Models/
│   └── DepthEntry.cs # 호가 데이터 모델
│   └── PrintEntry.cs # 체결 데이터 모델 (Client 출력용)
│   └── Trade.cs # 체결 데이터 모델 (저장 용 Entity)
├── Program.cs
```
