using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private UIHud hud;   // TopBar HUD 연결

    private GridLayoutGroup _grid;

    [Header("Puzzle Settings")]
    public int rows = 3;
    public int cols = 3;
    public string imageName = "atti";
    [Range(10, 1000)] public int shuffleMoves = 200;

    private TileView[] tiles;        // 전체 타일 (빈칸 포함)
    private Sprite[] pieceSprites;   // 잘라낸 이미지 캐시
    private int emptyIndex = -1;

    private int moveCount = 0;
    private bool isSolved = false;
    private bool isShuffling = false;
    private bool inputLocked = false;

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
        // 기존 자식 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        Texture2D tex = source.texture;
        int total = rows * cols;
        int pieceW = tex.width / cols;
        int pieceH = tex.height / rows;

        tiles = new TileView[total];
        pieceSprites = new Sprite[total];
        emptyIndex = total - 1;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                var obj = Instantiate(tilePrefab, transform);
                var tile = obj.GetComponent<TileView>();

                tile.correctIndex = idx;
                tile.currentIndex = idx;

                Rect rect = new Rect(c * pieceW, tex.height - (r + 1) * pieceH, pieceW, pieceH);
                var piece = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                pieceSprites[idx] = piece;   // ✅ 스프라이트 캐시

                if (idx == emptyIndex)
                {
                    tile.SetEmpty(true);     // 마지막 칸은 빈칸
                }
                else
                {
                    tile.SetImage(piece);
                    tile.SetEmpty(false);
                }

                tiles[idx] = tile;
            }
        }
    }

    private IEnumerator ShuffleRandomWalk()
    {
        isShuffling = true;
        isSolved = false;
        moveCount = 0;

        var neighbors = new List<int>();
        for (int m = 0; m < shuffleMoves; m++)
        {
            neighbors.Clear();
            foreach (var nb in GetNeighbors(emptyIndex))
                if (!tiles[nb].isEmpty) neighbors.Add(nb);

            if (neighbors.Count > 0)
            {
                int pick = neighbors[Random.Range(0, neighbors.Count)];
                SwapTiles(pick, emptyIndex, countMove: false, doCheck: false);
            }
            yield return null;
        }

        isShuffling = false;

        // 셔플 후 정답이면 다시 섞기
        if (IsSolvedNow())
        {
            for (int i = 0; i < Mathf.Max(20, rows * cols); i++)
            {
                neighbors.Clear();
                foreach (var nb in GetNeighbors(emptyIndex))
                    if (!tiles[nb].isEmpty) neighbors.Add(nb);
                if (neighbors.Count > 0)
                {
                    int pick = neighbors[Random.Range(0, neighbors.Count)];
                    SwapTiles(pick, emptyIndex, countMove: false, doCheck: false);
                }
            }
        }

        moveCount = 0;
        isSolved = IsSolvedNow();
        if (hud != null) hud.UpdateMoveText(moveCount);
    }

    private IEnumerable<int> GetNeighbors(int cell)
    {
        int r = cell / cols, c = cell % cols;
        if (r > 0) yield return (r - 1) * cols + c;       // 위
        if (r < rows - 1) yield return (r + 1) * cols + c; // 아래
        if (c > 0) yield return r * cols + (c - 1);       // 왼쪽
        if (c < cols - 1) yield return r * cols + (c + 1); // 오른쪽
    }

    public void TrySwipeMove(TileView tile, Vector2 dir)
    {
        if (isShuffling || inputLocked || isSolved) return;

        int from = tile.currentIndex;
        int target = GetNeighborByDirection(from, dir);
        if (target < 0) return;

        if (tiles[target].isEmpty)
        {
            SwapTiles(from, target); // 기본값: 카운트/체크 O
        }
    }

    private int GetNeighborByDirection(int cell, Vector2 dir)
    {
        int r = cell / cols, c = cell % cols;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            int nc = c + (dir.x > 0 ? 1 : -1);
            if (nc < 0 || nc >= cols) return -1;
            return r * cols + nc;
        }
        else
        {
            int nr = r + (dir.y > 0 ? -1 : 1); // 위 스와이프 → 행 -1
            if (nr < 0 || nr >= rows) return -1;
            return nr * cols + c;
        }
    }

    private void SwapTiles(int a, int b, bool countMove = true, bool doCheck = true)
    {
        if (a == b) return;

        var A = tiles[a];
        var B = tiles[b];

        int sibA = A.transform.GetSiblingIndex();
        int sibB = B.transform.GetSiblingIndex();
        A.transform.SetSiblingIndex(sibB);
        B.transform.SetSiblingIndex(sibA);

        tiles[a] = B;
        tiles[b] = A;

        int tmp = A.currentIndex;
        A.currentIndex = b;
        B.currentIndex = a;

        if (A.isEmpty) emptyIndex = b;
        else if (B.isEmpty) emptyIndex = a;

        if (countMove && !isShuffling)
        {
            moveCount++;
            if (hud != null) hud.UpdateMoveText(moveCount);
        }
        if (doCheck && !isShuffling) CheckSolved();
    }

    private bool IsSolvedNow()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            var t = tiles[i];
            if (t == null) continue;
            if (t.isEmpty) continue;
            if (t.currentIndex != t.correctIndex) return false;
        }
        return true;
    }

    private void CheckSolved()
    {
        if (isSolved) return;
        if (IsSolvedNow())
        {
            isSolved = true;
            inputLocked = true;

            FillEmptyTile(); // ✅ 빈칸 채우기

            Debug.Log($"[Board] 🎉 SOLVED! moves={moveCount}");
            // TODO: Step 9-3에서 영상 재생 연결
        }
    }

    private void FillEmptyTile()
    {
        var emptyTile = tiles[emptyIndex];
        if (emptyTile == null) return;

        var sprite = pieceSprites[emptyIndex];
        if (sprite != null)
        {
            emptyTile.SetImage(sprite);
            emptyTile.SetEmpty(false);
        }
    }

}
