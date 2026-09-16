# DK AerialView - Project

## 목적
주소를 입력하면 지도에서 해당 위치를 찾고 조감도 구도를 자동/수동으로 조정한 뒤, 현재 출력 프레임을 캡처하고 필요 시 OpenRouter 이미지 모델로 향상하여 결과를 저장하는 Windows 데스크톱 도구.

## 기본 원칙
- 옵션화 가능한 기능은 설정 또는 작업 화면 옵션으로 제공한다.
- AI가 필요하지 않은 작업은 로컬/지도 SDK 기능으로 처리한다.
- AI 이미지 처리가 필요한 경우 OpenRouter를 사용한다.
- 설정, 결과 비교 등 독립성이 높은 기능은 별도 창으로 분리한다.
- 지도 Provider별 기능 차이를 공통 인터페이스 뒤로 숨긴다.

## 1차 범위
1. Google Maps Provider
2. 주소 검색
3. 위성 지도
4. 줌/기울기/회전 조정
5. 조감도 프리셋
6. 출력 프레임 비율/크기 프리셋
7. WebView2 캡처
8. OpenRouter reference image 기반 이미지 향상
9. 원본/결과 확인 및 저장

## 후속 범위
- Naver Maps Provider
- Kakao Maps Provider
- 모델 capability 자동 조회
- 공급자별 지원 기능에 따른 UI 자동 활성/비활성
