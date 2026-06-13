"""같은 Wi-Fi의 휴대폰에서 USB 폴더의 영상을 스트리밍하기 위한 HTTP 서버.

- 커스텀 모바일 UI: 폴더 탐색 + 전용 영상 플레이어 (다크 테마)
- HTTP Range(구간 요청) 지원: 영상 재생/탐색(seek)이 모바일에서 정상 동작
- 멀티스레드: 동시 연결 처리
- 표준 라이브러리만 사용 (읽기 전용 — 파일 수정/삭제 없음)
"""

import html
import http.server
import io
import locale
import os
import secrets
import socket
import time
import sys
import threading
from functools import partial
from urllib.parse import quote, unquote, urlsplit, parse_qs

try:
    _lang = (locale.getlocale()[0] or "").lower()
except Exception:
    _lang = ""
_IS_KO = _lang.startswith("ko")

def _s(ko, en):
    return ko if _IS_KO else en

if getattr(sys, "frozen", False):
    _BASE_DIR = sys._MEIPASS
    _USER_CONFIG_DIR = os.path.dirname(sys.executable)
else:
    _BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    _USER_CONFIG_DIR = _BASE_DIR

_USER_CONFIG_FILE = os.path.join(_USER_CONFIG_DIR, "homestream.cfg")

# ===== 설정 =====
HOST = "0.0.0.0"
PORT = 8000
VIDEO_EXTS = {".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v"}

_TOKEN = secrets.token_hex(16)
_SESSIONS: dict = {}  # {sid: expire_at (time.monotonic())}
_COOKIE_NAME = "hs_sid"
_COOKIE_MAX_AGE = 86400 * 7


def _init_user_config():
    """homestream.cfg 없으면 example에서 자동 복사."""
    if not os.path.exists(_USER_CONFIG_FILE):
        example = os.path.join(_BASE_DIR, "homestream.cfg.example")
        if os.path.exists(example):
            import shutil
            shutil.copy2(example, _USER_CONFIG_FILE)


def load_user_config():
    """homestream.cfg에서 설정 읽기. dict 반환."""
    config = {}
    try:
        with open(_USER_CONFIG_FILE, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith("#") or "=" not in line:
                    continue
                key, _, val = line.partition("=")
                config[key.strip()] = val.strip()
    except FileNotFoundError:
        pass
    return config


def save_user_config(serve_dir):
    """설정을 homestream.cfg에 저장."""
    with open(_USER_CONFIG_FILE, "w", encoding="utf-8") as f:
        f.write(f"SERVE_DIR={serve_dir}\n")


def _short_path(path, max_len=42):
    if len(path) <= max_len:
        return path
    parts = path.replace("\\", "/").split("/")
    if len(parts) > 2:
        return ".../" + "/".join(parts[-2:])
    return path


def human_size(n):
    if n >= 1024 ** 3:
        return f"{n / 1024 ** 3:.1f} GB"
    return f"{n / 1024 ** 2:.0f} MB"


def _make_qr_matrix(data):
    import qrcode as _qrcode
    qr = _qrcode.QRCode(border=1)
    qr.add_data(data)
    qr.make(fit=True)
    return qr.get_matrix()


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
    title = parts[-1] if parts else "홈"
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
        now = time.monotonic()

        # 1. 세션 쿠키
        for part in self.headers.get("Cookie", "").split(";"):
            k, _, v = part.strip().partition("=")
            if k == _COOKIE_NAME:
                if _SESSIONS.get(v, 0) > now:
                    return True
                _SESSIONS.pop(v, None)  # 만료된 세션 제거

        # 2. QR 토큰 → 세션 발급 후 리다이렉트
        qs = parse_qs(urlsplit(self.path).query)
        if qs.get("auth", [""])[0] == _TOKEN:
            sid = secrets.token_hex(16)
            _SESSIONS[sid] = now + _COOKIE_MAX_AGE
            dest = urlsplit(self.path).path or "/"
            self.send_response(302)
            self.send_header("Location", dest)
            self.send_header(
                "Set-Cookie",
                f"{_COOKIE_NAME}={sid}; Path=/; Max-Age={_COOKIE_MAX_AGE}; HttpOnly; SameSite=Lax",
            )
            self.send_header("Content-Length", "0")
            self.end_headers()
            return False

        return True

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
            try:
                super().copyfile(source, outputfile)
            except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError):
                pass
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


