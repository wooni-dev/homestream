# HTTP 서버 + 인증

## StreamServer

`HttpListener`를 사용하는 멀티스레드 HTTP 서버.

### 포트 자동 순환

- 기본 포트: 8000
- 충돌 시 8001~8009 순서대로 시도
- 모두 실패 시 예외 발생

```csharp
for (int port = 8000; port < 8010; port++) {
    try {
        listener.Prefixes.Add($"http://+:{port}/");
        listener.Start();
        ActualPort = port;
        break;
    } catch (HttpListenerException) { continue; }
}
```

### 요청 처리

`ThreadPool` 또는 `Task`로 각 요청을 병렬 처리.

```csharp
while (listener.IsListening) {
    var ctx = await listener.GetContextAsync();
    _ = Task.Run(() => HandleRequest(ctx));
}
```

### LAN IP 감지

```csharp
using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
s.Connect("8.8.8.8", 80);
return ((IPEndPoint)s.LocalEndPoint!).Address.ToString();
```

---

## 인증 (AuthManager)

### 토큰 생성

프로세스 시작 시 1회 생성, 프로세스 종료까지 고정.

```csharp
Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();
```

### 세션 관리

```csharp
// {sid: expireAt (Environment.TickCount64 기준 ms)}
private readonly Dictionary<string, long> _sessions = new();
private const long CookieMaxAgeMs = 7L * 24 * 3600 * 1000;
private const string CookieName = "hs_sid";
```

### 인증 체크 순서

1. `Cookie` 헤더에서 `hs_sid` 추출 → 만료 확인 → 유효하면 `true`
2. `?auth=` 쿼리 파라미터가 토큰과 일치 → 새 sid 발급 → 302 리다이렉트 → `false`
3. 위 둘 다 아니면 403 응답 → `false`

### 세션 쿠키 발급

```
Set-Cookie: hs_sid={sid}; Path=/; Max-Age=604800; HttpOnly; SameSite=Lax
```

---

## Range 요청 처리 (RangeHandler)

모바일 영상 seek를 위한 HTTP/1.1 206 Partial Content 구현.

### Range 헤더 파싱

```
Range: bytes=1048576-2097151
```

- `start` 없으면 0
- `end` 없으면 fileSize - 1
- `start >= fileSize` → 416

### 응답 헤더

```
HTTP/1.1 206 Partial Content
Content-Type: video/mp4
Accept-Ranges: bytes
Content-Range: bytes {start}-{end}/{size}
Content-Length: {length}
```

### 전송

64KB 청크 단위로 읽어서 전송. `BrokenPipeException` 발생 시 조용히 종료.

---

## 지원 확장자

```csharp
private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
    { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v" };
```

## MIME 타입

| 확장자 | MIME |
|--------|------|
| .mp4 | video/mp4 |
| .mkv | video/x-matroska |
| .avi | video/x-msvideo |
| .mov | video/quicktime |
| .wmv | video/x-ms-wmv |
| .m4v | video/x-m4v |
