"""빌드/실행 태스크 러너. 사용: python tasks.py <task>"""
import subprocess
import sys
import os

VENV_PYTHON = os.path.join(".venv", "Scripts" if sys.platform == "win32" else "bin", "python")
PYTHON = VENV_PYTHON if os.path.exists(VENV_PYTHON) else sys.executable

TASKS = {
    "build": [PYTHON, "-m", "PyInstaller", "stream_server.spec", "--clean"],
    "run":   [PYTHON, "stream_server.py"],
}

def main():
    task = sys.argv[1] if len(sys.argv) > 1 else ""
    if task not in TASKS:
        print(f"사용법: python tasks.py <task>")
        print(f"태스크: {', '.join(TASKS)}")
        sys.exit(1)
    sys.exit(subprocess.run(TASKS[task]).returncode)

if __name__ == "__main__":
    main()