def _ask_serve_dir(stale_path=None):
    """경로 직접 입력 or 찾아보기 다이얼로그.

    stale_path: 이전에 저장됐지만 지금 없는 경로 (있으면 입력칸에 미리 채워줌).
    확인 시 경로 유효성 검사 → 없으면 에러 메시지 유지, 있으면 경로 반환.
    취소하면 None 반환.
    """
    import tkinter as tk
    from tkinter import filedialog, messagebox

    result = [None]

    dlg = tk.Tk()
    dlg.title(_s("영상 폴더 설정", "Video Folder Settings"))
    dlg.configure(bg="#0e0e12")
    dlg.resizable(False, False)
    try:
        dlg.iconbitmap(os.path.join(_BASE_DIR, "tv.ico"))
    except Exception:
        pass

    if stale_path:
        tk.Label(dlg, text=_s(f"저장된 폴더를 찾을 수 없습니다:\n{stale_path}",
                              f"Saved folder not found:\n{stale_path}"),
                 bg="#0e0e12", fg="#ff6b6b", font=("Segoe UI", 9),
                 wraplength=320, justify="left").pack(padx=24, pady=(20, 4))

    tk.Label(dlg, text=_s("스트리밍할 영상 폴더 경로를 입력하거나 선택하세요",
                           "Enter or browse to the video folder"),
             bg="#0e0e12", fg="#9a9aae", font=("Segoe UI", 9)
             ).pack(padx=24, pady=(16 if not stale_path else 6, 6))

    row = tk.Frame(dlg, bg="#0e0e12")
    row.pack(padx=24, pady=4, fill="x")

    entry = tk.Entry(row, bg="#191922", fg="#ececf1", insertbackground="#8a8aff",
                     relief="flat", font=("Segoe UI", 10), width=38)
    entry.pack(side="left", ipady=7, fill="x", expand=True)
    if stale_path:
        entry.insert(0, stale_path)

    def browse():
        initial = entry.get().strip() or os.path.expanduser("~")
        picked = filedialog.askdirectory(title=_s("폴더 선택", "Select Folder"), initialdir=initial, parent=dlg)
        if picked:
            entry.delete(0, "end")
            entry.insert(0, picked)

    tk.Button(row, text=_s("찾아보기", "Browse"), command=browse,
              bg="#2a2540", fg="#8a8aff", activebackground="#3a3560",
              activeforeground="#a3a3ff", relief="flat", cursor="hand2",
              font=("Segoe UI", 9)).pack(side="left", padx=(6, 0), ipady=7, ipadx=10)

    def confirm():
        path = entry.get().strip()
        if not path:
            messagebox.showwarning(_s("경로 없음", "No Path"),
                                   _s("폴더 경로를 입력하세요.", "Please enter a folder path."), parent=dlg)
            return
        if not os.path.isdir(path):
            messagebox.showerror(_s("폴더 없음", "Folder Not Found"),
                                 _s(f"해당 경로를 찾을 수 없습니다:\n{path}",
                                    f"The folder could not be found:\n{path}"), parent=dlg)
            return
        result[0] = path
        dlg.destroy()

    def cancel():
        dlg.destroy()

    btns = tk.Frame(dlg, bg="#0e0e12")
    btns.pack(padx=24, pady=(14, 24), fill="x")

    tk.Button(btns, text=_s("취소", "Cancel"), command=cancel,
              bg="#23232c", fg="#9a9aae", activebackground="#2a2a36",
              relief="flat", cursor="hand2", font=("Segoe UI", 9)
              ).pack(side="right", padx=(6, 0), ipadx=14, ipady=6)

    tk.Button(btns, text=_s("확인", "OK"), command=confirm,
              bg="#8a8aff", fg="#0e0e12", activebackground="#a3a3ff",
              activeforeground="#0e0e12", relief="flat", cursor="hand2",
              font=("Segoe UI", 10, "bold")
              ).pack(side="right", ipadx=18, ipady=6)

    dlg.bind("<Return>", lambda e: confirm())
    dlg.protocol("WM_DELETE_WINDOW", cancel)
    dlg.mainloop()

    return result[0]



def _start_server(serve_dir):
    """serve_dir 로 ThreadingHTTPServer 를 만들고 백그라운드에서 시작한다.

    PORT 부터 최대 9개 포트를 순서대로 시도해 첫 번째 빈 포트를 사용한다.
    (server, actual_port) 튜플을 반환한다.
    """
    handler = partial(StreamHandler, directory=serve_dir)
    for port in range(PORT, PORT + 10):
        try:
            server = http.server.ThreadingHTTPServer((HOST, port), handler)
            threading.Thread(target=server.serve_forever, daemon=True).start()
            return server, port
        except OSError:
            continue
    raise OSError(_s(
        f"포트 {PORT}~{PORT + 9} 가 모두 사용 중입니다. 다른 앱을 종료 후 다시 실행하세요.",
        f"Ports {PORT}–{PORT + 9} are all in use. Close other apps and try again.",
    ))


