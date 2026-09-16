# DK AerialView

지도 기반 조감도 캡처 및 AI 이미지 향상 도구입니다.

## 주요 기능

- 주소 검색 후 지도 위치 자동 이동
- Google / Naver / Kakao 지도 Provider 선택
- Google: zoom / tilt / heading 조정
- Naver / Kakao: zoom / 중심 위치 조정
- 위성 지도 기반 출력 프레임 표시
- FHD / QHD / UHD / 1:1 출력 프리셋
- 현재 출력 프레임 PNG 캡처
- OpenRouter reference-image 기반 AI 조감도 향상
- OpenRouter 이미지 편집 모델 목록 및 capability 자동 조회
- 원본 / AI 결과 비교 및 결과 저장

## 현재 상태

- Google / Naver / Kakao Provider 구현 완료
- Windows GitHub Actions Release 빌드 성공
- 실제 API Key를 사용한 사용자 PC 통합 검증 남음
- 버전: `0.1.0`

## 설정

프로그램의 **설정** 창에서 필요한 키를 입력합니다.

- Google Maps API Key
- Naver Maps Client ID
- Kakao JavaScript Key
- OpenRouter API Key
- 기본 지도 Provider
- 기본 출력 크기
- OpenRouter 이미지 모델
- AI 기본 프롬프트

### 지도 Web Origin

WebView2의 MapHost는 다음 가상 HTTPS Origin으로 실행됩니다.

```text
https://app.dk-aerialview.local
```

Naver Maps의 Web Service URL 및 Kakao JavaScript SDK 도메인 제한을 사용하는 경우 위 Origin을 등록해야 합니다.

Naver 주소 검색은 `geocoder` 서브모듈을, Kakao 주소 검색은 `services` 라이브러리를 사용합니다.

## 빌드

```powershell
dotnet restore DK-AerialView.sln
dotnet build DK-AerialView.sln --configuration Release
```

## 릴리스

`v*` 형식의 Git 태그를 push하면 GitHub Actions가 자동으로 다음 작업을 수행합니다.

1. `win-x64` self-contained publish
2. 배포 폴더 ZIP 압축
3. GitHub Release 생성
4. Release ZIP 첨부

## 기술 스택

- .NET 8
- WPF
- Microsoft WebView2
- Google Maps JavaScript API
- NAVER Maps JavaScript API v3
- Kakao 지도 Web API
- OpenRouter Image API

자세한 범위와 진행 상태는 `PROJECT.md`, `ARCHITECTURE.md`, `TASKS.md`를 참고하세요.
