# WinForms GUI

## 색상 상수

```csharp
static readonly Color BgColor       = ColorTranslator.FromHtml("#0e0e12");
static readonly Color TextMuted     = ColorTranslator.FromHtml("#9a9aae");
static readonly Color TextDim       = ColorTranslator.FromHtml("#55556a");
static readonly Color AccentColor   = ColorTranslator.FromHtml("#8a8aff");
static readonly Color AccentDark    = ColorTranslator.FromHtml("#2a2540");
static readonly Color AccentLight   = ColorTranslator.FromHtml("#a3a3ff");
static readonly Color SuccessColor  = ColorTranslator.FromHtml("#3aa76d");
static readonly Color QrModule      = ColorTranslator.FromHtml("#ececf1");
```

## MainForm 레이아웃 (위에서 아래 순서)

```
[Label]  "QR 스캔 후 뜨는 주소를 터치하세요"  색상: TextMuted  패딩: 상22 하6
[Panel]  QR 캔버스  (OnPaint로 직접 렌더링)
[Button] "주소 복사"  bg: AccentColor  fg: BgColor
[Label]  "서비스 중인 폴더"  색상: TextDim  크기: 8pt
[Label]  {폴더 경로 축약}  색상: TextMuted  wraplength 적용
[Button] "폴더 변경"  bg: AccentDark  fg: AccentColor
[Label]  "이 창을 X(닫기)로 닫으면 서버가 꺼집니다"  색상: TextDim  크기: 8pt
```

## QR 캔버스

`Panel`을 상속하거나 `PictureBox` 대신 `Panel.Paint` 이벤트에서 직접 그림.

```csharp
// QR 매트릭스에서 셀 크기 계산
int n = matrix.GetLength(0);
int cell = Math.Max(1, 180 / n);
int size = cell * n;

// Paint 이벤트에서 렌더링
using var brush = new SolidBrush(QrModule);
for (int r = 0; r < n; r++)
    for (int c = 0; c < n; c++)
        if (matrix[r, c])
            e.Graphics.FillRectangle(brush, c * cell, r * cell, cell, cell);
```

패널 배경: `BgColor`. 패딩: 좌우 30px.

## 주소 복사 버튼

클릭 시:
1. `Clipboard.SetText(url)` 실행
2. 버튼 텍스트 → "복사됨!", 배경 → `SuccessColor`
3. 1500ms 후 원래 상태로 복원

```csharp
copyBtn.Text = isKo ? "복사됨!" : "Copied!";
copyBtn.BackColor = SuccessColor;
await Task.Delay(1500);
copyBtn.Text = isKo ? "주소 복사" : "Copy URL";
copyBtn.BackColor = AccentColor;
```

## 폴더 변경 버튼

```csharp
using var dlg = new FolderBrowserDialog();
dlg.InitialDirectory = currentServeDir;
if (dlg.ShowDialog() == DialogResult.OK && dlg.SelectedPath != currentServeDir) {
    server.Stop();
    server.Start(dlg.SelectedPath);
    UpdateFolderLabel(dlg.SelectedPath);
}
```

## 폴더 경로 축약

```csharp
static string ShortPath(string path, int maxLen = 42) {
    if (path.Length <= maxLen) return path;
    var parts = path.Replace('\\', '/').Split('/');
    return parts.Length > 2
        ? ".../" + string.Join("/", parts[^2..])
        : path;
}
```

## 폴더 선택 창 (초기 실행)

환경변수 `SERVE_DIR`이 없거나 유효하지 않을 때, 탐색기 다이얼로그를 바로 띄우지 않고
앱과 같은 다크 테마의 `FolderSelectForm`을 먼저 표시한다. 이 창의 "폴더 선택" 버튼을
눌러야 `FolderBrowserDialog`(탐색기)가 열린다. 폴더를 고르면 창이 닫히고 서버가
시작되며 QR 창(`MainForm`)이 뜬다. 취소 시 앱 종료.

```csharp
using var folderForm = new FolderSelectForm();
if (folderForm.ShowDialog() != DialogResult.OK) return; // 취소 시 종료
serveDir = folderForm.SelectedPath;
```

## 창 닫기

```csharp
protected override void OnFormClosing(FormClosingEventArgs e) {
    server.Stop();
    base.OnFormClosing(e);
}
```

## 로컬라이제이션

```csharp
static readonly bool IsKo =
    CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ko";

static string S(string ko, string en) => IsKo ? ko : en;
```

## 창 속성

- `FormBorderStyle`: `FixedSingle` (크기 조절 불가)
- `MaximizeBox`: `false`
- `BackColor`: `BgColor`
- `StartPosition`: `CenterScreen`
