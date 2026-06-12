# 📺 homestream

같은 Wi-Fi에 연결된 **휴대폰에서 PC/USB 안의 영상을 브라우저로 재생**하는 로컬 스트리밍 서버입니다.
파이썬 **표준 라이브러리만** 사용하며, 영상을 외부로 업로드하지 않고 내 네트워크 안에서만 전송합니다.

---

## ✨ 특징

- **모바일 전용 UI** — 폴더 탐색 + 다크 테마 영상 플레이어
- **탐색(seek)·일시정지 지원** — HTTP Range 요청 처리로 모바일에서 정상 동작
- **GUI 설정** — 영상 폴더·계정을 앱 안에서 직접 변경 (`homestream.cfg`에 저장)
- **비밀번호 보호** — 선택적 Basic 인증, 비워두면 인증 없이 접속
- **다국어 GUI** — OS 언어가 한국어면 한국어, 그 외에는 영어로 표시
- **크로스플랫폼** — Windows / macOS / Linux 빌드 지원
- **단일 실행파일** — Python 없이 더블클릭 실행 (PyInstaller)
- **읽기 전용** — 파일을 수정·삭제·이동하지 않음
- **의존성 0** — 런타임은 파이썬 표준 라이브러리만 사용

---

## 🧩 요구사항

- **Python 3.9 이상** (권장: 3.13)
- PC와 휴대폰이 **같은 Wi-Fi**에 연결

---

## 🚀 빠른 시작

```bash
# 1) 가상환경 생성 (최초 1회)
python -m venv .venv

# 2) 실행
python tasks.py run
```

처음 실행하면 **영상 폴더 선택 다이얼로그**가 뜨고, 이후 접속 주소가 적힌 창이 표시됩니다.
폴더·계정은 앱 안에서 언제든 변경할 수 있습니다.

휴대폰 브라우저에 표시된 주소를 입력하면 폴더·영상 목록이 보이고, 영상을 누르면 재생됩니다.

> 창을 닫으면 서버가 꺼집니다. 켜둔 동안만 접속됩니다(최소화는 OK).

---

## ⚙️ 설정

설정은 두 가지 방법으로 할 수 있습니다.

### GUI에서 변경 (권장)

앱 실행 후 **폴더 변경** / **계정 설정** 버튼으로 변경하면 `homestream.cfg`에 자동 저장됩니다.

### 파일로 직접 변경

`homestream.cfg.example`을 `homestream.cfg`로 복사 후 수정:

```ini
SERVE_DIR=D:\Videos
AUTH_USER=admin
AUTH_PASS=
```

| 키 | 설명 |
|------|------|
| `SERVE_DIR` | 스트리밍할 영상 폴더 경로 |
| `AUTH_USER` | 접속 아이디 (기본 `admin`) |
| `AUTH_PASS` | 접속 비밀번호. 비우면 인증 비활성화 |

> 설정 우선순위: **환경변수 > `homestream.cfg`**

---

## 📦 실행파일로 만들기

```bash
# PyInstaller 설치 (최초 1회)
python -m pip install pyinstaller

# 빌드
python tasks.py build
```

→ `dist/stream_server.exe` (Windows) / `dist/HomeStream.app` (macOS) / `dist/stream_server` (Linux) 생성.

각 OS에서 실행한 결과물은 해당 OS에서만 동작합니다.

> ⚠️ 비밀번호가 설정돼 있으면 `.env`에 포함되어 빌드됩니다. 빌드한 파일을 **남에게 공유하지 마세요.**

---

## 🔒 보안 / 네트워크 범위

- 기본은 **같은 Wi-Fi 안에서만** 접속 가능하며, 인터넷 외부에는 노출되지 않습니다(공유기 NAT + 방화벽).
- 비밀번호(Basic 인증)는 **HTTPS가 아니라** 평문(base64)으로 전송됩니다.
  → 외부에서 쓰려면 **Tailscale / WireGuard / SSH 터널** 같은 암호화 통로 안에서 사용하세요.
- 카페·회사 등 **모르는 사람과 같은 공용 Wi-Fi**에서는 비밀번호를 꼭 설정하세요.

---

## 📁 구성 파일

| 파일 | 용도 |
|------|------|
| `stream_server.py` | 스트리밍 서버 + GUI (메인) |
| `stream_server.spec` | PyInstaller 빌드 설정 |
| `tasks.py` | 빌드·실행 태스크 러너 (`python tasks.py build/run`) |
| `homestream.cfg` / `homestream.cfg.example` | 사용자 런타임 설정 (GUI 저장값) |

---

## 🛠 문제 해결

| 증상 | 원인 / 해결 |
|------|------------|
| 폰에서 페이지가 안 열림 | 같은 Wi-Fi인지 확인. 게스트 와이파이는 기기 간 차단될 수 있음 |
| 영상만 재생 안 됨 | 코덱 문제일 수 있음 (대부분 mp4는 정상) |
| 처음 실행 시 접속 불가 | Windows 방화벽 팝업에서 "개인 네트워크 허용" 클릭 |
| 재빌드 시 `PermissionError` | 이전 exe가 실행 중 → 창을 닫고 다시 빌드 |
