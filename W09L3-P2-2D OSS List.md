# 컴퓨터그래픽스 수업용 오픈소스 (2D)

---

## 한눈에 보기

| 주제 | 프로젝트 | 소개 포인트 |
|---|---|---|
| 실습 환경 | ★ [p5.js](https://github.com/processing/p5.js) | 학기 내내 쓸 수 있는 2D 그리기 환경 |
| 래스터화 | ★ [tinyrenderer](https://github.com/ssloy/tinyrenderer) (1~2강) | 브레젠험 선 그리기, 삼각형 채우기를 직접 구현 |
| 벡터 렌더링 | ★ [NanoVG](https://github.com/memononen/nanovg) | 경로 채우기·안티앨리어싱을 GPU로 처리하는 작은 라이브러리 |
| 곡선 | ★ [Bezier.js](https://github.com/pomax/bezierjs) | 드 카스텔조·곡선 분할. 같은 저자의 *A Primer on Bézier Curves*와 함께 |
| 다각형 삼각분할 | ★ [Earcut](https://github.com/mapbox/earcut) | 귀 자르기 알고리즘을 짧은 코드로 |
| 다각형 클리핑 | ★ [Clipper2](https://github.com/angusjohnson/clipper2) | 교집합·합집합·오프셋의 표준 구현 |
| 계산기하 | ★ [Delaunator](https://github.com/mapbox/delaunator) | 들로네 삼각분할 + 반변(half-edge) 자료구조 |
| 이미지 처리 | ★ [dither](https://github.com/makew0rld/dither) | 오차 확산·순서 디더링 비교 |
| 이미지 기반 표현 | ★ [Primitive](https://github.com/fogleman/primitive) | 도형을 하나씩 얹어 사진을 근사 — 학생 반응이 좋음 |
| 2D 물리 | ★ [Matter.js](https://github.com/liabru/matter-js) | 브라우저에서 바로 도는 강체 충돌 데모 |

---

## 주제별 추천 목록

### 1. 2D 좌표 변환과 그리기 API

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 330 | ★ [p5.js](https://github.com/processing/p5.js) | 코드 드로잉 | `translate/rotate/scale`과 행렬 스택 실습 |
| 22 | [Go Graphics (gg)](https://github.com/fogleman/gg) | 아핀 변환·행렬 합성 | 변환 합성 순서가 결과를 바꾸는 예시 |
| 10 | [Paper.js](https://github.com/paperjs/paper.js) | 곡선 경로·불리언 연산 | 장면 그래프 방식의 2D 벡터 그래픽스 |
| 59 | [Pts](https://github.com/williamngan/pts) | 점·벡터·창작 기하 | 벡터 연산을 시각적으로 보여주는 데모 |

### 2. 래스터화·벡터 렌더링·안티앨리어싱

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 373 | ★ [tinyrenderer](https://github.com/ssloy/tinyrenderer) | 선·삼각형 래스터화 | 앞부분(선 그리기, 삼각형 채우기)만 2D 과제로 사용 |
| 191 | ★ [NanoVG](https://github.com/memononen/nanovg) | 스텐실 채움·경계 안티앨리어싱 | 경로 채우기가 GPU에서 어떻게 처리되는지 |
| 44 | [libtess2](https://github.com/memononen/libtess2) | 스윕 라인·와인딩 규칙 | even-odd / nonzero 채우기 규칙 |
| 47 | [robust-point-in-polygon](https://github.com/mikolalysenko/robust-point-in-polygon) | 교차 패리티 | 점의 안팎 판정 = 채우기 규칙의 기초 |
| 38 | [Lyon](https://github.com/nical/lyon) | 벡터 경로 테셀레이션 | 곡선 경로를 삼각형으로 바꿔 GPU에 넘기기 |
| 329 | [Vello](https://github.com/linebender/vello) | 병렬 벡터 래스터화 | 최신 GPU 벡터 렌더러 사례 (심화) |
| 40 | [polyline-normals](https://github.com/mattdesl/polyline-normals) | 마이터 접합·선의 법선 | 두께 있는 선(stroke) 만들기 |
| 51 | [extrude-polyline](https://github.com/mattdesl/extrude-polyline) | 선 두께·마이터 접합 | 선 끝·이음 처리 비교 |
| 201 | [glsl-aastep](https://github.com/glslify/glsl-aastep) | 화면 미분·안티앨리어싱 | 셰이더로 부드러운 경계 만들기 |
| 196 | [glsl-fxaa](https://github.com/mattdesl/glsl-fxaa) | 후처리 안티앨리어싱 | 앨리어싱의 원인과 해결책 |

### 3. 색과 합성

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 186 | [Color.js](https://github.com/color-js/color.js) | 지각 색공간·색 보간 | RGB와 지각 색공간(OKLab 등) 보간 비교 |
| 199 | [HSLuv](https://github.com/hsluv/hsluv-javascript) | 지각 명도·채도 | HSL의 한계와 개선 |
| 208 | [Chroma.js](https://github.com/gka/chroma.js) | 색공간 변환·보간 | 색 팔레트 만들기 |
| 142 | [Spectral.js](https://github.com/rvanwijnen/spectral.js) | 쿠벨카–뭉크 혼색 | 빛의 혼합 vs 물감의 혼합 |
| 200 | [glsl-blend](https://github.com/jamieowen/glsl-blend) | 픽셀별 합성 | 블렌드 모드(곱하기·스크린 등) 수식 |

### 4. 곡선

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 23 | ★ [Bezier.js](https://github.com/pomax/bezierjs) | 드 카스텔조·곡선 분할 | 인터랙티브 베지어 곡선 설명 |
| 21 | [adaptive-bezier-curve](https://github.com/mattdesl/adaptive-bezier-curve) | 적응형 세분화 | 곡선을 선분으로 바꿀 때 몇 개로 나눌지 |
| 54 | [b-spline](https://github.com/thibauts/b-spline) | de Boor 알고리즘 | 베지어에서 B-스플라인으로 |
| 27 | [chaikin-smooth](https://github.com/experience-monks/chaikin-smooth) | 모서리 자르기 | 세분 곡선의 가장 단순한 예 |
| 32 | [d3-shape](https://github.com/d3/d3-shape) | Catmull–Rom 스플라인 | 보간 곡선과 근사 곡선 비교 |
| 45 | [fit-curve](https://github.com/soswow/fit-curve) | 최소제곱 곡선 맞춤 | 손으로 그린 점들을 베지어로 변환 |
| 17 | [Simplify.js](https://github.com/mourner/simplify-js) | 더글러스–퍼커 | 선 단순화 |
| 26 | [Perfect Freehand](https://github.com/steveruizok/perfect-freehand) | 필압·가변 폭 획 | 드로잉 앱의 붓 획 만들기 |
| 39 | [Kurbo](https://github.com/linebender/kurbo) | 곡선 길이·곡률 계산 | 호 길이·곡률 (심화) |

### 5. 다각형 연산

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 11 | ★ [Clipper2](https://github.com/angusjohnson/clipper2) | 클리핑·오프셋 | 다각형 불리언과 오프셋 |
| 24 | [Martinez polygon clipping](https://github.com/w8r/martinez) | 스윕 라인 집합 연산 | 클리핑 알고리즘의 내부 구조 |
| 18 | ★ [Earcut](https://github.com/mapbox/earcut) | 귀 자르기 삼각분할 | 다각형 → 삼각형 |
| 49 | [poly-decomp.js](https://github.com/schteppe/poly-decomp.js) | 볼록 분해 | 충돌 처리용 볼록 조각 나누기 |
| 110 | [d3-polygon](https://github.com/d3/d3-polygon) | 신발끈 공식·무게중심 | 넓이·무게중심 계산 |
| 53 | [polygon-offset](https://github.com/w8r/polygon-offset) | 법선·호 연결 | 외곽선 두께 주기 |

### 6. 2D 계산기하

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 41 | ★ [Delaunator](https://github.com/mapbox/delaunator) | 들로네 삼각분할 | 반변 자료구조 소개 |
| 2 | [d3-delaunay](https://github.com/d3/d3-delaunay) | 들로네·보로노이 | 두 다이어그램의 쌍대 관계 시각화 |
| 29 | [cdt2d](https://github.com/mikolalysenko/cdt2d) | 제약 들로네 삼각분할 | 경계선을 지키는 삼각분할 |
| 46 | [monotone-convex-hull-2d](https://github.com/mikolalysenko/monotone-convex-hull-2d) | 단조 체인 | 볼록 껍질 기본 알고리즘 |
| 16 | [concaveman](https://github.com/mapbox/concaveman) | 오목 껍질 | 볼록 껍질과 비교 |
| 106 | [robust-predicates](https://github.com/mourner/robust-predicates) | 적응 정밀도 판정 | 부동소수점 오차가 기하 알고리즘을 망가뜨리는 이유 |
| 3 | [poisson-disk-sampling](https://github.com/kchapelier/poisson-disk-sampling) | 최소 간격 표본화 | 무작위 점 배치의 품질 비교 |
| 52 | [MarchingSquaresJS](https://github.com/raumzeit/marchingsquares.js) | 마칭 스퀘어 | 격자 데이터 → 등치선 |
| 313 | [d3-quadtree](https://github.com/d3/d3-quadtree) | 사분 트리 | 공간 분할로 탐색 가속 |

### 7. 이미지 처리

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 189 | ★ [dither](https://github.com/makew0rld/dither) | 오차 확산·순서 디더링 | 하프토닝 알고리즘 비교 |
| 185 | [image-q](https://github.com/ibezkrovnyi/image-quantization) | 색 양자화 | 팔레트 줄이기 |
| 204 | [glsl-halftone](https://github.com/glslify/glsl-halftone) | CMYK 망점 | 인쇄 망점 원리 |
| 202 | [glsl-fast-gaussian-blur](https://github.com/experience-monks/glsl-fast-gaussian-blur) | 분리 합성곱 | 합성곱 필터와 성능 최적화 |
| 195 | [glsl-fft](https://github.com/rreusser/glsl-fft) | 2D FFT | 주파수 영역 필터링 |
| 205 | [ndarray-warp](https://github.com/scijs/ndarray-warp) | 역방향 매핑·보간 | 이미지 변형에서 역방향 매핑이 필요한 이유 |
| 375 | [caire](https://github.com/esimov/caire) | 심 카빙 | 내용 인식 크기 조절 (동적 계획법 예제) |
| 226 | [scikit-image](https://github.com/scikit-image/scikit-image) | 필터·분할·형태 연산 | Python 이미지 처리 실습 |

### 8. 이미지 기반 표현·비사실적 렌더링·벡터화

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 183 | ★ [Primitive](https://github.com/fogleman/primitive) | 도형 근사 | 최적화로 그림 만들기 |
| 352 | [Threading](https://github.com/piellardj/image-stylization-threading) | 실 경로로 이미지 근사 | 스트링 아트 — 탐욕 알고리즘 예시 |
| 175 | [StippleGen](https://github.com/evil-mad/stipplegen) | 가중 보로노이 점묘 | 보로노이 응용 |
| 209 | [triangulate-image](https://github.com/snorpey/triangulate-image) | 윤곽 검출·삼각분할 | 로우폴리 이미지 |
| 116 | [Rough.js](https://github.com/rough-stuff/rough) | 확률적 획·해칭 | 손그림 스타일 렌더링 |
| 125 | [p5.brush](https://github.com/acamposuribe/p5.brush) | 벡터장·붓 질감 | p5.js에서 붓 표현 |
| 178 | [VTracer](https://github.com/visioncortex/vtracer) | 색 영역 분할·곡선 피팅 | 래스터 → 벡터 변환 |
| 177 | [ImageTracerJS](https://github.com/jankovicsandras/imagetracerjs) | 색 양자화·곡선 근사 | 브라우저에서 벡터화 |

### 9. 글자 렌더링

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 402 | [FreeType](https://github.com/freetype/freetype) | 글꼴 윤곽·래스터화 | 글자 = 베지어 윤곽을 채운 것 |
| 302 | [HarfBuzz](https://github.com/harfbuzz/harfbuzz) | 문자 → 글리프 배치 | 한글 조합·텍스트 셰이핑 |
| 187 | [TinySDF](https://github.com/mapbox/tiny-sdf) | 부호 거리장 | 거리장으로 글자 그리기 |
| 188 | [msdfgen](https://github.com/chlumsky/msdfgen) | 다중 채널 거리장 | 확대해도 모서리가 선명한 글자 |
| 13 | [p5-hershey-js](https://github.com/lingdong-/p5-hershey-js) | 획 기반 벡터 글자 | 윤곽 글꼴 vs 획 글꼴 |

### 10. 절차적 생성·패턴

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 129 | [simplex-noise.js](https://github.com/jwagner/simplex-noise.js) | 심플렉스 노이즈 | 절차적 텍스처의 기초 |
| 113 | ★ [WaveFunctionCollapse](https://github.com/mxgmn/wavefunctioncollapse) | 제약 기반 배치 | 2D 타일맵 생성 |
| 118 | [Lindenmayer](https://github.com/nylki/lindenmayer) | L-시스템 | 터틀 그래픽스로 식물 그리기 |
| 117 | [Context Free](https://github.com/mtnviewjohn/context-free) | 도형 문법·재귀 | 재귀 규칙으로 그림 만들기 |
| 7 | [TactileJS](https://github.com/isohedral/tactile-js) | 등면 타일링 | 에셔 스타일 타일 |
| 391 | [hatviz](https://github.com/isohedral/hatviz) | 비주기 모자 타일 | 2023년 발견된 비주기 단일 타일 |
| 8 | [streamlines](https://github.com/anvaka/streamlines) | 벡터장·유선 적분 | 흐름장 아트 |
| 379 | [doom-fire-algorithm](https://github.com/filipedeschamps/doom-fire-algorithm) | 격자·감쇠·팔레트 | 몇 줄로 만드는 불꽃 효과 |
| 123 | [Shan Shui Infinite](https://github.com/lingdong-/shan-shui-inf) | 절차형 산수화 | 절차적 생성의 예술적 응용 |

### 11. 2D 애니메이션

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 245 | [bezier-easing](https://github.com/gre/bezier-easing) | 삼차 베지어 시간 보간 | 이징 곡선 = 시간에 대한 베지어 |
| 247 | [Flubber](https://github.com/veltman/flubber) | 윤곽 재표본화·대응 보간 | 모양 모핑 |
| 255 | [Lottie Web](https://github.com/airbnb/lottie-web) | JSON 키프레임·벡터 렌더 | 키프레임 애니메이션 포맷 |
| 243 | [Motion Canvas](https://github.com/motion-canvas/motion-canvas) | 코드 기반 애니메이션 | 설명용 모션 그래픽 제작 |

### 12. 2D 물리 시뮬레이션

| # | 프로젝트 | 핵심 개념 | 수업 활용 |
|---:|---|---|---|
| 168 | ★ [Matter.js](https://github.com/liabru/matter-js) | 강체 충돌·거리 제약 | 브라우저 데모로 바로 시연 |
| 149 | [Box2D](https://github.com/erincatto/box2d) | 강체·충돌 | 2D 게임 물리 엔진의 표준 |
| 162 | [WebGL Fluid Simulation](https://github.com/paveldogreat/webgl-fluid-simulation) | 실시간 유체 | 수업 시작용 시연으로 인상적 |
| 163 | [Ready](https://github.com/gollygang/ready) | 반응·확산 | 튜링 패턴 |

### 실습 도구

| # | 프로젝트 | 용도 |
|---:|---|---|
| 330 | [p5.js](https://github.com/processing/p5.js) | JavaScript 2D 스케치 (위 1번과 동일) |
| 348 | [py5](https://github.com/py5coding/py5) | Python으로 Processing 사용 |
| 347 | [canvas-sketch](https://github.com/mattdesl/canvas-sketch) | 캔버스 작업을 고해상도 이미지·프레임으로 출력 |

출처: https://indexopensource.com/index