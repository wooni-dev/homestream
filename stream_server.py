"""같은 Wi-Fi의 휴대폰에서 USB 폴더의 영상을 스트리밍하기 위한 HTTP 서버.

- 커스텀 모바일 UI: 폴더 탐색 + 전용 영상 플레이어 (다크 테마)
- HTTP Range(구간 요청) 지원: 영상 재생/탐색(seek)이 모바일에서 정상 동작
- 멀티스레드: 동시 연결 처리
- 표준 라이브러리만 사용 (읽기 전용 — 파일 수정/삭제 없음)
"""

import base64
import hmac
import html
import http.server
import io
import os
import socket
import sys
import threading
from functools import partial
from urllib.parse import quote, unquote, urlsplit, parse_qs

def load_env(path):
    """간단한 .env 로더: KEY=VALUE 형식, '#' 주석·빈 줄 무시. 표준 라이브러리만 사용.

    실제 환경 변수가 이미 있으면 그것을 우선한다(.env 는 기본값 역할).
    """
    try:
        with open(path, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith("#") or "=" not in line:
                    continue
                key, _, val = line.partition("=")
                os.environ.setdefault(key.strip(), val.strip().strip('"').strip("'"))
    except FileNotFoundError:
        pass


if getattr(sys, "frozen", False):
    # PyInstaller exe: 빌드 시 함께 넣은 .env 를 exe 내부 임시 폴더에서 읽는다.
    _BASE_DIR = sys._MEIPASS
else:
    _BASE_DIR = os.path.dirname(os.path.abspath(__file__))
load_env(os.path.join(_BASE_DIR, ".env"))

# ===== 설정 =====
HOST = "0.0.0.0"
PORT = 8000
SERVE_DIR = os.environ.get("SERVE_DIR", r"E:\PUA\리비도\러스트스쿨\리비도 훈련 프로그램")
VIDEO_EXTS = {".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v"}

# 접속 비밀번호. AUTH_PASS 가 비어 있으면 인증 없이 동작(같은 Wi-Fi 전용).
AUTH_USER = os.environ.get("AUTH_USER", "admin")
AUTH_PASS = os.environ.get("AUTH_PASS", "")


def human_size(n):
    if n >= 1024 ** 3:
        return f"{n / 1024 ** 3:.1f} GB"
    return f"{n / 1024 ** 2:.0f} MB"


PAGE_CSS = """
* { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
body { background:#0e0e12; color:#ececf1; font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif; padding-bottom:40px; }
header { position:sticky; top:0; background:rgba(14,14,18,.92); backdrop-filter:blur(8px);
  padding:16px 18px 12px; border-bottom:1px solid #23232c; z-index:10; }
header h1 { font-size:17px; font-weight:700; color:#fff; }
header .sub { font-size:12px; color:#7d7d8c; margin-top:3px; }
.crumb { font-size:13px; color:#8a8aff; margin-top:10px; line-height:1.6; word-break:break-all; }
.crumb a { color:#8a8aff; text-decoration:none; }
.list { padding:8px 12px; }
.item { display:flex; align-items:center; gap:13px; padding:14px 14px; margin:7px 0;
  background:#191922; border:1px solid #23232c; border-radius:14px; text-decoration:none;
  color:#ececf1; transition:background .12s; }
.item:active { background:#22222e; }
.ic { width:42px; height:42px; flex:none; border-radius:11px; display:flex; align-items:center;
  justify-content:center; font-size:20px; }
.ic.folder { background:#2a2540; }
.ic.video { background:#3a1f2a; }
.meta { flex:1; min-width:0; }
.name { font-size:15px; font-weight:500; line-height:1.35; word-break:break-all; }
.sz { font-size:12px; color:#7d7d8c; margin-top:3px; }
.chev { color:#55556a; font-size:18px; flex:none; }
.empty { text-align:center; color:#666; padding:50px 0; font-size:14px; }
.pbody { background:#000; display:flex; flex-direction:column; min-height:100vh; }
.ptop { padding:14px 16px; display:flex; align-items:center; gap:12px; background:#0e0e12; }
.back { color:#8a8aff; text-decoration:none; font-size:15px; font-weight:600; flex:none; }
.ptitle { font-size:14px; color:#ddd; line-height:1.35; word-break:break-all; }
.pwrap { flex:1; display:flex; align-items:center; justify-content:center; background:#000; }
video { width:100%; max-height:100%; background:#000; }
"""


def render_listing(disp_path, dirs, vids):
    parts = [p for p in disp_path.split("/") if p]
    cum = ""
    crumb = '<a href="/">HOME</a>'
    for p in parts:
        cum += "/" + quote(p)
        crumb += f' <span style="color:#444">/</span> <a href="{cum}/">{html.escape(p)}</a>'

    rows = []
    for name in dirs:
        link = quote(name) + "/"
        rows.append(
            f'<a class="item" href="{link}">'
            f'<div class="ic folder">[D]</div>'
            f'<div class="meta"><div class="name">{html.escape(name)}</div></div>'
            f'<div class="chev">&rsaquo;</div></a>'
        )
    for name, size in vids:
        link = quote(name) + "?play"
        rows.append(
            f'<a class="item" href="{link}">'
            f'<div class="ic video">&#9654;</div>'
            f'<div class="meta"><div class="name">{html.escape(name)}</div>'
            f'<div class="sz">{human_size(size)}</div></div>'
            f'<div class="chev">&rsaquo;</div></a>'
        )
    body = "".join(rows) if rows else '<div class="empty">이 폴더에 영상이 없습니다</div>'
    title = parts[-1] if parts else "리비도 훈련 프로그램"
    count = f"폴더 {len(dirs)} · 영상 {len(vids)}"

    return f"""<!DOCTYPE html><html lang="ko"><head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>{html.escape(title)}</title><style>{PAGE_CSS}</style></head><body>
<header><h1>{html.escape(title)}</h1><div class="sub">{count}</div>
<div class="crumb">{crumb}</div></header>
<div class="list">{body}</div></body></html>"""


def render_player(url_path):
    name = unquote(url_path.rstrip("/").split("/")[-1])
    return f"""<!DOCTYPE html><html lang="ko"><head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>{html.escape(name)}</title><style>{PAGE_CSS}</style></head><body class="pbody">
<div class="ptop"><a class="back" href="javascript:history.back()">&lsaquo; 뒤로</a>
<div class="ptitle">{html.escape(name)}</div></div>
<div class="pwrap">
<video controls autoplay playsinline preload="metadata" src="{url_path}"></video>
</div></body></html>"""


class StreamHandler(http.server.SimpleHTTPRequestHandler):
    """Range 지원 + 커스텀 UI 핸들러."""

    def _check_auth(self):
        """AUTH_PASS 가 설정돼 있으면 Basic 인증을 요구한다. 통과 시 True."""
        if not AUTH_PASS:
            return True
        expected = "Basic " + base64.b64encode(
            f"{AUTH_USER}:{AUTH_PASS}".encode("utf-8")
        ).decode("ascii")
        given = self.headers.get("Authorization", "")
        if hmac.compare_digest(given, expected):
            return True
        self.send_response(401)
        self.send_header("WWW-Authenticate", 'Basic realm="video", charset="UTF-8"')
        self.send_header("Content-Length", "0")
        self.end_headers()
        return False

    def do_GET(self):
        if not self._check_auth():
            return
        parsed = urlsplit(self.path)
        if "play" in parse_qs(parsed.query):
            fpath = self.translate_path(self.path)
            if os.path.isfile(fpath) and os.path.splitext(fpath)[1].lower() in VIDEO_EXTS:
                self._send_html(render_player(parsed.path))
                return
        super().do_GET()

    def list_directory(self, path):
        try:
            names = os.listdir(path)
        except OSError:
            self.send_error(404, "디렉터리를 열 수 없습니다")
            return None
        dirs, vids = [], []
        for n in names:
            full = os.path.join(path, n)
            if os.path.isdir(full):
                dirs.append(n)
            elif os.path.splitext(n)[1].lower() in VIDEO_EXTS:
                vids.append((n, os.path.getsize(full)))
        dirs.sort()
        vids.sort()
        disp = unquote(urlsplit(self.path).path)
        page = render_listing(disp, dirs, vids)
        return self._send_html(page, return_file=True)

    def _send_html(self, page, return_file=False):
        data = page.encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        if return_file:
            return io.BytesIO(data)
        self.wfile.write(data)
        return None

    def send_head(self):
        self._range_remaining = None
        path = self.translate_path(self.path)
        range_header = self.headers.get("Range")
        if os.path.isdir(path) or range_header is None:
            return super().send_head()
        try:
            f = open(path, "rb")
        except OSError:
            self.send_error(404, "File not found")
            return None
        size = os.fstat(f.fileno()).st_size
        try:
            _unit, _, rng = range_header.partition("=")
            start_s, _, end_s = rng.partition("-")
            start = int(start_s) if start_s else 0
            end = int(end_s) if end_s else size - 1
        except ValueError:
            f.close()
            return super().send_head()
        if start >= size:
            self.send_error(416, "Requested Range Not Satisfiable")
            f.close()
            return None
        end = min(end, size - 1)
        length = end - start + 1
        self.send_response(206)
        self.send_header("Content-Type", self.guess_type(path))
        self.send_header("Accept-Ranges", "bytes")
        self.send_header("Content-Range", f"bytes {start}-{end}/{size}")
        self.send_header("Content-Length", str(length))
        self.end_headers()
        f.seek(start)
        self._range_remaining = length
        return f

    def copyfile(self, source, outputfile):
        remaining = self._range_remaining
        if remaining is None:
            super().copyfile(source, outputfile)
            return
        bufsize = 64 * 1024
        while remaining > 0:
            chunk = source.read(min(bufsize, remaining))
            if not chunk:
                break
            try:
                outputfile.write(chunk)
            except (BrokenPipeError, ConnectionResetError):
                break
            remaining -= len(chunk)

    def log_message(self, *args):
        pass


def get_lan_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("8.8.8.8", 80))
        return s.getsockname()[0]
    except OSError:
        return "127.0.0.1"
    finally:
        s.close()


