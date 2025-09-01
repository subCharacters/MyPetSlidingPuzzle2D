using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int correctIndex;
    public int currentIndex;
    public bool isEmpty;       // ✅ 빈칸이면 true
    public Image image;

    private Vector2 _downPos;
    private const float SwipeThreshold = 40f;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
    }

    public void SetImage(Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;          // ✅ 비어있지 않은 타일은 터치 받기
    }

    public void SetEmpty(bool empty)
    {
        isEmpty = empty;
        if (image != null)
        {
            image.enabled = !empty;
            image.raycastTarget = !empty;    // ✅ 빈칸은 터치 막기
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _downPos = eventData.position;
        Debug.Log($"[TileView] Down idx={currentIndex} pos={_downPos}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        var up = eventData.position;
        var delta = up - _downPos;
        Debug.Log($"[TileView] Up   idx={currentIndex} pos={up} delta={delta}");

        if (delta.magnitude < SwipeThreshold) return;

        Vector2 dir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? new Vector2(Mathf.Sign(delta.x), 0f)
            : new Vector2(0f, Mathf.Sign(delta.y));

        var board = GetComponentInParent<BoardController>();
        if (board != null)
        {
            Debug.Log($"[TileView] Swipe dir={dir}");
            board.TrySwipeMove(this, dir);
        }
    }
}
