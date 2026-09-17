# DK AerialView

![DK AerialView](src/DKAerialView/Assets/AppIcon.svg)

**DK AerialView**는 Kakao SkyView를 기반으로 항공 지도를 탐색하고, 현재 화면을 캡처하거나 OpenRouter 이미지 모델을 이용해 사선 조감도를 생성하는 Windows용 도구입니다.

- 제작: **참빛바다**
- 버전: **0.2.0**
- 라이선스: **MIT**

## 주요 기능

- Kakao SkyView 기반 항공 지도 탐색
- 주소, 건물명, 역명, 상호/장소명 통합 검색
- 왼쪽 드래그 이동, 휠 미세 줌, 오른쪽 드래그 2D 회전
- FHD / QHD / UHD / 2048×2048 출력 프리셋
- 현재 지도 프레임 PNG 캡처
- OpenRouter 기반 이미지 향상
- OpenRouter 기반 사선 조감도 생성
- Kakao Roadview 참조 이미지 자동 수집
- Roadview 후보 탐색 / 중복 제거 / 최대 4장 실시간 검토
- 현재 지도 중앙점을 기준으로 Roadview 시선 방향 계산
- 선택한 Roadview만 AI 참조 이미지로 사용

## 설치

GitHub의 **Releases**에서 최신 `DK-AerialView-vX.Y.Z-win-x64.zip` 파일을 내려받아 원하는 폴더에 압축을 풀고 `DK-AerialView.exe`를 실행합니다.

배포 파일은 `win-x64` self-contained 형식이므로 별도의 .NET 8 설치는 필요하지 않습니다. Microsoft Edge WebView2 Runtime은 Windows 환경에 필요합니다.

## 처음 설정

오른쪽 위 **설정** 버튼에서 필요한 키를 입력합니다.

### Kakao JavaScript Key

지도, 장소 검색, Roadview 기능에 사용합니다.

Kakao Developers에서 JavaScript Key를 발급하고 JavaScript SDK 도메인에 아래 Origin을 등록해야 합니다.

```text
https://app.dk-aerialview.local
```

### OpenRouter API Key

AI 이미지 향상 및 조감도 생성 기능을 사용할 때 필요합니다. 단순 지도 탐색과 캡처만 사용할 경우에는 필요하지 않습니다.

설정 화면에서 OpenRouter 모델을 선택할 수 있으며, 모델이 지원하는 참조 이미지 수에 따라 Roadview 사용 가능 장수가 달라집니다.

## 기본 사용법

1. 상단 검색창에 주소 또는 장소명을 입력합니다.
2. 검색 결과가 여러 개면 지도 위 결과 목록에서 원하는 장소를 선택합니다.
3. 지도를 드래그하고 줌/회전을 조절해 원하는 구도를 맞춥니다.
4. 필요에 따라 **현재 프레임 캡처**, **AI 이미지 향상**, **AI 조감도 생성**을 실행합니다.

### AI 조감도 + Roadview

AI 조감도 생성 시 Roadview 사용을 켜면 현재 지도 중앙점 주변의 Kakao Roadview pano 후보를 먼저 빠르게 탐색합니다. 고유 후보를 찾은 뒤 실제 Roadview를 렌더링하며, 최대 4장의 참조 이미지를 검토창에서 확인하고 선택할 수 있습니다.

Roadview는 보조 참조 자료이며, AI 생성 결과는 실제 측량/설계 자료처럼 정확한 지리·건축 데이터를 보장하지 않습니다.

## 직접 빌드

Windows PowerShell 기준입니다.

```powershell
cd DK-AerialView

dotnet restore DK-AerialView.sln

dotnet publish .\src\DKAerialView\DKAerialView.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\publish\win-x64
```

빌드 과정에서 `tools/Generate-AppIcon.ps1`이 Windows용 `AppIcon.ico`를 자동 생성해 EXE에 적용합니다.

## 자동 배포

`main` 브랜치에 변경이 들어오면 GitHub Actions가 Windows 빌드와 self-contained publish를 검증합니다.

프로젝트의 `<Version>`에 해당하는 Release가 아직 없으면 Actions가 자동으로:

1. `win-x64` self-contained publish
2. ZIP 패키징
3. `vX.Y.Z` 태그 및 GitHub Release 생성
4. `DK-AerialView-vX.Y.Z-win-x64.zip` 첨부

를 수행합니다. 같은 버전의 Release가 이미 존재하면 중복 배포하지 않습니다.

## 기술 스택

- .NET 8 / WPF
- Microsoft WebView2
- Kakao 지도 Web API / Places / Roadview
- OpenRouter Image API
- GitHub Actions

## 라이선스

MIT License. 자세한 내용은 [LICENSE](LICENSE)를 참고하세요.