def run_gui(server, ip):
    """접속 주소만 보여주는 작은 창. 창을 X(닫기)로 닫으면 서버를 멈춘다."""
    import tkinter as tk

    def stop():
        server.shutdown()      # serve_forever 루프 종료(별도 스레드에서 동작 중)
        root.destroy()

    root = tk.Tk()
    root.title("USB 영상 스트리밍")
    root.configure(bg="#0e0e12")
    root.resizable(False, False)

    tk.Label(root, text="폰에서 아래 주소로 접속하세요", bg="#0e0e12", fg="#9a9aae",
             font=("Segoe UI", 10)).pack(padx=30, pady=(22, 8))

    # 주소: 읽기전용 입력칸이라 마우스로 선택·복사 가능
    url = tk.Entry(root, justify="center", relief="flat", state="normal",
                   readonlybackground="#191922", fg="#8a8aff", disabledforeground="#8a8aff",
                   font=("Consolas", 15, "bold"), width=24)
    url.insert(0, f"http://{ip}:{PORT}/")
    url.configure(state="readonly")
    url.pack(padx=30, pady=4, ipady=8)

    tk.Label(root, text="이 창을 X(닫기)로 닫으면 서버가 꺼집니다", bg="#0e0e12",
             fg="#55556a", font=("Segoe UI", 8)).pack(pady=(8, 22))

    root.protocol("WM_DELETE_WINDOW", stop)   # 창 X(닫기)로 서버 종료
    root.mainloop()


def main():
    if not os.path.isdir(SERVE_DIR):
        import tkinter as tk
        from tkinter import messagebox
        r = tk.Tk()
        r.withdraw()
        messagebox.showerror("오류", f"영상 폴더를 찾을 수 없습니다:\n{SERVE_DIR}")
        r.destroy()
        return
    handler = partial(StreamHandler, directory=SERVE_DIR)
    server = http.server.ThreadingHTTPServer((HOST, PORT), handler)
    ip = get_lan_ip()
    threading.Thread(target=server.serve_forever, daemon=True).start()
    run_gui(server, ip)
    server.server_close()


if __name__ == "__main__":
    main()
