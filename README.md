# 📺 HomeStream

같은 Wi-Fi에 연결된 **휴대폰에서 PC/USB 안의 영상을 브라우저로 재생**하는 로컬 스트리밍 서버입니다.
영상을 외부로 업로드하지 않고 내 네트워크 안에서만 전송합니다.

[![GitHub release](https://img.shields.io/github/v/release/wooni-dev/homestream)](https://github.com/wooni-dev/homestream/releases/latest)
[![Python](https://img.shields.io/badge/Python-3.9%2B-blue)](https://www.python.org/)

---

## ⬇️ 다운로드

**[홈페이지에서 다운로드](https://homestream-web.vercel.app/)** (Windows 단일 실행파일)

또는 [GitHub Releases](https://github.com/wooni-dev/homestream/releases/latest)에서 직접 받을 수 있습니다.

---

## 📸 미리보기

![HomeStream 미리보기](tv_preview.png)

---

## ✨ 특징

- **모바일 전용 UI** — 폴더 탐색 + 다크 테마 영상 플레이어
- **탐색(seek)·일시정지 지원** — HTTP Range 요청 처리로 모바일에서 정상 동작
- **GUI 설정** — 영상 폴더를 앱 안에서 직접 변경
- **QR 토큰 인증** — QR 스캔으로 접속, 자격증명이 네트워크를 타지 않음
- **다국어 GUI** — OS 언어가 한국어면 한국어, 그 외에는 영어로 표시
- **크로스플랫폼** — Windows / macOS / Linux 빌드 지원
- **단일 실행파일** — Python 없이 더블클릭 실행 (PyInstaller)
- **읽기 전용** — 파일을 수정·삭제·이동하지 않음
- **최소 의존성** — 런타임 의존성: `qrcode` 단 1개

---

## 🎬 지원 영상 형식

`.mp4` `.mkv` `.avi` `.mov` `.wmv` `.m4v`

---

## 🧩 요구사항

- PC와 휴대폰이 **같은 Wi-Fi**에 연결
- 실행파일 사용 시 Python 불필요
- 소스에서 직접 실행/빌드 시 **Python 3.9 이상** (권장: 3.13)

---

## 🚀 빠른 시작

### 실행파일 (일반 사용자)

1. [홈페이지](https://homestream-web.vercel.app/)에서 `stream_server.exe` 다운로드
2. 더블클릭으로 실행
3. **영상 폴더**를 선택하면 접속 주소(예: `192.168.x.x:8000`)가 표시됨
4. 폰 브라우저에 주소 입력 → 폴더·영상 목록에서 재생

> 창을 닫으면 서버가 꺼집니다. 켜둔 동안만 접속됩니다(최소화는 OK).

### 소스에서 실행 (개발)

```bash
# 1) 가상환경 생성 (최초 1회)
python -m venv .venv

# 2) 실행
python tasks.py run
```

처음 실행하면 **영상 폴더 선택 다이얼로그**가 뜨고, 이후 접속 QR 코드가 적힌 창이 표시됩니다.
폴더는 앱의 **폴더 변경** 버튼으로 바꿀 수 있습니다.

---

## ⚙️ 설정

영상 폴더는 앱 실행 시 다이얼로그에서 선택합니다. 환경변수로도 지정할 수 있습니다.

| 환경변수 | 설명 |
|------|------|
| `SERVE_DIR` | 스트리밍할 영상 폴더 경로 (설정 시 다이얼로그 생략) |

---

## 📦 실행파일 빌드

```bash
# PyInstaller 설치 (최초 1회)
python -m pip install pyinstaller

# 빌드
python tasks.py build
```

→ `dist/stream_server.exe` (Windows) / `dist/HomeStream.app` (macOS) / `dist/stream_server` (Linux) 생성.

각 OS에서 실행한 결과물은 해당 OS에서만 동작합니다.

> 빌드 후 `dist/` 폴더에는 exe 파일만 있어야 합니다.

---

## 🚢 GitHub Releases 배포

### 사전 준비

[GitHub CLI](https://cli.github.com/) 설치 후 로그인:

```bash
winget install GitHub.cli   # Windows
gh auth login
```

### 기존 릴리즈 exe 교체

```bash
# 1) 클린 빌드
python tasks.py build

# 2) 기존 asset 삭제 후 새 exe 업로드
gh release delete-asset v1.0.0 stream_server.exe --yes
gh release upload v1.0.0 dist\stream_server.exe
```

### 새 버전 릴리즈

```bash
# 태그 생성 및 릴리즈 발행
git tag v1.x.x
git push origin v1.x.x
gh release create v1.x.x dist\stream_server.exe --title "HomeStream v1.x.x" --notes "변경 내용"
```

---

## 🌐 웹사이트 (`homestream-web`)

릴리즈 다운로드 페이지는 별도 레포 [`wooni-dev/homestream-web`](https://github.com/wooni-dev/homestream-web)으로 관리되며 Vercel에 배포됩니다.

- **URL**: https://homestream-web.vercel.app/
- **배포**: `homestream-web` 레포에 push하면 Vercel이 자동 배포

### GitHub Releases API 인증

웹사이트는 이 레포의 Releases에서 파일 크기 등을 읽어오기 위해 GitHub API를 사용합니다.
Private 레포이므로 Vercel에 환경변수 설정이 필요합니다.

| 변수 | 값 |
|------|------|
| `GITHUB_TOKEN` | Fine-grained PAT (Contents · Metadata Read-only) |

토큰 발급: GitHub → Settings → Developer settings → Fine-grained tokens
→ 대상 레포: `wooni-dev/homestream`, 권한: Contents · Metadata Read-only

Vercel 설정: homestream-web 프로젝트 → Settings → Environment Variables → `GITHUB_TOKEN` 추가 (Sensitive)

---

## 🔒 보안 / 네트워크 범위

- 기본은 **같은 Wi-Fi 안에서만** 접속 가능하며, 인터넷 외부에는 노출되지 않습니다(공유기 NAT + 방화벽).
- 기본 포트: **8000** (방화벽에서 허용 필요 — Windows는 첫 실행 시 팝업에서 허용)
- **QR 토큰 인증** — QR 코드 스캔 없이는 접속 불가. 자격증명이 네트워크를 타지 않아 HTTP 평문 전송 문제 없음.
- 외부에서 접속이 필요하면 **Tailscale / WireGuard / SSH 터널** 같은 암호화 통로 안에서 사용하세요.

---

## 📁 구성 파일

| 파일 | 용도 |
|------|------|
| `stream_server.py` | 스트리밍 서버 + GUI (메인) |
| `stream_server.spec` | PyInstaller 빌드 설정 |
| `tasks.py` | 빌드·실행 태스크 러너 (`python tasks.py build/run`) |

---

## 🛠 문제 해결

| 증상 | 원인 / 해결 |
|------|------------|
| 폰에서 페이지가 안 열림 | 같은 Wi-Fi인지 확인. 게스트 와이파이는 기기 간 차단될 수 있음 |
| 영상만 재생 안 됨 | 코덱 문제일 수 있음 (대부분 mp4는 정상) |
| 처음 실행 시 접속 불가 | Windows 방화벽 팝업에서 "개인 네트워크 허용" 클릭 |
| 재빌드 시 `PermissionError` | 이전 exe가 실행 중 → 창을 닫고 다시 빌드 |
| 포트 8000이 이미 사용 중 | 다른 앱이 8000번 포트 사용 중 → 해당 앱 종료 후 재실행 |
