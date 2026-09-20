# Architecture

## 구성

```text
NativeAOT Launcher
 ├─ .NET 10 Desktop Runtime x64 검사
 └─ app/DK-AerialView.App.exe 실행

WPF UI
 ├─ MainWindow
 ├─ SettingsWindow
 └─ ResultWindow

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
 ├─ app/Assets/MapHost/index.html
 └─ app/Assets/RoadviewHost/index.html
```

## 배포

- 대상: Windows x64
- 실제 앱: .NET 10 WPF, framework-dependent, single-file
- 런처: .NET 10 NativeAOT, self-contained
- .NET Desktop Runtime은 배포 ZIP에 포함하지 않는다.
- 런처가 Microsoft.WindowsDesktop.App 10.x 설치 여부를 확인한 뒤 실제 앱을 실행한다.
- MapHost/RoadviewHost HTML은 기존 `AppContext.BaseDirectory/Assets` 경로 계약을 유지하기 위해 실제 앱과 함께 `app/Assets`에 외부 파일로 배포한다.

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
