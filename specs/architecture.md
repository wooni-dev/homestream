# 프로젝트 구조

## 파일 구성

```
HomeStream/
├── HomeStream.csproj
├── Program.cs              # 진입점, 폴더 선택 → 서버 시작 → GUI
├── Server/
│   ├── StreamServer.cs     # HttpListener 기반 HTTP 서버
│   ├── AuthManager.cs      # 토큰/세션 관리
│   └── RangeHandler.cs     # HTTP Range 요청 처리
├── Web/
│   ├── HtmlRenderer.cs     # 폴더 목록 / 영상 플레이어 HTML 생성
│   └── PageCss.cs          # 인라인 CSS 상수
├── Gui/
│   └── MainForm.cs         # WinForms 메인 창 (QR + 버튼)
└── Qr/
    ├── QrEncoder.cs        # 데이터 → QR 비트스트림
    ├── ReedSolomon.cs      # 오류 정정 코드워드 계산
    └── QrMatrix.cs         # 매트릭스 배치 + 마스킹
```

## 네임스페이스

```
HomeStream
HomeStream.Server
HomeStream.Web
HomeStream.Gui
HomeStream.Qr
```

## 실행 흐름

```
Program.Main()
  → SERVE_DIR 환경변수 확인
  → 없으면 FolderSelectDialog 표시
  → StreamServer.Start(serveDir)   // 백그라운드 스레드
  → Application.Run(new MainForm(server, ip, port))
  → 창 닫힘 → server.Stop()
```

## 프로젝트 파일 (HomeStream.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
</Project>
```
