using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHud : MonoBehaviour
{
    public Button btnExit;
    public Button btnRestart;
    public TextMeshProUGUI txtMoves; // TMP 기반

    private void Awake()
    {
        if (btnExit != null) btnExit.onClick.AddListener(OnExit);
        if (btnRestart != null) btnRestart.onClick.AddListener(OnRestart);
        UpdateMoveText(0);
    }

    public void UpdateMoveText(int moves)
    {
        if (txtMoves != null)
            txtMoves.text = $"이동: {moves}";
    }

    private void OnExit()
    {
        Debug.Log("[UIHud] 나가기 클릭");
    }

    private void OnRestart()
    {
        Debug.Log("[UIHud] 다시시작 클릭");
    }
}
