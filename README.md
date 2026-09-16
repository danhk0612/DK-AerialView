# DK AerialView

지도 기반 조감도 캡처 및 AI 이미지 향상 도구입니다.

## 목표

- 주소 검색 후 지도 위치 자동 이동
- Google / Naver / Kakao 지도 Provider 선택
- 줌 / 회전 / 기울기 / 출력 프레임 자동 프리셋 및 수동 조정
- 현재 출력 프레임 캡처
- OpenRouter를 통한 이미지 업스케일/조감도 향상
- 결과 비교 및 저장

## 현재 상태

초기 개발 단계입니다. Google Maps Provider를 우선 구현하고 이후 Naver/Kakao Provider를 추가합니다.

## 기술 스택

- .NET 8
- WPF
- Microsoft WebView2
- 지도별 JavaScript SDK
- OpenRouter Image API

자세한 범위와 진행 상태는 `PROJECT.md`, `ARCHITECTURE.md`, `TASKS.md`를 참고하세요.
