// Adapted from WaveFunctionCollapse/Model.cs by Maxim Gumin (2016).
// Distributed under the MIT license. See WFC-LICENSE.txt.
using System;
using System.Collections.Generic;

namespace WfcPrototype
{
    internal sealed class DungeonWave
    {
        // 타일 종류 수
        private const int TileCount = 16;

        // n방향 내 타일끼리 나눠가지는 가중치 : 예를들어 2방향의 경우 6종류인데 
        // 직선 2개는 각각 9개 나머지는 6이므로 직선이 모퉁이보다 1.5배 자주 등장 
        private static readonly double[] ShapeWeights = { 4, 2, 2, 6, 2, 9, 6, 2, 2, 6, 9, 2, 6, 2, 2, 1 };
        // x방향
        private static readonly int[] Dx = { 0, 1, 0, -1 };
        // y방향
        private static readonly int[] Dy = { -1, 0, 1, 0 };
        // 시드설정용 random 객체
        private readonly Random random;

        // 현재 칸에 타일 놓을 수 있는가?
        private readonly bool[,] wave; 

        // cell : 현재 칸
        // tile : 타일 종류(1,2,4,8 : 각각 북동남서로 1+2+4 면 북동남으로 열린 타일인것임)
        // (cell,tile,edge) : 현재 칸(cell)의 (tile)번 타일에서 (edge)번 방향으로 붙을 수 있는 타일이 몇개인지
        private readonly int[,,] compatible;

        //(edge, tile, index) : (tile)번 타일의 (edge)방향으로 올 수 있는 후보들 표
        // ex) 어떤 타일(tile)의 동쪽(edge)에 붙을 수 있는 타일은 서쪽이 열린 8개 타일
        private readonly int[,,] neighborsByTile = new int[4, TileCount, TileCount / 2]; 

        // 이미 제거됐지만 아직 이웃에 전파하지 않은 (칸, 타일) 쌍
        private readonly Stack<(int Cell, int Tile)> removed = new();

        // 타일별 가중치 : 플레이어 입력으로부터 계산한 타일별 가중치
        private readonly double[] weights = new double[TileCount];
        
        // 타일별 w * log(w) 적용한 가중치
        private readonly double[] weightLogWeights = new double[TileCount];
        
        // 각 칸별 남은 후보의 가중치 합 : 확률 계산할때 각 가중치 나누는 데에 쓰임
        private readonly double[] sumsOfWeights;

        // 각 칸별 남은 후보의 w * log(w) 적용한 가중치 합
        private readonly double[] sumsOfWeightLogWeights;

        // 각 칸의 현재 불확실성 : 낮을수록 우선적으로 선택되는 척도
        private readonly double[] entropies;

        // 아직 후보가 하나로 확정되지 않은 칸 수
        private int unresolved;

        internal readonly int Width;
        internal readonly int Height;

        // 각 칸에 남은 타일 수, 화면에 보이는 숫자 그자체
        internal readonly int[] Counts; 
        internal bool Complete => unresolved == 0 && !Contradiction;

        // 후보가 0이 되어 모순이 되었는지, 모순되면 맵 생성 멈추고 다시 생성시도함
        internal bool Contradiction { get; private set; }

