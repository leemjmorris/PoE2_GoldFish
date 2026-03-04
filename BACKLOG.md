# PoE2 GoldFish — 개선 백로그

> v0.6 완료 후 코드 리뷰에서 도출된 개선 항목들.
> 중요도 순으로 정렬.

---

## 🔴 중요 (버그)

### 1. 무기 DPS 필터 중복 설정 버그
**파일:** `Features/Trade/TradeOverlay.xaml.cs` — `BuildSearchRequest()` (284~300번째 줄)

현재 `PhysicalDps`, `ElementalDps`, `TotalDps` 필터를 **동시에** 설정하고 있음.
Trade API는 이를 AND 조건으로 처리하므로:
- 물리 특화 무기 (pDPS=150, eDPS=0) → eDPS 하한 필터에 걸려 검색 결과 누락

**수정 방향:**
```csharp
// 현재 (잘못됨) — pDPS, eDPS, totalDPS 모두 설정
if (item.PhysicalDps.HasValue)   wf.PhysicalDps = ...
if (item.ElementalDps.HasValue)  wf.ElementalDps = ...
if (item.TotalDps.HasValue)      wf.TotalDps = ...

// 수정 — totalDPS 우선, 단일 유형만 있을 때 개별 필터 사용
if (item.TotalDps.HasValue)
    wf.TotalDps = new RangeFilter { Min = Math.Floor(item.TotalDps.Value * 0.8) };
else if (item.PhysicalDps.HasValue)
    wf.PhysicalDps = new RangeFilter { Min = Math.Floor(item.PhysicalDps.Value * 0.8) };
else if (item.ElementalDps.HasValue)
    wf.ElementalDps = new RangeFilter { Min = Math.Floor(item.ElementalDps.Value * 0.8) };
```

---

## 🟡 소규모 (UX/기능)

### 2. Online Only 설정 미저장
**파일:** `Core/AppSettings.cs`, `Features/Trade/TradeOverlay.xaml.cs`

앱 재시작 시 "Online only" 체크박스가 항상 `true`로 초기화됨.

```csharp
// AppSettings.cs에 추가
public bool TradeOnlineOnly { get; set; } = true;

// TradeOverlay.xaml.cs — LoadState()에 추가
OnlineOnlyCheck.IsChecked = AppSettings.Instance.TradeOnlineOnly;

// TradeOverlay.xaml.cs — OnSearchClick()에서 저장
AppSettings.Instance.TradeOnlineOnly = OnlineOnlyCheck.IsChecked == true;
AppSettings.Instance.Save();
```

---

### 3. 리그 이름 오류 메시지 불명확
**파일:** `Features/Trade/Services/TradeApiClient.cs` — `SearchAndFetchAsync()` (66~72번째 줄)

잘못된 리그명 입력 시 `"Search failed: NotFound"` 표시 — 원인 불명확.

```csharp
// 현재
if (!searchResponse.IsSuccessStatusCode)
    return new TradeResult { Error = $"Search failed: {searchResponse.StatusCode}" };

// 수정
if (!searchResponse.IsSuccessStatusCode)
{
    var msg = searchResponse.StatusCode == HttpStatusCode.NotFound
        ? "League not found. Check league name."
        : $"Search failed: {(int)searchResponse.StatusCode}";
    return new TradeResult { Error = msg };
}
```

---

### 4. 젬 아이템 레벨/퀄리티 지원
**파일:** `Features/Trade/Services/ItemParser.cs`, `Features/Trade/TradeOverlay.xaml.cs`

Gem 아이템 복사 시 `Gem Level: X` 파싱 없음. Trade API의 `misc_filters.gem_level` 활용 가능.

