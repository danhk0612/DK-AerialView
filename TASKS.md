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
- [x] T10 지도/API 통합 방향 확정: Kakao SkyView 단일 Provider로 단순화
- [x] T11 출력 프레임 오버레이/비율 조정
- [x] T12 현재 프레임 캡처 및 PNG 저장
- [x] T13 AI 향상 실행 + 결과 창 + Before/After 비교 + 결과 저장
- [x] T14 OpenRouter 이미지 모델 목록/capability 조회 및 지원 파라미터 자동 적용
- [x] T15 Naver Maps Provider 구현 (이력, Kakao 단일화로 메인 UI에서 제거)
- [x] T16 Kakao Maps Provider 구현 (SkyView, 주소 검색, 줌/중심 동기화)
- [x] T17 배포/릴리스 자동화 (v* 태그 → win-x64 self-contained ZIP Release)
- [x] T18 Google 3D Maps Provider 구현 (이력, 실제 3D 표면 커버리지 제약으로 메인 UI에서 제거)
- [x] T19 지도 Provider 단순화: Google/Naver UI 및 설정 제거, Kakao SkyView 고정
- [x] T20 AI 조감도 생성 옵션 모델/옵션창 (시점, 방향, 구조 보존, 스타일, 출력 크기)
- [x] T21 AI 조감도 프롬프트 빌더 + OpenRouter reference-image 요청 연결
- [x] T22 현재 프레임 캡처 → AI 조감도 생성 → 결과 비교/저장 흐름 연결
- [x] T23 AI 향상/AI 조감도 기본 프롬프트 설정 분리
- [ ] T24 AI 조감도 실제 OpenRouter 결과 품질 검증 및 프롬프트 조정
- [x] T25 AI 조감도 옵션에 Kakao Roadview 참조/거리/검색 반경 추가
- [x] T26 별도 숨김 WebView2 Kakao Roadview 캡처 호스트 구현
- [x] T27 현재 중심 기준 주변 Roadview 자동 수집 + 대상 방향 pan 자동 계산
- [x] T28 항공사진 + Roadview 다중 reference OpenRouter 요청 연결 (모델 최대 reference 수 자동 제한)
- [ ] T29 Kakao Roadview 실제 캡처/다중 reference/AI 조감도 품질 검증
- [x] T30 Kakao 전용 MapHost 재구성: 좌클릭 드래그 이동 / 휠 0.1 미세 줌 / 우클릭 드래그 2D 회전
- [x] T31 회전 상태 화면 좌표 기준 드래그 보정 (화면 벡터 역회전 후 지도 중심 이동)
- [x] T32 AI 조감도 강제 사선 시점 프롬프트 + 실제 사용 Roadview 장수 상태/결과창 표시
- [ ] T33 사용자 PC에서 Kakao 미세 줌/회전/회전 상태 드래그 방향 최종 검증
- [x] T34 Roadview 최대 4장 미리보기/방향 표시/사용 체크 후 AI 생성 확인 단계
- [x] T35 Roadview 검토창을 수집 진행창으로 통합: 현재 단계/진행률/로그/성공 썸네일 실시간 표시
- [x] T36 Roadview 탐색 최적화: panoId 선탐색 → 고유 후보 선별 → 최종 후보만 실제 렌더링/캡처
- [x] T37 Roadview 동일 pano 편향 완화: 후보점 로컬 35m 우선 탐색, 보조 방향/거리 단계 확장, 마지막에만 최대 검색 반경 사용
- [ ] T38 사용자 PC 도심/시골에서 고유 pano 후보 다양성 및 수집 속도 검증

## 실제 실행 검증 항목

- Kakao SkyView: 지도 로드, 주소 검색, 0.1 단위 미세 줌, 0~359° 2D 회전, 회전 상태 화면 방향 드래그, 캡처
- Kakao 지도 입력: 왼쪽 드래그 이동, 마우스 휠 미세 줌, 오른쪽 드래그 회전
- Kakao Roadview: 중심 주변 panoId 선탐색, 고유 pano 중복 제거, 방향 분산 선별, 대상 중심 자동 pan, 최종 후보만 숨김 WebView2 렌더링/캡처
- Roadview 탐색: 우선 거리 기본 8방향 → 보조 8방향 → 추가 거리 단계, 로컬 약 35m 반경 우선, 부족할 때만 최대 검색 반경 확장
- Roadview 검토: 수집 시작 전에 창 표시, 탐색/캡처 단계·고유 후보·중복·실패·로그 실시간 표시, 성공 썸네일 즉시 추가, 개별 사용 체크
- OpenRouter AI 향상: 모델 목록 조회, reference 이미지 요청, 결과 비교/저장
- OpenRouter AI 조감도: 항공사진을 1번 기준 reference로 사용, Roadview는 높이/외벽/지붕 추정용 보조 reference로 사용, 모델 최대 reference 수 자동 제한, 강제 사선 시점 재구성
- MapHost/RoadviewHost Origin: `https://app.dk-aerialview.local`

## 배포 검증

- `main` 빌드: Restore → Release Build → win-x64 Runtime Restore → self-contained Publish → Artifact Upload
- 테스트 Artifact: `DK-AerialView-win-x64`
- Artifact 내부 필수 파일: `DK-AerialView.exe`, `.NET runtime`, `Assets/MapHost/index.html`, `Assets/RoadviewHost/index.html`
