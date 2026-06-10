"""특정 USB가 연결되면 자동 감지해 지정 폴더의 영상 파일에 접근한다.

- 자동 감지: 일정 주기로 드라이브 목록을 확인하는 폴링 방식
- USB 식별: 드라이브 문자는 매번 바뀌므로 볼륨 일련번호로 판별
- 영상 수집: 지정 폴더를 재귀 탐색하며 영상 확장자만 모은다
- 표준 라이브러리(ctypes)만 사용 - pip 설치 불필요
"""

import ctypes
import os
import string
import time

# ===== 설정 =====
TARGET_SERIAL = "9C336BBD"                              # 이 USB의 볼륨 일련번호
TARGET_SUBPATH = r"PUA\리비도\러스트스쿨\리비도 훈련 프로그램"  # 읽을 폴더 (USB 루트 기준)
VIDEO_EXTS = {".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v"}  # 영상으로 인정할 확장자
POLL_INTERVAL = 2                                       # 감지 주기(초)

DRIVE_REMOVABLE = 2                                     # GetDriveType: 이동식 드라이브


def get_removable_drives():
    """현재 연결된 이동식(USB) 드라이브 루트 목록을 반환한다."""
    drives = []
    bitmask = ctypes.windll.kernel32.GetLogicalDrives()
    for i, letter in enumerate(string.ascii_uppercase):
        if bitmask & (1 << i):
            root = f"{letter}:\\"
            if ctypes.windll.kernel32.GetDriveTypeW(root) == DRIVE_REMOVABLE:
                drives.append(root)
    return drives


def get_volume_serial(root):
    """드라이브의 볼륨 일련번호를 'XXXXXXXX' 형식 문자열로 반환한다. 실패 시 None."""
    serial = ctypes.c_uint(0)
    name_buf = ctypes.create_unicode_buffer(1024)
    fs_buf = ctypes.create_unicode_buffer(1024)
    ok = ctypes.windll.kernel32.GetVolumeInformationW(
        ctypes.c_wchar_p(root), name_buf, ctypes.sizeof(name_buf),
        ctypes.byref(serial), None, None, fs_buf, ctypes.sizeof(fs_buf),
    )
    return f"{serial.value:08X}" if ok else None


def find_target_drive():
    """일련번호가 일치하는 USB의 루트 경로를 반환한다. 없으면 None."""
    for root in get_removable_drives():
        if get_volume_serial(root) == TARGET_SERIAL.upper():
            return root
    return None


def collect_videos(folder):
    """폴더를 재귀 탐색하며 영상 파일의 전체 경로 목록을 반환한다."""
    videos = []
    for cur_dir, _dirs, files in os.walk(folder):
        for name in files:
            if os.path.splitext(name)[1].lower() in VIDEO_EXTS:
                videos.append(os.path.join(cur_dir, name))
    return sorted(videos)


def process_videos(videos):
    """수집한 영상 목록을 처리한다. 여기에 실제 로직을 넣으세요."""
    total_mb = sum(os.path.getsize(p) for p in videos) / (1024 * 1024)
    print(f"  영상 {len(videos)}개, 합계 {total_mb:,.1f} MB")
    for p in videos:
        print(f"  [영상] {p}")
        # 예) 재생: os.startfile(p)
        # 예) 복사: shutil.copy2(p, dest_dir)


def main():
    print(f"USB 일련번호 '{TARGET_SERIAL}' 감지 대기 중... (Ctrl+C 종료)")
    connected = False
    while True:
        root = find_target_drive()
        if root and not connected:
            connected = True
            target = os.path.join(root, TARGET_SUBPATH)
            print(f"[감지] {root} 연결됨 → {target}")
            if os.path.isdir(target):
                process_videos(collect_videos(target))
            else:
                print(f"  [경고] 폴더 없음: {target}")
        elif not root and connected:
            connected = False
            print("[분리] USB 제거됨")
        time.sleep(POLL_INTERVAL)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n종료")
