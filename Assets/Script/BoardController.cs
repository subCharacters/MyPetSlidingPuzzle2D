using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    private GridLayoutGroup _grid;

    [Header("Puzzle Settings")]
    public int rows = 3;
    public int cols = 3;
    public string imageName = "atti";
    [Range(10, 1000)] public int shuffleMoves = 200;

    private TileView[] tiles;   // ✅ 이제 빈칸 포함하여 모두 TileView가 존재
    private int emptyIndex = -1;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        if (_grid == null)
            Debug.LogError("[BoardController] GridLayoutGroup 없음 - BoardRoot에 추가하세요.");
    }

    private void Start()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("[BoardController] tilePrefab 미할당 - Inspector에서 Tile.prefab 드래그하세요.");
            return;
        }

        Sprite source = Resources.Load<Sprite>(imageName);
        if (source == null)
        {
            Debug.LogError($"[BoardController] 이미지 {imageName} 로드 실패");
            return;
        }

        CreatePuzzleWithEmpty(source);
        StartCoroutine(ShuffleRandomWalk());
    }

    private void CreatePuzzleWithEmpty(Sprite source)
    {
        // 자식 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        Texture2D tex = source.texture;
        int total = rows * cols;
        int pieceW = tex.width / cols;
        int pieceH = tex.height / rows;

        tiles = new TileView[total];
        emptyIndex = total - 1; // 마지막 칸을 빈칸

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                var obj = Instantiate(tilePrefab, transform);
                var tile = obj.GetComponent<TileView>();

                tile.correctIndex = idx;
                tile.currentIndex = idx;

                if (idx == emptyIndex)
                {
                    tile.SetEmpty(true);        // ✅ 빈칸으로 표시(이미지 off)
                }
                else
                {
                    // 상하 보정: Unity 텍스처 좌표는 좌하(0,0) 기준
                    Rect rect = new Rect(c * pieceW, tex.height - (r + 1) * pieceH, pieceW, pieceH);
                    var piece = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                    tile.SetImage(piece);
                    tile.SetEmpty(false);
                }

                tiles[idx] = tile;
            }
        }
    }

    private IEnumerator ShuffleRandomWalk()
    {
        // ✅ "빈칸과 인접한 실제 타일"만 랜덤 스왑 → 항상 해답 존재
        var neighbors = new List<int>();
        for (int m = 0; m < shuffleMoves; m++)
        {
            neighbors.Clear();
            foreach (var nb in GetNeighbors(emptyIndex))
                if (!tiles[nb].isEmpty) neighbors.Add(nb);

            if (neighbors.Count > 0)
            {
                int pick = neighbors[Random.Range(0, neighbors.Count)];
                SwapTiles(pick, emptyIndex);
            }
            yield return null;
        }
    }

    private IEnumerable<int> GetNeighbors(int cell)
    {
        int r = cell / cols, c = cell % cols;
        if (r > 0) yield return (r - 1) * cols + c;     // 위
        if (r < rows - 1) yield return (r + 1) * cols + c;     // 아래
        if (c > 0) yield return r * cols + (c - 1);     // 왼
        if (c < cols - 1) yield return r * cols + (c + 1);     // 오른
    }

    // ✅ 스와이프 입력: dir = (±1,0) or (0,±1)
    public void TrySwipeMove(TileView tile, Vector2 dir)
    {
        int from = tile.currentIndex;
        int target = GetNeighborByDirection(from, dir);
        Debug.Log($"[Board] Swipe from={from} dir={dir} -> target={target}, emptyIndex={emptyIndex}");

        if (target < 0) return;

        var targetTile = tiles[target];
        if (targetTile == null)
        {
            Debug.LogError("[Board] targetTile null - 생성 로직 확인 필요");
            return;
        }

        if (targetTile.isEmpty)
        {
            Debug.Log("[Board] swap OK (into empty)");
            SwapTiles(from, target);   // 타일을 빈칸으로 이동
        }
        else
        {
            Debug.Log("[Board] blocked (not empty)");
        }
    }

    private int GetNeighborByDirection(int cell, Vector2 dir)
    {
        int r = cell / cols, c = cell % cols;

        if (Mathf.Abs(dir.x) > 0.5f) // 좌우 스와이프
        {
            int nc = c + (dir.x > 0 ? 1 : -1);
            if (nc < 0 || nc >= cols) return -1;
            return r * cols + nc;
        }
        else                         // 상하 스와이프
        {
            // 화면 상단으로 스와이프(dir.y>0) → 행 -1 (위쪽 칸)
            int nr = r + (dir.y > 0 ? -1 : 1);
            if (nr < 0 || nr >= rows) return -1;
            return nr * cols + c;
        }
    }

    // ✅ 두 칸 교환(타일↔빈칸 포함)
    private void SwapTiles(int a, int b)
    {
        if (a == b) return;

        var A = tiles[a];
        var B = tiles[b];

        // 시각적 순서 교환 (GridLayoutGroup는 siblingIndex 순서대로 배치)
        int sibA = A.transform.GetSiblingIndex();
        int sibB = B.transform.GetSiblingIndex();
        A.transform.SetSiblingIndex(sibB);
        B.transform.SetSiblingIndex(sibA);

        // 상태 교환
        tiles[a] = B;
        tiles[b] = A;

        int tmp = A.currentIndex;
        A.currentIndex = b;
        B.currentIndex = a;

        // 빈칸 위치 갱신
        if (A.isEmpty) emptyIndex = b;
        else if (B.isEmpty) emptyIndex = a;
    }
}
