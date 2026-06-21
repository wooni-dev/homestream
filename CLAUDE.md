# HomeStream

같은 Wi-Fi의 휴대폰에서 PC/USB 영상을 브라우저로 스트리밍하는 로컬 HTTP 서버 + Windows 데스크탑 앱.

## 현재 상태

Python(PyInstaller) → **C# (.NET)으로 재작성 중**.
기존 Python 소스: `stream_server.py` (참조용으로 유지)

## 기술 스팩

- **언어**: C# (.NET 8+)
- **외부 라이브러리**: 없음 — .NET BCL만 사용
- **GUI**: WinForms (`System.Windows.Forms`)
- **HTTP 서버**: `System.Net.HttpListener`
- **QR 코드**: 직접 구현 (외부 라이브러리 금지)
- **빌드 결과물**: 단일 실행파일 (`HomeStream.exe`)

## 빌드 명령

```
dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true -p:Configuration=Release
```

## 성공 기준

1. `dotnet build` 오류 없이 성공
2. `HomeStream.exe` 실행 시 WinForms 창 열림
3. QR 코드가 창에 렌더링됨
4. HTTP 서버가 지정 포트에서 응답함 (기본 8000)
5. 브라우저에서 폴더 목록 및 영상 재생 동작

## 구현해야 할 기능

### HTTP 서버
- `HttpListener`로 멀티스레드 HTTP 서버
- 포트 충돌 시 8000~8009 자동 순환
- HTTP Range 요청 처리 (206 Partial Content) — 모바일 seek 지원
- LAN IP 자동 감지

### 인증
- 프로세스 시작 시 `RandomNumberGenerator`로 토큰 생성 (32자 hex)
- QR 코드에 `http://{ip}:{port}/?auth={token}` 삽입
- `?auth=` 파라미터 검증 → 세션 쿠키(`hs_sid`) 발급 → 302 리다이렉트
- 세션 쿠키 유효기간 7일 (`time` 기반 만료)
- 미인증 요청 → 403

### 웹 UI (인라인 HTML, 외부 리소스 없음)
- 다크 테마 (배경 `#0e0e12`, 텍스트 `#ececf1`)
- 폴더 목록 페이지: 디렉터리/영상 분리, breadcrumb 네비게이션
- 영상 플레이어 페이지: `<video>` 태그, 뒤로가기 버튼
- 지원 확장자: `.mp4 .mkv .avi .mov .wmv .m4v`
- 파일 크기 표시 (MB/GB)

### WinForms GUI
- 다크 배경 (`#0e0e12`)
- QR 코드 캔버스 (흰 모듈 `#ececf1`, 여백 없음)
- "주소 복사" 버튼 (클릭 시 1.5초간 "복사됨!" 표시)
- 현재 서비스 폴더 경로 표시 (42자 초과 시 `.../{parent}/{name}` 축약)
- "폴더 변경" 버튼 → `FolderBrowserDialog` → 서버 재시작
- 창 닫기 시 서버 종료
- 폴더 선택 다이얼로그 (초기 실행 시 또는 환경변수 `SERVE_DIR` 없을 때)

### QR 코드 직접 구현
- URL을 byte 모드로 인코딩
- Reed-Solomon 오류 정정
- QR 매트릭스 생성 → WinForms Canvas에 렌더링

### 로컬라이제이션
- 시스템 로케일 감지 → 한국어/영어 자동 전환
- `CultureInfo.CurrentCulture`로 판별

### 설정
- 환경변수 `SERVE_DIR` → 폴더 선택 다이얼로그 건너뜀