        // 생성자 : 가로,세로,시드, 열린 방향 수별(0~4) 확률 입력받음(플레이어 입력값)
        internal DungeonWave(int width, int height, int seed, int[] directionChances)
        {
            Width = width;
            Height = height;
            random = new Random(seed);
            int cells = width * height;

            // 각 칸(cell)에 (tile)번 타일 놓을 수 있는지
            wave = new bool[cells, TileCount]; 

            // 각 칸(cell)에 (tile)번 타일에서 (edge)번 방향으로 
            // 붙을 수 있는 타일이 몇개인지
            compatible = new int[cells, TileCount, 4];

            // 각 칸(cell)에 남은 후보의 수, 화면에 보이는 숫자 그자체 
            Counts = new int[cells];

            // sumofweights ~ entropies : 
            // 각 칸의 엔트로피 관련 초기화 -> 나중에 확률 계산할때 쓰임
            sumsOfWeights = new double[cells];
            sumsOfWeightLogWeights = new double[cells];
            entropies = new double[cells];
            
            // 모든 칸(cell)이 불확실함
            unresolved = cells;

            // tile : 타일 모양의 번호
            // edge : 타일의 방향(0~3)
            // directionChances : 플레이어 입력값으로 들어온 열린 방향 수별 확률

            // directionsPerTile : 각 타일별 열린 방향 수
            var directionsPerTile = new int[TileCount];

            // shapeTotals : 열린 방향 수별 타일 가중치 합
            var shapeTotals = new double[directionChances.Length];
            
            for (int tile = 0; tile < TileCount; tile++)
            {
                for (int edge = 0; edge < 4; edge++)
                {
                    if (HasEdge(tile, edge)) directionsPerTile[tile]++;
                    int index = 0;
                    for (int neighbor = 0; neighbor < TileCount; neighbor++)
                        if (HasEdge(tile, edge) == HasEdge(neighbor, (edge + 2) % 4))
                            neighborsByTile[edge, tile, index++] = neighbor;
                }
                shapeTotals[directionsPerTile[tile]] += ShapeWeights[tile];
            } // 각 타일의 열린 방향을 세고 각 타일별로 edge 방향으로 붙을 수 있는 
            // 후보들을 neighborsByTile에 저장함

            double totalWeight = 0;
            double totalWeightLogWeight = 0;
            for (int tile = 0; tile < TileCount; tile++)
            {
                int directions = directionsPerTile[tile];
                double weight = directionChances[directions] * ShapeWeights[tile] / shapeTotals[directions];
                weights[tile] = weight;
                weightLogWeights[tile] = weight > 0 ? weight * Math.Log(weight) : 0;
                totalWeight += weight;
                totalWeightLogWeight += weightLogWeights[tile];
            }// 각 타일별 가중치 계산, 열린 방향 수별 확률(플레이어 입력값)과 
            //shapeWeights를 곱하고 shapeTotals로 나눠서 각 타일별 가중치 계산

            for (int cell = 0; cell < cells; cell++)
            {
                Counts[cell] = TileCount;
                sumsOfWeights[cell] = totalWeight;
                sumsOfWeightLogWeights[cell] = totalWeightLogWeight;
                UpdateEntropy(cell);
                for (int tile = 0; tile < TileCount; tile++)
                {
                    wave[cell, tile] = true;
                    for (int edge = 0; edge < 4; edge++)
                        compatible[cell, tile, edge] = TileCount / 2;
                }
            }// 각 칸별로 모든 타일을 놓을 수 있도록 초기화,
            // 각 칸별로 모든 타일이 edge 방향으로 붙을 수 있는 후보 수를 
            // TileCount/2로 초기화

            for (int cell = 0; cell < cells; cell++)
            {
                for (int tile = 0; tile < TileCount; tile++)
                    if (weights[tile] == 0) Ban(cell, tile);

                for (int edge = 0; edge < 4; edge++)
                    if (Neighbor(cell, edge) < 0)
                        for (int tile = 0; tile < TileCount; tile++)
                            if (HasEdge(tile, edge)) Ban(cell, tile);
            } // 가중치가 0인 타일을 모든 칸에서 제거, 경계선에 있는 타일도 제거

            Propagate();// 제거된 후보들이 이웃칸에 미치는 영향 전파시킴
        }

        // selected : 엔트로피가 가장 낮은 칸
        internal int Observe()
        {
            int selected = SelectCell();
            if (selected >= 0) CollapseCell(selected);
            return selected;
        }

        internal int SelectCell(List<int> candidates = null)
        {
            int selected = -1;
            double lowest = double.PositiveInfinity;
            double minimumEntropy = double.PositiveInfinity;
            for (int cell = 0; cell < Counts.Length; cell++)
            {
                if (Counts[cell] <= 1) continue;
                minimumEntropy = Math.Min(minimumEntropy, entropies[cell]);
                // 최솟값 중에서도 랜덤으로 선택되도록
                double entropy = entropies[cell] + 1e-6 * random.NextDouble();
                if (entropy >= lowest) continue;
                lowest = entropy;
                selected = cell;
            }

            // 못찾으면 -1 반환
            if (candidates != null)
            {
                candidates.Clear();
                for (int cell = 0; cell < Counts.Length; cell++)
                    if (Counts[cell] > 1 && entropies[cell] <= minimumEntropy + 1e-6)
                        candidates.Add(cell);
            }
            return selected;
        }

