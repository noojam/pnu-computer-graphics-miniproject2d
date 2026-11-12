using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace WfcPrototype
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class DungeonPrototype : MonoBehaviour
    {
        private const int DefaultWidth = 8;
        private const int DefaultHeight = 8;
        private const int MinSize = 4;
        private const int MaxSize = 20;
        private const int BoardWidth = 720;
        private const int BoardHeight = 480;
        private const int MaxAttempts = 6;
        
        // 기본 던전 생성 단계 진행 속도
        private const float DefaultPhaseDuration = 0.13f;
        private const float ExplanationPhaseDuration = 0.85f;
        private const float ClearBlinkInterval = 0.24f;
        private const int ClearBlinkSteps = 6;
        private const float MoveInterval = 0.12f;
        private const float StepHoldDelay = 0.25f;
        private const float StepRepeatInterval = 0.05f;
        private const int MaxChance = 100;
        private static readonly int[] DefaultChances = { 6, 13, 67, 13, 1 };
        private readonly IntegerField[] chanceFields = new IntegerField[DefaultChances.Length];
        private readonly int[] directionChances = new int[DefaultChances.Length];
        private CellView[] cells;
        private int[] beforePropagation;

        private enum Phase
        {
            Setup, Ready, SelectCell, InspectCell, PreviewTile, Collapse, ExplainPropagation,
            Propagate, ClearPaths, Explore, Escaped, Retry, NoRoute
        }
        private Phase phase;
        private bool IsGenerating => phase is Phase.Ready or Phase.SelectCell or Phase.InspectCell or
            Phase.PreviewTile or Phase.Collapse or Phase.ExplainPropagation or Phase.Propagate or Phase.ClearPaths or Phase.Retry;
        
        // 던전 생성 및 WFC 알고리즘 처리 담당 객체
        private DungeonWave wave; 

        // DungeonWave에서 TryFindPlayableArea() 호출 후 반환되는 값을 저장하는 배열
        private int[] reachableTiles;
        private int clearBlinkStep, disconnectedCells;
        private VisualElement setupView, gameView, board;
        private IntegerField widthField, heightField;
        private Button settingsButton;
        private Label setupError, chanceSummary, stage, stats, controls;
        private int width, height, attempt;
        private int seed, step, selected, changedCells, removedOptions, entrance, exit, player, moves;
        private bool paused;
        private float phaseDuration, nextPhase, nextMove;
        private float generationSpeed = 1f;
        private float nextStepRepeat;
        private Label speedValue;
        private RadioButtonGroup modeSelector;
        private Label boardCaption;
        private bool explanationMode;
        private VisualElement explanationView, console;
        private Label changedLegend, eligibleLegend;
        private readonly List<int> selectionCandidates = new();
        private int[] tileCandidates = System.Array.Empty<int>();
        private int previewIndex;
        private IEnumerator<DungeonWave.CandidateRemoval> propagation;
        private PanelSettings runtimePanelSettings;

        private void Awake()
        {
            var document = GetComponent<UIDocument>();
            runtimePanelSettings = Instantiate(document.panelSettings);
            runtimePanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimePanelSettings.referenceResolution = new Vector2Int(1280, 720);
            runtimePanelSettings.screenMatchMode = PanelScreenMatchMode.Expand;
            document.panelSettings = runtimePanelSettings;
        }

        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            console = root.Q<VisualElement>(className: "console");
            setupView = root.Q("setup-view");
            gameView = root.Q("game-view");
            board = root.Q("board");
            board.parent.style.width = BoardWidth;
            board.parent.style.height = BoardHeight;
            widthField = root.Q<IntegerField>("map-width");
            heightField = root.Q<IntegerField>("map-height");
            widthField.value = DefaultWidth;
            heightField.value = DefaultHeight;
            setupError = root.Q<Label>("setup-error");
            chanceSummary = root.Q<Label>("chance-summary");
            for (int i = 0; i < chanceFields.Length; i++)
            {
                var field = root.Q<IntegerField>($"chance-{i}");
                field.SetValueWithoutNotify(DefaultChances[i]);
                chanceFields[i] = field;
                field.RegisterValueChangedCallback(_ => UpdateChanceSummary());
            }
            UpdateChanceSummary();
            root.Q<Label>("size-hint").text = $"{MinSize}~{MaxSize}사이의 칸 수를 입력하세요.";
            settingsButton = root.Q<Button>("settings-button");
            settingsButton.clicked += ShowSettings;
            root.Q<Button>("quit-button").clicked += QuitGame;
            root.Q<Button>("start-button").clicked += StartFromSettings;
            stage = root.Q<Label>("stage");
            stats = root.Q<Label>("stats");
            controls = root.Q<Label>("controls");
            speedValue = root.Q<Label>("speed-value");
            var speedSlider = root.Q<Slider>("generation-speed");
            speedSlider.SetValueWithoutNotify(generationSpeed);
            speedSlider.RegisterValueChangedCallback(evt => SetGenerationSpeed(evt.newValue));
            speedValue.text = $"{generationSpeed:0.0}배";
            modeSelector = root.Q<RadioButtonGroup>("generation-mode");
            modeSelector.SetValueWithoutNotify(0);
            boardCaption = root.Q<Label>("board-caption");
            explanationView = root.Q("explanation-view");
            changedLegend = root.Q<Label>("changed-legend");
            eligibleLegend = root.Q<Label>("eligible-legend");

            ShowSettings();
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowSettings()
        {
            propagation?.Dispose();
            propagation = null;
            phase = Phase.Setup;
            setupError.text = "";
            SetSetupVisible(true);
        }

        private void SetSetupVisible(bool visible)
        {
            setupView.EnableInClassList("view-hidden", !visible);
            gameView.EnableInClassList("view-hidden", visible);
            settingsButton.EnableInClassList("view-hidden", visible);
        }

        private int GetChanceTotal()
        {
            int total = 0;
            foreach (var field in chanceFields)
            {
                if (field.value < 0 || field.value > MaxChance)
                    return -1;
                total += field.value;
            }
            return total;
        }

        private void UpdateChanceSummary()
        {
            setupError.text = "";
            int total = GetChanceTotal();
            if (total < 0)
            {
                chanceSummary.text = "각 타일 종류에 0~100 사이의 값을 입력하세요.";
                return;
            }
            if (total == 0)
            {
                chanceSummary.text = "타일 종류를 하나 이상 0%보다 높게 설정하세요.";
                return;
            }

            var shares = new string[chanceFields.Length];
            for (int i = 0; i < shares.Length; i++)
                shares[i] = $"{i}방향 {100.0 * chanceFields[i].value / total:0.#}%";
            chanceSummary.text = "기본 확률  " + string.Join("  /  ", shares);
        }

        private void StartFromSettings()
        {
            int requestedWidth = widthField.value;
            int requestedHeight = heightField.value;
            if (requestedWidth < MinSize || requestedWidth > MaxSize ||
                requestedHeight < MinSize || requestedHeight > MaxSize)
            {
                setupError.text = $"가로와 세로를 {MinSize}~{MaxSize}칸으로 입력하세요.";
                return;
            }

            if (!TryStoreDirectionChances()) return;

            width = requestedWidth;
            height = requestedHeight;
            explanationMode = modeSelector.value == 1;
            boardCaption.EnableInClassList("view-hidden", !explanationMode);
            explanationView.EnableInClassList("view-hidden", !explanationMode);
            console.EnableInClassList("explanation-active", explanationMode);
            eligibleLegend.EnableInClassList("view-hidden", !explanationMode);
            changedLegend.text = explanationMode ? "빨간색 / 후보가 줄어든 칸" : "청록색 / 후보가 줄어든 칸";
            changedLegend.EnableInClassList("red", explanationMode);
            changedLegend.EnableInClassList("cyan", !explanationMode);
            BuildBoard();
            SetSetupVisible(false);
            RestartGeneration();
        }

        private bool TryStoreDirectionChances()
        {
            int total = GetChanceTotal();
            if (total < 0)
            {
                setupError.text = "타일 등장 확률은 0~100%로 입력하세요.";
                return false;
            }
            if (total == 0)
            {
                setupError.text = "타일 종류를 하나 이상 0%보다 높게 설정하세요.";
                return false;
            }
            for (int i = 0; i < chanceFields.Length; i++)
                directionChances[i] = chanceFields[i].value;
            if (directionChances[0] + directionChances[1] + directionChances[2] == 0)
            {
                setupError.text = "모서리를 채우려면 0~2방향 타일 중 하나 이상이 필요합니다.";
                return false;
            }
            if (directionChances[2] + directionChances[3] + directionChances[4] == 0)
            {
                setupError.text = "탈출 경로를 만들려면 2~4방향 타일 중 하나 이상이 필요합니다.";
                return false;
            }

            return true;
        }

        private void BuildBoard()
        {
            board.Clear();
            int boardHeight = BoardHeight;
            board.parent.style.height = boardHeight;
            float cellSize = Mathf.Min((float)BoardWidth / width, (float)boardHeight / height);
            board.style.width = width * cellSize;
            board.style.height = height * cellSize;
            cells = new CellView[width * height];
            beforePropagation = new int[cells.Length];
            phaseDuration = Mathf.Clamp(DefaultPhaseDuration * DefaultWidth * DefaultHeight / cells.Length,
                0.045f, DefaultPhaseDuration);
            if (explanationMode) phaseDuration = ExplanationPhaseDuration;
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new CellView(cellSize, explanationMode ? (i + 1).ToString() : null);
                cells[i].Root.tooltip = CellName(i);
                cells[i].Root.style.left = i % width * cellSize;
                cells[i].Root.style.top = i / width * cellSize;
                board.Add(cells[i].Root);
            }
        }

        private void RestartGeneration()
        {
            attempt = 1;
            paused = false;
            Begin(UnityEngine.Random.Range(10000, 99999));
            Render();
        }

        // 던전 본격적으로 생성 시작하는 함수
        private void Begin(int newSeed)
        {
            propagation?.Dispose();
            propagation = null;
            seed = newSeed;
            wave = new DungeonWave(width, height, seed, directionChances);
            reachableTiles = null;
            phase = Phase.Ready;
            // 선택된 칸, 입구, 출구, 플레이어 위치 초기화
            selected = entrance = exit = player = -1;
            // 진행 단계, 이동 횟수 초기화
            step = moves = 0;
            selectionCandidates.Clear();
            tileCandidates = System.Array.Empty<int>();
            previewIndex = -1;
            changedCells = removedOptions = 0;
            nextMove = 0;
            nextStepRepeat = 0;
            nextPhase = Time.unscaledTime + 1.6f / generationSpeed;
        }

        // 매 프레임마다 호출되는 함수, 게임 진행 상태에 따라 던전 생성, 이동, 렌더링 등을 처리
        private void Update()
        {
            if (phase == Phase.Setup) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                RestartGeneration();
                return;
            } // r키 눌리면 던전 생성 재시작
            if (IsGenerating && keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                SkipGeneration();
                return;
            }// f키 눌리면 던전 생성 과정 건너뛰기

            if (phase == Phase.Explore)
            {
                if (keyboard != null) ReadMovement(keyboard);
                return;
            }// 던전 생성 끝나고 플레이어가 탐험 중이면 이동 처리, 
            // ReadMovement()에서 플레이어 이동 처리

            if (phase == Phase.Escaped || phase == Phase.NoRoute) return;

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                TogglePause();
            }
            if (keyboard != null && (keyboard.nKey.wasPressedThisFrame || keyboard.nKey.isPressed))
            {
                if (keyboard.nKey.wasPressedThisFrame || Time.unscaledTime >= nextStepRepeat)
                {
                    NextStep();
                    nextStepRepeat = Time.unscaledTime +
                        (keyboard.nKey.wasPressedThisFrame ? StepHoldDelay : StepRepeatInterval);
                }
            }
            else if (!paused && Time.unscaledTime >= nextPhase)
                Advance();
        }

        private void SetGenerationSpeed(float speed)
        {
            speed = Mathf.Clamp(speed, 0.5f, 10f);
            float now = Time.unscaledTime;
            nextPhase = now + Mathf.Max(0f, nextPhase - now) * generationSpeed / speed;
            generationSpeed = speed;
            speedValue.text = $"{generationSpeed:0.0}배";
        }

        private void TogglePause()
        {
            if (!IsGenerating) return;
            paused = !paused;
            Render();
        }

        private void NextStep()
        {
            if (!IsGenerating) return;
            paused = true;
            Advance();
        }

        private void SkipGeneration()
        {
            paused = false;
            while (IsGenerating) Advance(animate: false);
            Render();
        }

        // 던전 생성 단계 진행시키는 주요 함수, animate : 중간 함수 보여주는지
        private void Advance(bool animate = true)
        {
            nextPhase = Time.unscaledTime + phaseDuration / generationSpeed;
            switch (phase)
            {
                case Phase.SelectCell:
                    phase = Phase.InspectCell;
                    break;
                case Phase.InspectCell:
                    previewIndex = 0;
                    phase = Phase.PreviewTile;
                    break;
                case Phase.PreviewTile:
                    if (animate && ++previewIndex < tileCandidates.Length) break;
                    wave.CollapseCell(selected);
                    System.Array.Copy(wave.Counts, beforePropagation, cells.Length);
                    changedCells = removedOptions = 0;
                    propagation = wave.PropagateSteps().GetEnumerator();
                    phase = Phase.Collapse;
                    break;
                case Phase.ExplainPropagation:
                    AdvancePropagation(animate);
                    break;
                case Phase.ClearPaths: // 끊긴 통로 삭제하는 단계
                    if (!animate || ++clearBlinkStep >= ClearBlinkSteps)
                        StartExploring();
                    else
                        nextPhase = Time.unscaledTime + ClearBlinkInterval / generationSpeed;
                    break;
                case Phase.Retry: // 모순 발생 등으로 던전 생성 실패 시 재시도 단계
                    attempt++;
                    Begin(UnityEngine.Random.Range(10000, 99999));
                    break;
                case Phase.Collapse: // 확정된 칸의 후보 제거를 이웃으로 전파하는 단계
                    if (explanationMode)
                    {
                        AdvancePropagation(animate);
                        break;
                    }
                    wave.Propagate();
                    if (animate)
                    {
                        // changedCells : 이번 붕괴(칸 선택)에 의해 후보가 제거된 칸이 몇개 인지
                        // removedOptions : 제거된 총 후보 수
                        changedCells = removedOptions = 0;
                        for (int i = 0; i < cells.Length; i++)
                        {
                            int difference = beforePropagation[i] - wave.Counts[i];
                            if (difference <= 0) continue;
                            changedCells++;
                            removedOptions += difference;
                        }
                    }
                    phase = Phase.Propagate;
                    break;
                case Phase.Ready:
                case Phase.Propagate:
                    if (wave.Contradiction || wave.Complete)
                    {
                        Finish();
                        break;
                    }
                    
                    // 엔트로피 낮은 칸 하나 선택하고 beforePropagation 배열을 wave.Counts 배열로 갱신
                    if (explanationMode)
                    {
                        selected = wave.SelectCell(selectionCandidates);
                        if (selected < 0)
                        {
                            Finish();
                            break;
                        }
                        tileCandidates = wave.CandidatesAt(selected);
                        previewIndex = -1;
                        step++;
                        phase = Phase.SelectCell;
                        break;
                    }
                    selected = wave.Observe();
                    if (animate) System.Array.Copy(wave.Counts, beforePropagation, cells.Length);
                    
                    // 다음 단계로 진행
                    step++;
                    phase = Phase.Collapse;
                    break;
            }
            if (animate) Render();
        }

        private void AdvancePropagation(bool animate)
        {
            if (!animate)
            {
                while (propagation.MoveNext()) { }
            }
            else if (!wave.Contradiction && propagation.MoveNext())
            {
                var removal = propagation.Current;
                removedOptions++;
                if (beforePropagation[removal.Cell] - wave.Counts[removal.Cell] == 1) changedCells++;
                phase = Phase.ExplainPropagation;
                return;
            }

            propagation.Dispose();
            propagation = null;
            phase = Phase.Propagate;
        }

        private string CellName(int cell) => $"{cell + 1}번 칸({cell % width + 1}열 {cell / width + 1}행)";

        private void OnDestroy()
        {
            propagation?.Dispose();
            if (runtimePanelSettings != null) Destroy(runtimePanelSettings);
        }

        private void Finish()
        {
            selected = -1;
            int minimumRoute = Mathf.Min(10, (width + height) / 2);

            // 던전 자동 생성 후 wave.TryFindPlayableArea() 호출해서
            //  입구, 출구, 도달 가능한 칸들 reachableTiles 배열에 담음
            if (!wave.Complete ||
                !wave.TryFindPlayableArea(minimumRoute, out entrance, out exit, out reachableTiles))
            {
                entrance = exit = -1;
                bool initialContradiction = step == 0 && wave.Contradiction;
                phase = attempt < MaxAttempts && !initialContradiction ? Phase.Retry : Phase.NoRoute;
                nextPhase = Time.unscaledTime + (explanationMode ? 3f : 1.4f) / generationSpeed;
                return;
            }
            disconnectedCells = 0;
            for (int cell = 0; cell < reachableTiles.Length; cell++)
                if (reachableTiles[cell] == 0 && wave.TileAt(cell) != 0)
                    disconnectedCells++;


            // 끊긴 통로가 없는 경우 탐험 시작
            if (disconnectedCells == 0 && !explanationMode)
            {
                StartExploring();
                return;
            }

            // 끊긴 통로가 있는 경우 ClearPaths 단계로 진행
            phase = Phase.ClearPaths;
            clearBlinkStep = 0;
            nextPhase = Time.unscaledTime + (explanationMode ? 3f : ClearBlinkInterval) / generationSpeed;
        }

        private void StartExploring()
        {
            player = entrance;
            phase = Phase.Explore;
        }

        private void ReadMovement(Keyboard keyboard)
        {
            if (Time.unscaledTime < nextMove) return;
            int direction = -1;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction = 0;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction = 1;
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction = 2;
            else if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction = 3;
            if (direction < 0) return;
            nextMove = Time.unscaledTime + MoveInterval;
            TryMove(direction);
        }

        private bool TryMove(int direction)
        {
            if (phase != Phase.Explore) return false;
            int destination = wave.Walk(player, direction);
            if (destination < 0) return false;
            player = destination;
            moves++;
            if (player == exit) phase = Phase.Escaped;
            Render();
            return true;
        }
        
        
        // 여기 아래로는 디자인 관련 코드

        private void Render()
        {
            bool playing = phase == Phase.Explore || phase == Phase.Escaped;
            bool flash = phase == Phase.ClearPaths && clearBlinkStep % 2 == 0;
            int resolved = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                int options = wave.Counts[i];
                if (options == 1) resolved++;
                bool changed = (phase == Phase.Propagate || phase == Phase.ExplainPropagation) && options < beforePropagation[i];
                int tile = playing ? reachableTiles[i] : options == 1 ? wave.TileAt(i) : 0;
                bool previewing = phase == Phase.PreviewTile && i == selected;
                if (previewing) tile = tileCandidates[previewIndex];
                bool removing = phase == Phase.ExplainPropagation && i == propagation.Current.Cell;
                if (removing) tile = propagation.Current.Tile;
                string symbol = i == player && phase != Phase.Escaped ? "P" : i == exit ? "E" : i == entrance ? "S" : "";
                cells[i].Draw(options, tile, i == selected && phase != Phase.SelectCell,
                    changed && !explanationMode, symbol, flash && tile != 0 && reachableTiles[i] == 0,
                    phase == Phase.SelectCell && selectionCandidates.Contains(i), previewing, changed && explanationMode, removing,
                    explanationMode && i == selected && options == 1 && !playing);
            }

            stats.text = playing
                ? $"시드 {seed}  |  이동 {moves:00}회"
                : $"시드 {seed}  |  {cells.Length}칸 중 {resolved}칸 확정";
            controls.text = playing
                ? "WASD / 방향키  이동  |  R  새 던전"
                : phase == Phase.NoRoute ? "R  다시 시작  |  설정  조건 변경"
                : "Space:일시 정지·재개  |  N:한 단계 진행, 꾹 누르면 빠른 진행  |  F:건너뛰기  |  R:다시 시작";

            string title = phase switch
            {
                Phase.Ready => "01 / 초기 후보",
                Phase.SelectCell or Phase.InspectCell =>
                    "02 / 후보군이 적은 칸 중 관찰할 칸을 무작위로 뽑습니다",
                Phase.PreviewTile or Phase.Collapse =>
                    "03 / 관찰할 칸에 들어올 수 있는 후보들 중 하나를 선택합니다.",
                Phase.ExplainPropagation or Phase.Propagate =>
                    "04 / 관찰된 칸에 들어온 타일에 의해 인접한 칸에 들어올 수 없는 후보군을 제거합니다.",
                Phase.ClearPaths => "05 / 입구와 연결되지 않은 경로를 정리합니다.",
                Phase.Explore => "06 / 연결된 통로를 따라 출구를 찾으세요.",
                Phase.Escaped => $"탈출 성공 / {moves}번 이동했습니다.",
                Phase.Retry => $"후보 또는 경로 조건을 만족하지 못해 다시 생성합니다. ({attempt}/{MaxAttempts})",
                _ => "던전 생성 실패 / 설정을 바꾸거나 다시 시도하세요."
            };
            stage.text = paused && !playing && phase != Phase.NoRoute ? title + "  [일시 정지]" : title;
        }


        private sealed class CellView
        {
            internal readonly VisualElement Root = new();
            private readonly VisualElement floor;
            private readonly VisualElement[] passages = new VisualElement[4];
            private readonly Label marker = new();

            internal CellView(float size, string cellNumber = null)
            {
                Root.AddToClassList("cell");
                Root.style.width = size;
                Root.style.height = size;
                floor = Piece("floor");
                string[] directions = { "north", "east", "south", "west" };
                for (int i = 0; i < passages.Length; i++)
                    passages[i] = Piece("passage-" + directions[i]);
                marker.AddToClassList("cell-label");
                marker.style.fontSize = Mathf.Min(15f, size * 0.6f);
                Root.Add(marker);
                if (cellNumber != null)
                {
                    Root.AddToClassList("cell-numbered");
                    var number = new Label("#" + cellNumber);
                    number.AddToClassList("cell-number");
                    number.style.fontSize = Mathf.Min(8f, size * 0.25f);
                    Root.Add(number);
                }
            }

            private VisualElement Piece(string styleClass)
            {
                var piece = new VisualElement();
                piece.AddToClassList("floor-piece");
                piece.AddToClassList(styleClass);
                Root.Add(piece);
                return piece;
            }

            internal void Draw(int options, int tile, bool selected, bool changed, string symbol, bool flashingCell,
                bool eligible = false, bool preview = false, bool pruned = false, bool removing = false, bool confirmed = false)
            {
                Root.EnableInClassList("cell-fixed", options == 1);
                Root.EnableInClassList("cell-eligible", eligible);
                Root.EnableInClassList("cell-selected", selected);
                Root.EnableInClassList("cell-confirmed", confirmed);
                Root.EnableInClassList("cell-changed", changed);
                Root.EnableInClassList("cell-pruned", pruned);
                Root.EnableInClassList("cell-clearing", flashingCell);
                floor.style.display = tile == 0 && !removing ? DisplayStyle.None : DisplayStyle.Flex;
                floor.EnableInClassList("piece-clearing", flashingCell);
                floor.EnableInClassList("piece-removing", removing);
                floor.EnableInClassList("piece-empty", removing && tile == 0);
                for (int i = 0; i < passages.Length; i++)
                {
                    passages[i].style.display = DungeonWave.HasEdge(tile, i) ? DisplayStyle.Flex : DisplayStyle.None;
                    passages[i].EnableInClassList("piece-clearing", flashingCell);
                    passages[i].EnableInClassList("piece-removing", removing);
                }
                marker.text = preview || removing ? "" : options == 1 ? symbol : options.ToString();
                marker.EnableInClassList("candidate-count", options > 1 && !removing);
                marker.EnableInClassList("player", symbol == "P");
                marker.EnableInClassList("exit", symbol == "E");
                marker.EnableInClassList("entrance", symbol == "S");
            }
        }
    }
}
