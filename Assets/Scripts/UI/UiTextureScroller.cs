using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UiTextureScroller : MonoBehaviour
{
    [SerializeField] private float speed = 0.01f;

    private RawImage rawImage;
    private float offset;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    private void Update()
    {
        // UI animation should continue independently from gameplay time.
        offset = Mathf.Repeat(
            offset - speed * Time.unscaledDeltaTime,
            1f
        );

        Rect uvRect = rawImage.uvRect;
        uvRect.y = offset;

        rawImage.uvRect = uvRect;
    }
}