        internal int CollapseCell(int selected)
        {
            // 후보가 가장 적은 칸에서 0~sumofweights[selected] 사이의 랜덤값을 뽑고,
            // 그 랜덤값이 속하는 후보를 선택함
            // 만약 2,5,7번 후보가 남아있고 sumofweights[selected] = 10이라면
            // 0~10 사이의 랜덤값이 4.6이면 뺐을때 처음으로 음수가 되는 후보인 5번 후보를 선택함
            double remaining = random.NextDouble() * sumsOfWeights[selected];
            int chosen = -1;
            for (int tile = 0; tile < TileCount; tile++)
            {
                if (!wave[selected, tile]) continue;
                chosen = tile;
                remaining -= weights[tile];
                if (remaining < 0) break;
            }
            for (int tile = 0; tile < TileCount; tile++)
                if (tile != chosen) Ban(selected, tile);
            // 선택되지 못한 후보들은 모두 제거됨, 제거된 후보들은 removed에 쌓임

            return chosen;
        }

        // removed에 쌓인 후보들을 이웃칸에 전파시켜 상태 갱신하는 함수
        internal void Propagate()
        {
            foreach (var removal in PropagateSteps()) { }
        }

        internal IEnumerable<CandidateRemoval> PropagateSteps()
        {
            // 설명 모드에서는 제거 한 건마다 멈추고 같은 열거자로 다음 작업을 이어감.
            while (removed.Count > 0 && !Contradiction)
            {
                // 우선 removed에 쌓인 후보들을 하나씩 꺼내봄
                var (cell, tile) = removed.Pop();
                for (int edge = 0; edge < 4; edge++)
                {
                    // cell번 칸의 edge 방향 이웃칸의 인덱스
                    int neighbor = Neighbor(cell, edge);
                    if (neighbor < 0) continue;
                    for (int index = 0; index < TileCount / 2; index++)
                    {
                        // (tile)번 타일의 (edge)방향으로 붙을 수 있는 후보들 중 하나를 가져옴
                        int candidate = neighborsByTile[edge, tile, index];
                        // 제거된 후보의 이웃인 neighbor번 칸에 대해서 (tile)번 타일의 (edge)방향으로 
                        // 붙을 수 있는 후보가 몇개인지 compatible에서 1 감소시키고, 결국 0이 된다면 
                        // neighbor번 칸의 candidate번 타일을 제거함
                        if (wave[neighbor, candidate] && --compatible[neighbor, candidate, edge] == 0)
                        {
                            Ban(neighbor, candidate);
                            yield return new CandidateRemoval(cell, tile, edge, neighbor, candidate);
                        }
                    }
                }
            }
        }

        internal readonly struct CandidateRemoval
        {
            internal readonly int SourceCell, SourceTile, Edge, Cell, Tile;

            internal CandidateRemoval(int sourceCell, int sourceTile, int edge, int cell, int tile)
            {
                SourceCell = sourceCell;
                SourceTile = sourceTile;
                Edge = edge;
                Cell = cell;
                Tile = tile;
            }
        }

        internal int[] CandidatesAt(int cell)
        {
            var candidates = new List<int>();
            for (int tile = 0; tile < TileCount; tile++)
                if (wave[cell, tile]) candidates.Add(tile);
            return candidates.ToArray();
        }

        internal double EntropyAt(int cell) => entropies[cell];

        internal double ProbabilityOf(int cell, int tile) =>
            wave[cell, tile] && sumsOfWeights[cell] > 0 ? weights[tile] / sumsOfWeights[cell] : 0;

        // (cell, tile)쌍을 제거하는 함수
        private void Ban(int cell, int tile)
        {
            if (!wave[cell, tile]) return;
            wave[cell, tile] = false;
            removed.Push((cell, tile));
            Counts[cell]--;
            if (Counts[cell] == 1) unresolved--;
            // 후보가 0이 되어 모순발생, 맵 생성 멈추고 다시 생성시도함
            if (Counts[cell] == 0) Contradiction = true; 
            sumsOfWeights[cell] -= weights[tile];
            sumsOfWeightLogWeights[cell] -= weightLogWeights[tile];
            UpdateEntropy(cell);
        }

        // 각 칸별 엔트로피를 갱신해주는 계산함수
        private void UpdateEntropy(int cell)
        {
            double sum = sumsOfWeights[cell];
            entropies[cell] = sum > 0 ? Math.Log(sum) - sumsOfWeightLogWeights[cell] / sum : 0;
        } 

