"""빌드/실행/배포 태스크 러너. 사용: python tasks.py <task> [args]"""
import subprocess
import sys
import os
import shutil

VENV_PYTHON = os.path.join(".venv", "Scripts" if sys.platform == "win32" else "bin", "python")
PYTHON = VENV_PYTHON if os.path.exists(VENV_PYTHON) else sys.executable

EXE_SRC = os.path.join("dist", "stream_server.exe")
EXE_DST = os.path.join("dist", "HomeStream.exe")


def task_build():
    subprocess.run([PYTHON, "-m", "PyInstaller", "stream_server.spec", "--clean"], check=True)
    if os.path.exists(EXE_SRC):
        shutil.move(EXE_SRC, EXE_DST)


def task_run():
    subprocess.run([PYTHON, "stream_server.py"])


def task_release(version):
    try:
        import boto3
    except ImportError:
        print("오류: boto3가 필요합니다. pip install boto3")
        sys.exit(1)

    for key in ["R2_ACCESS_KEY_ID", "R2_SECRET_ACCESS_KEY", "R2_ACCOUNT_ID", "R2_BUCKET"]:
        if not os.environ.get(key):
            print(f"오류: 환경변수 {key} 가 설정되지 않았습니다.")
            sys.exit(1)

    task_build()

    account_id = os.environ["R2_ACCOUNT_ID"]
    bucket = os.environ["R2_BUCKET"]
    endpoint = f"https://{account_id}.r2.cloudflarestorage.com"

    s3 = boto3.client(
        "s3",
        endpoint_url=endpoint,
        aws_access_key_id=os.environ["R2_ACCESS_KEY_ID"],
        aws_secret_access_key=os.environ["R2_SECRET_ACCESS_KEY"],
        region_name="auto",
    )

    for key in [f"releases/{version}/windows/HomeStream.exe", "releases/latest/windows/HomeStream.exe"]:
        print(f"업로드 중: {key}")
        s3.upload_file(EXE_DST, bucket, key)
        print(f"완료: {key}")


def main():
    task = sys.argv[1] if len(sys.argv) > 1 else ""

    if task == "build":
        task_build()
    elif task == "run":
        task_run()
    elif task == "release":
        if len(sys.argv) < 3:
            print("사용법: python tasks.py release <version>")
            print("예시:   python tasks.py release v1.3.0")
            sys.exit(1)
        task_release(sys.argv[2])
    else:
        print("사용법: python tasks.py <task>")
        print("태스크: build, run, release <version>")
        sys.exit(1)


if __name__ == "__main__":
    main()
