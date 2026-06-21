# 웹 UI (인라인 HTML)

외부 리소스 없음. 모든 CSS/HTML을 서버에서 직접 렌더링.

## 공통 CSS

```css
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
```

## 폴더 목록 페이지

URL: `/{path}/` (디렉터리)

### 레이아웃

```
[sticky header]
  제목: 현재 폴더명
  sub: "폴더 N · 영상 M"
  breadcrumb: HOME / 상위폴더 / 현재폴더

[목록]
  디렉터리: [D 아이콘] 이름 >
  영상파일: [▶ 아이콘] 이름 / 크기(MB) >
```

### breadcrumb

- 루트: `<a href="/">HOME</a>`
- 각 경로 세그먼트: `<a href="/seg1/seg2/">seg2</a>`
- 구분자: `/` (색상 `#444`)

### 빈 폴더

```html
<div class="empty">이 폴더에 영상이 없습니다</div>
```

## 영상 플레이어 페이지

URL: `/{path}/video.mp4?play`

```html
<body class="pbody">
  <div class="ptop">
    <a class="back" href="javascript:history.back()">‹ 뒤로</a>
    <div class="ptitle">{파일명}</div>
  </div>
  <div class="pwrap">
    <video controls autoplay playsinline preload="metadata" src="{url}"></video>
  </div>
</body>
```

## 파일 크기 표시

```csharp
static string HumanSize(long n) =>
    n >= 1024L * 1024 * 1024
        ? $"{n / (1024.0 * 1024 * 1024):F1} GB"
        : $"{n / (1024.0 * 1024):F0} MB";
```

## URL 인코딩

파일/폴더명에 한글, 공백 등이 포함될 수 있으므로 링크 생성 시 `Uri.EscapeDataString()` 사용.
HTML 출력 시 `System.Net.WebUtility.HtmlEncode()` 사용.
