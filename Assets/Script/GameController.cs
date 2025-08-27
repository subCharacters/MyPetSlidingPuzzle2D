using UnityEngine;

public class GameController : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[GameController] Awake - 씬 준비 시작");
    }

    private void Start()
    {
        Debug.Log("[GameController] Start - 초기화 완료");
        // 앞으로 여기서 퍼즐 보드를 불러올 예정
    }
}