def run_gui(state, ip, port):
    """접속 주소와 현재 폴더를 보여주는 창.

    state = {"server": ThreadingHTTPServer, "serve_dir": str}
    창을 X(닫기)로 닫으면 서버를 멈춘다.
    """
    import tkinter as tk
    from tkinter import filedialog

    url_text = f"http://{ip}:{port}/"

    root = tk.Tk()
    root.title(_s("홈 스트리밍", "Home Streaming"))
    root.configure(bg="#0e0e12")
    root.resizable(False, False)
    try:
        root.iconbitmap(os.path.join(_BASE_DIR, "tv.ico"))
    except Exception:
        pass

    def stop():
        state["server"].shutdown()
        root.destroy()

    def change_folder():
        new_dir = filedialog.askdirectory(
            title=_s("스트리밍할 영상 폴더를 선택하세요", "Select video folder to stream"),
            initialdir=state["serve_dir"],
            parent=root,
        )
        if not new_dir or new_dir == state["serve_dir"]:
            return
        # 기존 서버 종료 후 새 폴더로 재시작
        state["server"].shutdown()
        state["server"].server_close()
        save_user_config(new_dir)
        state["serve_dir"] = new_dir
        state["server"], _ = _start_server(new_dir)
        dir_label.configure(text=_short_path(new_dir))

    tk.Label(root, text=_s("QR 스캔 또는 주소로 접속", "Scan QR or enter address"),
             bg="#0e0e12", fg="#9a9aae", font=("Segoe UI", 10)).pack(padx=30, pady=(22, 6))

    qr_matrix = _make_qr_matrix(f"http://{ip}:{port}/?auth={_TOKEN}")
    _n = len(qr_matrix)
    _cell = max(1, 180 // _n)
    _QR_SIZE = _cell * _n
    qr_canvas = tk.Canvas(root, width=_QR_SIZE, height=_QR_SIZE, bg="#0e0e12",
                          highlightthickness=0)
    qr_canvas.pack(padx=30, pady=(0, 10))
    for _ri, _row in enumerate(qr_matrix):
        for _ci, _val in enumerate(_row):
            if _val:
                _x0, _y0 = _ci * _cell, _ri * _cell
                qr_canvas.create_rectangle(_x0, _y0, _x0 + _cell, _y0 + _cell,
                                           fill="#ececf1", outline="")

    def copy_url():
        root.clipboard_clear()
        root.clipboard_append(url_text)
        copy_btn.configure(text=_s("복사됨!", "Copied!"), bg="#3aa76d")
        root.after(1500, lambda: copy_btn.configure(text=_s("주소 복사", "Copy URL"), bg="#8a8aff"))

    copy_btn = tk.Button(root, text=_s("주소 복사", "Copy URL"), command=copy_url,
                         bg="#8a8aff", fg="#0e0e12", activebackground="#a3a3ff",
                         activeforeground="#0e0e12", relief="flat", cursor="hand2",
                         font=("Segoe UI", 10, "bold"))
    copy_btn.pack(padx=30, pady=(10, 4), ipadx=12, ipady=6, fill="x")

    tk.Label(root, text=_s("서비스 중인 폴더", "Serving folder"), bg="#0e0e12", fg="#55556a",
             font=("Segoe UI", 8)).pack(pady=(14, 2))

    dir_label = tk.Label(root, text=_short_path(state["serve_dir"]),
                         bg="#0e0e12", fg="#9a9aae", font=("Segoe UI", 9),
                         wraplength=280)
    dir_label.pack(padx=30, pady=(0, 6))

    change_btn = tk.Button(root, text=_s("폴더 변경", "Change Folder"), command=change_folder,
                           bg="#2a2540", fg="#8a8aff", activebackground="#3a3560",
                           activeforeground="#a3a3ff", relief="flat", cursor="hand2",
                           font=("Segoe UI", 9, "bold"))
    change_btn.pack(padx=30, pady=(0, 4), ipadx=12, ipady=5, fill="x")

    tk.Label(root, text=_s("이 창을 X(닫기)로 닫으면 서버가 꺼집니다",
                           "Closing this window will stop the server"), bg="#0e0e12",
             fg="#55556a", font=("Segoe UI", 8)).pack(pady=(8, 22))

    root.protocol("WM_DELETE_WINDOW", stop)
    root.mainloop()


def main():
    _init_user_config()
    config = load_user_config()

    # SERVE_DIR 우선순위: 환경변수 > homestream.cfg > 입력 다이얼로그
    serve_dir = os.environ.get("SERVE_DIR") or config.get("SERVE_DIR")

    if not serve_dir or not os.path.isdir(serve_dir):
        stale = serve_dir if serve_dir and not os.path.isdir(serve_dir) else None
        serve_dir = _ask_serve_dir(stale_path=stale)
        if not serve_dir:
            return  # 취소 시 종료
        save_user_config(serve_dir)

    server, port = _start_server(serve_dir)
    state = {"serve_dir": serve_dir, "server": server}
    ip = get_lan_ip()
    run_gui(state, ip, port)
    state["server"].server_close()


if __name__ == "__main__":
    main()
