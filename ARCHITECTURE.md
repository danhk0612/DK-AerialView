# Architecture

## 구성

```text
WPF UI
 ├─ MainWindow
 ├─ SettingsWindow
 └─ ResultWindow (후속)

Application
 ├─ SettingsService
 ├─ MapHostService
 ├─ CaptureService
 └─ OpenRouterImageService

Map Provider
 ├─ IMapProvider
 ├─ GoogleMapProvider
 ├─ NaverMapProvider (후속)
 └─ KakaoMapProvider (후속)

WebView2
 └─ Assets/MapHost/index.html
```

## 지도 통신
WPF와 지도 JavaScript는 WebView2 WebMessage를 사용한다.

C# -> JS
- initialize
- searchAddress
- setCamera
- setMapType

JS -> C#
- ready
- cameraChanged
- geocodeResult
- error

## 설정 저장
`%APPDATA%/DK-AerialView/settings.json`

API Key는 저장 시 로컬 설정 파일에만 존재하고 저장소에는 포함하지 않는다.
