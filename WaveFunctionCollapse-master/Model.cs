// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;

abstract class Model
{
    // DW(DungeonWave.cs)의 wave와 동일
    protected bool[][] wave;

    // DW의 neighborsByTile과 동일, 원본은 SimpleTileModel이나
    //  OverlappingModel에서 만들어둠 
    protected int[][][] propagator;

    // DW의 compatible과 동일
    int[][][] compatible;

    // (ceil)번 칸에 들어가는 (tile) 번호
    protected int[] observed;

    // DW의 removed와 같음
    (int, int)[] stack;

    // stacksize : 스택 크기, 이 코드에서는 스택을 직접 구현했음
    int stacksize, observedSoFar;

    // MX : 출력 이미지의 가로 길이, MY : 출력 이미지의 세로 길이
    // T : tile 종류 수, N : 패턴의 한 변 크기
    protected int MX, MY, T, N;

    // 맵의 반대쪽 경계와 이어지는지
    protected bool periodic, ground;

    // tile의 가중치, tile의 종류 수와 동일한 길이가지는 배열
    protected double[] weights;

    // tile의 로그 가중치
    double[] weightLogWeights, distribution;

    // 각 칸에 남은 타일 수, DW의 Counts와 동일
    protected int[] sumsOfOnes;
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    protected double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    // 다음에 관찰할 칸을 선택하는 방식
    // Entropy : 불확실도가 가장 낮은 칸 선택
    // MRV : 남은 타일 수가 가장 적은 칸 선택
    // Scanline : 왼쪽 위에서 오른쪽 아래로 순차적으로 선택
    public enum Heuristic { Entropy, MRV, Scanline };
    Heuristic heuristic;

    // 각종 멤버 변수 초기화 
    protected Model(int width, int height, int N, bool periodic, Heuristic heuristic)
    {
        MX = width;
        MY = height;
        this.N = N;
        this.periodic = periodic;
        this.heuristic = heuristic;
    }

    // 각종 멤버 변수 초기화2
    void Init()
    {
        wave = new bool[MX * MY][];
        compatible = new int[wave.Length][][];
        for (int i = 0; i < wave.Length; i++)
        {
            wave[i] = new bool[T];
            compatible[i] = new int[T][];
            for (int t = 0; t < T; t++) compatible[i][t] = new int[4];
        }
        distribution = new double[T];
        observed = new int[MX * MY];

        weightLogWeights = new double[T];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        for (int t = 0; t < T; t++)
        {
            weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
            sumOfWeights += weights[t];
            sumOfWeightLogWeights += weightLogWeights[t];
        }

        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        sumsOfOnes = new int[MX * MY];
        sumsOfWeights = new double[MX * MY];
        sumsOfWeightLogWeights = new double[MX * MY];
        entropies = new double[MX * MY];

        stack = new (int, int)[wave.Length * T];
        stacksize = 0;
    }

    public bool Run(int seed, int limit)
    {
        if (wave == null) Init();

        Clear();
        Random random = new(seed);

        for (int l = 0; l < limit || limit < 0; l++)
        {
            int node = NextUnobservedNode(random);
            if (node >= 0)
            {
                Observe(node, random);
                bool success = Propagate();
                if (!success) return false;
            }
            else
            {
                for (int i = 0; i < wave.Length; i++) for (int t = 0; t < T; t++) if (wave[i][t]) { observed[i] = t; break; }
                return true;
            }
        }

        return true;
    }

