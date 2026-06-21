# QR 코드 직접 구현

외부 라이브러리 없이 .NET BCL만으로 구현. URL 인코딩 전용 (Byte 모드만 지원하면 됨).

## 목표 출력

`bool[,] matrix` — `true`면 검은 모듈(흰색으로 렌더링), `false`면 배경.

## 구현 단계

### 1. 데이터 인코딩 (QrEncoder.cs)

URL은 항상 **Byte 모드** 사용.

```
모드 지시자:    0100 (4비트)
문자 수:        8비트 (버전 1~9 기준)
데이터:         UTF-8 바이트 각각 8비트
종료자:         0000 (4비트)
패딩 비트:      바이트 경계 맞춤
패딩 코드워드:  0xEC, 0x11 반복
```

### 2. 오류 정정 (ReedSolomon.cs)

GF(256) 갈루아 필드 위에서 Reed-Solomon 다항식 나눗셈.

```csharp
// GF(256) 생성 (원시 다항식 0x11D)
static readonly int[] GfExp = new int[512];
static readonly int[] GfLog = new int[256];

static ReedSolomon() {
    int x = 1;
    for (int i = 0; i < 255; i++) {
        GfExp[i] = x;
        GfLog[x] = i;
        x <<= 1;
        if ((x & 0x100) != 0) x ^= 0x11D;
    }
    for (int i = 255; i < 512; i++) GfExp[i] = GfExp[i - 255];
}

public static int[] GetEcBytes(int[] data, int ecCount) {
    // 생성 다항식으로 나눗셈하여 나머지(EC 코드워드) 반환
}
```

### 3. QR 버전 선택

URL 길이에 따라 자동 선택. `http://{ip}:{port}/?auth={32hex}` 형태는 최대 60자 내외이므로 버전 3~4 수준.

버전별 용량 표 (ECC Level M 기준):

| 버전 | 최대 바이트 |
|------|------------|
| 1 | 14 |
| 2 | 26 |
| 3 | 42 |
| 4 | 62 |
| 5 | 84 |

### 4. 매트릭스 배치 (QrMatrix.cs)

```
1. 빈 매트릭스 초기화 (N×N, N = 17 + 4*version)
2. Finder 패턴 3개 배치 (좌상, 우상, 좌하)
3. Separator 배치
4. Timing 패턴 배치
5. Alignment 패턴 배치 (버전 2+)
6. Format 정보 영역 예약
7. 데이터 비트 지그재그 배치
8. 마스킹 (패턴 0~7 중 페널티 점수 최소인 것 선택)
9. Format 정보 기록
```

### 5. Finder 패턴

```
1110111
1000001
1011101
1011101
1011101
1000001
1110111
```

### 6. 마스킹 패턴

| 패턴 | 조건 |
|------|------|
| 0 | `(r + c) % 2 == 0` |
| 1 | `r % 2 == 0` |
| 2 | `c % 3 == 0` |
| 3 | `(r + c) % 3 == 0` |
| 4 | `(r/2 + c/3) % 2 == 0` |
| 5 | `(r*c) % 2 + (r*c) % 3 == 0` |
| 6 | `((r*c) % 2 + (r*c) % 3) % 2 == 0` |
| 7 | `((r+c) % 2 + (r*c) % 3) % 2 == 0` |

### 7. 공개 API

```csharp
public static class QrCode {
    // url을 인코딩한 QR 매트릭스 반환 (border=1 포함)
    public static bool[,] Encode(string url, ErrorCorrectionLevel ecl = ErrorCorrectionLevel.M);
}

public enum ErrorCorrectionLevel { L, M, Q, H }
```

## 렌더링 연동

`MainForm`에서 `QrCode.Encode(authUrl)` 호출 → `bool[,]` 반환 → `Panel.Paint`에서 그림.