```csharp
// ItemParser.TryParseItemProperties()에 추가
else if (line.StartsWith("Gem Level:"))
{
    var m = Regex.Match(line, @"(\d+)");
    if (m.Success) item.GemLevel = int.Parse(m.Groups[1].Value);
    matched = true;
}

// ParsedItem.cs에 추가
public int? GemLevel { get; set; }

// BuildSearchRequest()에서 젬 레벨 필터 추가
if (item.GemLevel.HasValue)
{
    request.Query.Filters ??= new QueryFilters();
    request.Query.Filters.MiscFilters ??= new MiscFilters();
    request.Query.Filters.MiscFilters.Filters.GemLevel =
        new RangeFilter { Min = item.GemLevel.Value };
}

// MiscFilterValues에 추가
[JsonProperty("gem_level")]
public RangeFilter GemLevel { get; set; }
```

---

### 5. 무기/방어구 자동 필터 UI 토글 없음
**파일:** `Features/Trade/TradeOverlay.xaml`, `Features/Trade/TradeOverlay.xaml.cs`

`BuildSearchRequest()`가 DPS/AR/EV/ES 필터를 자동으로 적용하는데, 사용자가 끌 방법 없음.
Filters 탭 상단에 체크박스 추가 권장.

```xml
<!-- TradeOverlay.xaml — Filters 탭 최상단에 추가 -->
<CheckBox x:Name="AutoWeaponFilterCheck"
          Content="Auto weapon/armour filters (80%)"
          IsChecked="True"
          Foreground="{StaticResource TextBrush}"
          FontSize="11" Margin="8,4"/>
```

```csharp
// BuildSearchRequest()에서 조건 추가
if (AutoWeaponFilterCheck.IsChecked == true &&
    (item.PhysicalDps.HasValue || ...))
{ ... }
```

---

## 🟢 코드 품질

### 6. StatIdResolver — crafted/enchant 타입 분리 미흡
**파일:** `Features/Trade/Services/StatIdResolver.cs` — `Resolve()` (88~92번째 줄)

`crafted`, `enchant`, `fractured` 타입이 모두 `_explicitMap`으로 fallback.
Stats API에서 crafted 모드 ID가 `explicit.*` 네임스페이스를 공유하므로 현재는 동작하나,
별도 맵(`_craftedMap`, `_enchantMap`)으로 구분하면 정확도 향상.

```csharp
// 현재
var map = type switch
{
    ModType.Implicit => _implicitMap,
    _ => _explicitMap
};

// 개선안
var map = type switch
{
    ModType.Implicit => _implicitMap,
    ModType.Crafted  => _craftedMap.Count > 0 ? _craftedMap : _explicitMap,
    ModType.Enchant  => _enchantMap.Count > 0 ? _enchantMap : _implicitMap,
    _ => _explicitMap
};
```

---

### 7. ParseMods의 Unidentified 스킵 주석 누락
**파일:** `Features/Trade/Services/ItemParser.cs` — `ParseMods()` 내부

섹션 레벨 감지(`item.IsUnidentified = sections.Any(...)`)와 `ParseMods` 내부의
`if (trimmed == "Unidentified") continue;` 코드가 중복.
의도적 방어 코드임을 주석으로 명시 권장.

```csharp
// 방어적 스킵 — 섹션 레벨에서 이미 감지하지만,
// "Unidentified"가 모드 섹션 안에 섞여있을 경우를 대비
if (trimmed == "Unidentified") continue;
```

---

## 구현 완료 이력

| 버전 | 주요 내용 |
|------|-----------|
| v0.1 | 메모 오버레이 (WPF 인프라) |
| v0.2 | 스크린샷 오버레이 |
| v0.3 | Trade 오버레이 기본 (클립보드 파싱 + API) |
| v0.4 | 클립보드 복원, Z-order, DI, 퍼지 매칭, ItemRarity 확장 |
| v0.5 | Trade UX (whisper, 시간표시, 브라우저, Corrupted) |
| v0.6 | 탐지 개선, API retry, mod 매칭 수, online 필터 |
| v0.7 | 무기/방어구 속성 파싱, DPS 계산, weapon/armour API 필터 |
