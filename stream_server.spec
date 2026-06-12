# -*- mode: python ; coding: utf-8 -*-
import os
import sys

# Platform-specific icon
if sys.platform == 'win32':
    _icon = 'tv.ico'
elif sys.platform == 'darwin':
    _icon = 'tv.icns' if os.path.exists('tv.icns') else None
else:
    _icon = None  # Linux: EXE에 아이콘 임베딩 미지원

# 플랫폼에 존재하는 아이콘만 번들에 포함
_datas = [('.env', '.'), ('homestream.cfg.example', '.')]
if sys.platform == 'win32' and os.path.exists('tv.ico'):
    _datas.append(('tv.ico', '.'))
elif sys.platform == 'darwin' and os.path.exists('tv.icns'):
    _datas.append(('tv.icns', '.'))

a = Analysis(
    ['stream_server.py'],
    pathex=[],
    binaries=[],
    datas=_datas,
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='stream_server',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=_icon,
)

# macOS: exe를 .app 번들로 감쌈
if sys.platform == 'darwin':
    app = BUNDLE(
        exe,
        name='HomeStream.app',
        icon=_icon,
        bundle_identifier='com.homestream.app',
    )