    // 다음으로 관찰할 칸을 선택하는 함수, DW와 달리 함수를 따로 분리해서 구현함
    int NextUnobservedNode(Random random)
    {
        if (heuristic == Heuristic.Scanline)
        {
            for (int i = observedSoFar; i < wave.Length; i++)
            {
                if (!periodic && (i % MX + N > MX || i / MX + N > MY)) continue;
                if (sumsOfOnes[i] > 1)
                {
                    observedSoFar = i + 1;
                    return i;
                }
            }
            return -1;
        }

        double min = 1E+4;
        int argmin = -1;
        for (int i = 0; i < wave.Length; i++)
        {
            if (!periodic && (i % MX + N > MX || i / MX + N > MY)) continue;
            int remainingValues = sumsOfOnes[i];
            double entropy = heuristic == Heuristic.Entropy ? entropies[i] : remainingValues;
            if (remainingValues > 1 && entropy <= min)
            {
                double noise = 1E-6 * random.NextDouble();
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    argmin = i;
                }
            }
        }
        return argmin;
    }

    // DW의 Obeserve 함수와 같이 특정 칸의 tile을 선택하는 함수
    void Observe(int node, Random random)
    {
        bool[] w = wave[node];
        for (int t = 0; t < T; t++) distribution[t] = w[t] ? weights[t] : 0.0;
        int r = distribution.Random(random.NextDouble());
        for (int t = 0; t < T; t++) if (w[t] != (t == r)) Ban(node, t);
    }

    // 스택에서 제거 기록을 꺼내서 인접한 칸의 compatible을 갱신하고
    //  만약 compatible이 0이 되면 Ban() 호출
    bool Propagate()
    {
        while (stacksize > 0)
        {
            (int i1, int t1) = stack[stacksize - 1];
            stacksize--;

            int x1 = i1 % MX;
            int y1 = i1 / MX;

            for (int d = 0; d < 4; d++)
            {
                int x2 = x1 + dx[d];
                int y2 = y1 + dy[d];
                if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY)) continue;

                if (x2 < 0) x2 += MX;
                else if (x2 >= MX) x2 -= MX;
                if (y2 < 0) y2 += MY;
                else if (y2 >= MY) y2 -= MY;

                int i2 = x2 + y2 * MX;
                int[] p = propagator[d][t1];
                int[][] compat = compatible[i2];

                for (int l = 0; l < p.Length; l++)
                {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    comp[d]--;
                    if (comp[d] == 0) Ban(i2, t2);
                }
            }
        }

        return sumsOfOnes[0] > 0;
    }

    // i번칸에 t번 tile을 배치할 수 없도록 wave를 갱신하고
    //  compatible을 갱신하고 stack에 기록
    void Ban(int i, int t)
    {
        wave[i][t] = false;

        int[] comp = compatible[i][t];
        for (int d = 0; d < 4; d++) comp[d] = 0;
        stack[stacksize] = (i, t);
        stacksize++;

        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= weights[t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[t];

        double sum = sumsOfWeights[i];
        entropies[i] = Math.Log(sum) - sumsOfWeightLogWeights[i] / sum;
    }

    // 새로운 맵 생성전에 각종 변수를 초기화하고
    // 불가능한 후보를 미리 제거해놓음
    void Clear()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            for (int t = 0; t < T; t++)
            {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
            }

            sumsOfOnes[i] = weights.Length;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
            entropies[i] = startingEntropy;
            observed[i] = -1;
        }
        observedSoFar = 0;

        for (int y = 0; y < MY; y++) for (int x = 0; x < MX; x++)
        {
            if (!periodic && (x + N > MX || y + N > MY)) continue;

            int i = x + y * MX;
            for (int t = 0; t < T; t++)
            {
                bool noNeighborsRight = (periodic || x < MX - N) && propagator[2][t].Length == 0;
                bool noNeighborsTop = (periodic || y > 0) && propagator[3][t].Length == 0;
                bool noNeighborsLeft = (periodic || x > 0) && propagator[0][t].Length == 0;
                bool noNeighborsBottom = (periodic || y < MY - N) && propagator[1][t].Length == 0;

                if (noNeighborsRight || noNeighborsTop || noNeighborsLeft || noNeighborsBottom) Ban(i, t);
            }
        }

        if (ground)
        {
            for (int x = 0; x < MX; x++)
            {
                int bottom = x + (MY - 1) * MX;
                for (int t = 0; t < T - 1; t++) if (wave[bottom][t]) Ban(bottom, t);
                for (int y = 0; y < MY - 1; y++)
                {
                    int i = x + y * MX;
                    if (wave[i][T - 1]) Ban(i, T - 1);
                }
            }
        }

        if (stacksize > 0) Propagate();
    }

    public abstract void Save(string filename);

    protected static int[] dx = { -1, 0, 1, 0 };
    protected static int[] dy = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };
}