        // (cell)번 칸에 남은 후보 중 하나를 반환, 후보가 없으면 0(빈칸) 반환
        internal int TileAt(int cell)
        {
            for (int tile = 0; tile < TileCount; tile++)
                if (wave[cell, tile]) return tile;
            return 0;
        }

        // (tile)번째 타일이 edge 방향으로 열린 타일인가?
        internal static bool HasEdge(int tile, int edge) => (tile & (1 << edge)) != 0;

        // (cell)번 칸의 (edge)방향의 이웃 칸의 인덱스를 반환, 경계선이면 -1 반환
        internal int Neighbor(int cell, int edge)
        {
            int x = cell % Width + Dx[edge];
            int y = cell / Width + Dy[edge];
            return x >= 0 && x < Width && y >= 0 && y < Height ? x + y * Width : -1;
        }

        // (cell)번 칸의 (edge)방향으로 붙을 수 있는 타일이 없으면 -1 반환,
        // 있으면 다음으로 이동할 칸(next) 계산
        internal int Walk(int cell, int edge)
        {
            // 현재 칸(cell)의 (edge)방향으로 붙을 수 있는 타일이 없으면 -1 반환
            if (!HasEdge(TileAt(cell), edge)) return -1;
            // 다음으로 이동할 칸(next) 계산, next가 경계선이면 -1 반환
            int next = Neighbor(cell, edge);
            return next >= 0 && HasEdge(TileAt(next), (edge + 2) % 4) ? next : -1;
        }

        // minimumLength : 시작-끝까지 최소길이
        // start : 시작칸, exit : 끝칸, reachableTiles : 시작칸에서 도달 가능한 칸들 담은 배열, 
        internal bool TryFindPlayableArea(int minimumLength, out int start, out int exit, out int[] reachableTiles)
        {
            // bfs에 사용할 visited, distances, queue 배열 초기화
            var visited = new bool[Counts.Length];
            var distances = new int[Counts.Length];
            var queue = new Queue<int>();
            start = exit = -1;
            int longest = 0;

            for (int cell = 0; cell < Counts.Length; cell++)
            {
                // 이미 방문했거나 (cell)번 칸이 빈칸이면 건너뜀
                if (visited[cell] || TileAt(cell) == 0) continue;
                // (cell)번 칸에서 가장 먼 칸을 찾고 그 칸에서 가장 먼 칸 찾기
                // 이렇게 하면 특정 칸에서 가장 먼 칸을 우선 찾고
                // 그 칸에서 다시 먼 칸을 찾으면 더 거리가 긴 경로를 찾을 수 있음.
                int firstEnd = FarthestFrom(cell);
                int otherEnd = FarthestFrom(firstEnd);

                if (distances[otherEnd] <= longest) continue;
                longest = distances[otherEnd];
                start = firstEnd;
                exit = otherEnd;
            }// 가장 먼 (시작, 끝) 짝을 찾는 로직
            if (start < 0 || longest < minimumLength)
            {
                reachableTiles = Array.Empty<int>();
                return false;
            } // start가 -1이면 도달 가능한 칸이 없다는 뜻, longest가 minimumLength보다 작으면
              // 최소길이보다 짧으므로 찾지 못한것임

            // 이동 가능한 경로 찾은 경우임
            // farthestFrom(start) : distance 배열을 최종 선택된 start에서 
            // 가장 먼 칸까지의 거리로 갱신, reachableTiles 배열에 타일 담아서 전달해줌
            // reachableTiles 매개변수 앞에 out 붙여서 반환값으로 전달해줌
            FarthestFrom(start);
            reachableTiles = new int[Counts.Length];
            for (int cell = 0; cell < Counts.Length; cell++)
                if (distances[cell] >= 0) reachableTiles[cell] = TileAt(cell);
            return true;

            // bfs : 한칸에서 시작해서 가장 먼칸을 찾는 함수
            int FarthestFrom(int from)
            {
                Array.Fill(distances, -1);
                distances[from] = 0;
                queue.Enqueue(from);
                int farthest = from;
                while (queue.Count > 0)
                {
                    farthest = queue.Dequeue();
                    visited[farthest] = true;
                    for (int edge = 0; edge < 4; edge++)
                    {
                        int next = Walk(farthest, edge);
                        if (next < 0 || distances[next] >= 0) continue;
                        distances[next] = distances[farthest] + 1;
                        queue.Enqueue(next);
                    }
                }
                return farthest;
            }
        }
    }
}
