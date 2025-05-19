# 📈 StockOrderBook

`StockOrderBook`은 실시간 주식 호가 데이터를 시뮬레이션하고 시각화하는 시스템으로,  
- **Client**는 WPF (.NET 8) 기반의 데스크톱 애플리케이션이고,
- **Server**는 ASP.NET Core (.NET 8) 기반의 SignalR 서버로 구성되어 있습니다.

---

## Client (WPF, .NET 8)
### 📌 개요

- 사용자에게 다중 종목의 실시간 호가 패널을 제공
- 체결 내역 실시간 표시 (최대 100개 유지)
- SignalR을 통한 서버와의 WebSocket 통신
- MVVM 패턴 기반
- DI 컨테이너를 직접 구성하여 ViewModel과 Service 분리

### 📁 폴더 구조
```
Client/
├── Views/                  # WPF XAML 화면 구성
│   └── MainWindow.xaml
│   └── OrderBookPanelView.xaml
├── ViewModels/             # MVVM ViewModels
│   └── MainViewModel.cs    # Server 측 Simulator의 Depth 정보 이벤트 구독
│   └── OrderBookPanelViewModel.cs # Server측 Simulator의 Print 정보 이벤트 구독
├── Services/               # Server 측 Realtime데이터 수신 서비스 및 인터페이스
│   └── IRealtimeConnectionService.cs  # 특정 실시간 데이터 처리 기술에 종속되지 않기 위한 인터페이스
│   └── SignalRClientService.cs        # SignalR Client
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

### 📁 폴더 구조
```
Server/
├── Hubs/
│   └── OrderBookHub.cs # SignalR Hub 정의
├── Services/
│   └── OrderBookSimulator.cs # 호가/체결 데이터 시뮬레이터
│   └── IOrderBookSimulator.cs
├── Models/
│   └── DepthEntry.cs, PrintEntry.cs # 호가/체결 데이터 모델
├── Program.cs
```
