# HomeStream

[![GitHub release](https://img.shields.io/github/v/release/wooni-dev/homestream)](https://github.com/wooni-dev/homestream/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

같은 네트워크에 연결된 휴대폰으로, PC(또는 연결된 USB)의 영상 폴더를 브라우저에서 열어보고 재생하는 앱입니다.

- **Wi-Fi 공유기** — PC와 휴대폰을 같은 공유기에 연결
- **휴대폰 핫스팟** — 휴대폰 핫스팟을 켜고 PC를 그 핫스팟에 연결

> 카페 등 공용 와이파이는 보안을 위해 기기 간 통신을 막아둔 경우가 많아 QR 스캔 후 접속이 안 될 수 있습니다.
> 이때는 휴대폰 핫스팟으로 전환하면 바로 해결됩니다.

---

## 다운로드

**[홈페이지에서 다운로드](https://gethomestream.com/)** (Windows 단일 실행파일)

또는 [GitHub Releases](https://github.com/wooni-dev/homestream/releases/latest)에서 Windows(win-x64) / macOS(Apple Silicon·Intel) 실행파일을 받을 수 있습니다.

---

## 특징

- **모바일 전용 UI** — 폴더 탐색 + 다크 테마 영상 플레이어
- **탐색(seek)·일시정지 지원** — HTTP Range 요청 처리로 모바일에서 정상 동작
- **GUI 설정** — 영상 폴더를 앱 안에서 직접 변경
- **QR 토큰 인증** — 최초 QR 스캔 시 1회만 토큰으로 인증하고, 이후엔 세션 쿠키(7일 유지)로 접속 유지
- **다국어 데스크톱 앱** — OS 언어가 한국어면 한국어, 그 외에는 영어로 표시 (폰 브라우저 화면은 한국어 고정)
- **단일 실행파일** — .NET 런타임 없이 더블클릭 실행 (self-contained)
- **읽기 전용** — 파일을 수정·삭제·이동하지 않음
- **외부 라이브러리** — Windows는 .NET BCL만 사용, macOS는 GUI를 위해 Avalonia 사용

---

## 지원 영상 형식

`.mp4` `.mkv` `.avi` `.mov` `.wmv` `.m4v`

---

## 요구사항

- PC와 휴대폰이 같은 네트워크에 연결 (Wi-Fi 공유기 또는 휴대폰 핫스팟 — 위 참고)
- Windows x64 또는 macOS (Apple Silicon/Intel)
- 실행파일 사용 시 .NET 런타임 불필요 (self-contained)

---

## 빠른 시작

1. PC에서 앱 실행 후 폴더 선택
2. QR 코드 생성
3. 휴대폰 카메라로 QR 코드 스캔
4. 스캔한 QR 코드의 URL 주소가 나오는지 확인
5. 확인된 URL을 터치해서 브라우저로 접속 완료

> 창을 닫으면 서버가 꺼집니다. 켜둔 동안만 접속됩니다(최소화는 OK).

---

## 설정

최초 실행 시 폴더 선택 창에서 영상 폴더를 지정합니다. 이후에는 QR 창의 **폴더 변경** 버튼으로 바꿀 수 있습니다. 환경변수로 미리 지정할 수도 있습니다.

| 환경변수 | 설명 |
|------|------|
| `SERVE_DIR` | 스트리밍할 영상 폴더 경로 (유효한 경로일 때만 폴더 선택 창 생략) |

---

## 소스에서 개발 실행

프로젝트는 `HomeStream.sln` 아래 3개로 나뉘어 있습니다: `Core`(서버·QR 로직), `Windows`(WinForms GUI), `Mac`(Avalonia GUI).

### Windows (PowerShell)

```powershell
# 기본 실행 — 폴더 선택 창이 먼저 뜸
dotnet run --project Windows/HomeStream.Windows.csproj

# 폴더를 미리 지정해서 바로 스트리밍
$env:SERVE_DIR = "C:\Videos"; dotnet run --project Windows/HomeStream.Windows.csproj
```

빌드 후 exe 직접 실행:

```powershell
dotnet build
.\Windows\bin\Debug\net8.0-windows\HomeStream.exe

# SERVE_DIR 지정 시
$env:SERVE_DIR = "C:\Videos"; .\Windows\bin\Debug\net8.0-windows\HomeStream.exe
```

### Windows (Git Bash / bash)

```bash
# 기본 실행
dotnet run --project Windows/HomeStream.Windows.csproj

# SERVE_DIR 지정 시
SERVE_DIR="C:/Videos" dotnet run --project Windows/HomeStream.Windows.csproj
```

### macOS

```bash
# 기본 실행 — 폴더 선택 창이 먼저 뜸
dotnet run --project Mac/HomeStream.Mac.csproj

# 폴더를 미리 지정해서 바로 스트리밍
SERVE_DIR="/Users/me/Videos" dotnet run --project Mac/HomeStream.Mac.csproj
```

---

## 배포용 빌드

```powershell
# Windows
dotnet publish Windows/HomeStream.Windows.csproj -r win-x64 --self-contained -p:PublishSingleFile=true -p:Configuration=Release

# macOS (Apple Silicon)
dotnet publish Mac/HomeStream.Mac.csproj -r osx-arm64 --self-contained -p:PublishSingleFile=true -p:Configuration=Release

# macOS (Intel)
dotnet publish Mac/HomeStream.Mac.csproj -r osx-x64 --self-contained -p:PublishSingleFile=true -p:Configuration=Release
```

---

## 라이선스

All Rights Reserved — 저작권자의 사전 서면 허가 없이 소스 코드의 복제·수정·배포·재사용을 금지합니다. 자세한 내용은 `LICENSE` 파일을 참고하세요.
