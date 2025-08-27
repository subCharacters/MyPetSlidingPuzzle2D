using UnityEngine;
using UnityEngine.UI;

public class BoardController : MonoBehaviour
{
    private GridLayoutGroup _grid;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        Debug.Log("[BoardController] Awake - 보드 준비 완료");
    }

    private void Start()
    {
        Debug.Log("[BoardController] Start - 보드 시작됨");
        // 다음 단계: 여기서 퍼즐 타일을 생성할 예정
    }
}
