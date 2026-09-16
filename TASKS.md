# Tasks

- [x] T01 저장소/프로젝트 초기화
- [x] T02 WPF 메인창/설정창 분리
- [x] T03 Map Provider capability 구조
- [x] T04 WebView2 Map Host 골격
- [x] T05 Google Maps JS Provider 초기 연동
- [x] T06 주소 검색 및 카메라 명령 브리지
- [x] T07 로컬 설정 저장 구조
- [x] T08 OpenRouter 이미지 reference 요청 서비스
- [ ] T09 실제 Windows 빌드/실행 검증 (GitHub Actions Windows Release 빌드 + win-x64 self-contained publish/artifact 성공, 사용자 PC 실행 검증 진행 중)
- [ ] T10 실제 지도/API 통합 검증 (Google/Naver/Kakao 지도 표시 확인, 세부 기능 검증 진행 중)
- [x] T11 출력 프레임 오버레이/비율 조정
- [x] T12 현재 프레임 캡처 및 PNG 저장
- [x] T13 AI 향상 실행 + 결과 창 + Before/After 비교 + 결과 저장
- [x] T14 OpenRouter 이미지 모델 목록/capability 조회 및 지원 파라미터 자동 적용
- [x] T15 Naver Maps Provider 구현 (위성 지도, 주소 검색, 줌/중심 동기화)
- [x] T16 Kakao Maps Provider 구현 (SkyView, 주소 검색, 줌/중심 동기화)
- [x] T17 배포/릴리스 자동화 (v* 태그 → win-x64 self-contained ZIP Release)
- [x] T18 Google 3D Maps Provider 구현 (Map3DElement, SATELLITE, range/tilt/heading, 주소 검색, 카메라 동기화)
- [ ] T19 Google 3D 실제 지역 렌더링/캡처 최종 검증 (3D 지형 렌더링 확인, 표면 3D 데이터 지역 편차 확인)
- [x] T20 AI 조감도 생성 옵션 모델/옵션창 (시점, 방향, 구조 보존, 스타일, 출력 크기)
- [x] T21 AI 조감도 프롬프트 빌더 + OpenRouter reference-image 요청 연결
- [x] T22 현재 프레임 캡처 → AI 조감도 생성 → 결과 비교/저장 흐름 연결
- [x] T23 AI 향상/AI 조감도 기본 프롬프트 설정 분리
- [ ] T24 AI 조감도 실제 OpenRouter 결과 품질 검증 및 프롬프트 조정
- [x] T25 AI 조감도 옵션에 Kakao Roadview 참조/거리/검색 반경 추가
- [x] T26 별도 숨김 WebView2 Kakao Roadview 캡처 호스트 구현
- [x] T27 현재 중심 기준 북/동/남/서 Roadview 자동 수집 + 대상 방향 pan 자동 계산
- [x] T28 항공사진 + Roadview 다중 reference OpenRouter 요청 연결 (모델 최대 reference 수 자동 제한)
- [ ] T29 Kakao Roadview 실제 캡처/다중 reference/AI 조감도 품질 검증

## 실제 실행 검증 항목

- Google 3D: 3D 건물/지형 로드, 주소 검색, range/tilt/heading, 사용자 카메라 조작, 캡처
- Google 위성: 지도 로드, 주소 검색, zoom, 캡처
- Naver: 지도 로드, 주소 검색, zoom/center, 위성 지도, 캡처
- Kakao: 지도 로드, 주소 검색, zoom/center, SkyView, 캡처
- Kakao Roadview: 중심 기준 4방향 pano 검색, 대상 중심 자동 pan, 숨김 WebView2 캡처, 미존재 방향 건너뛰기
- OpenRouter AI 향상: 모델 목록 조회, reference 이미지 요청, 결과 비교/저장
- OpenRouter AI 조감도: 항공사진을 1번 기준 reference로 사용, Roadview는 높이/외벽/지붕 추정용 보조 reference로 사용, 모델 최대 reference 수 자동 제한
- 공통 MapHost/RoadviewHost Origin: `https://app.dk-aerialview.local`

## 배포 검증

- `main` 빌드: Restore → Release Build → win-x64 Runtime Restore → self-contained Publish → Artifact Upload
- 테스트 Artifact: `DK-AerialView-win-x64`
- Artifact 내부 필수 파일: `DK-AerialView.exe`, `.NET runtime`, `Assets/MapHost/index.html`, `Assets/RoadviewHost/index.html`
