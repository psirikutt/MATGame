using UnityEngine;
using UnityEngine.UI;

public class gridicon : MonoBehaviour
{
    public enum IconType { Hammer, Fire, Ice } // Define types of icons
    public IconType iconType;

    private RectTransform rectTransform;
    private Image iconImage;

    public Vector2Int gridPosition; // Position in the grid (row, column)
    private Vector2 originalPosition; // To store the initial position
    public IconsDraggable draggableComponent;

    // Sprites for each icon type
    public Sprite hammerSprite;
    public Sprite fireSprite;
    public Sprite iceSprite;

    public void Initialize(IconType type)
    {
        iconType = type;
        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>(); // Ensure iconImage is assigned
        originalPosition = rectTransform.anchoredPosition;
        draggableComponent = GetComponent<IconsDraggable>();
        SetIconAppearance();
        Debug.Log("GridIcon");
    }

    // Sets the icon's appearance based on its type
    private void SetIconAppearance()
    {
        switch (iconType)
        {
            case IconType.Hammer:
                iconImage.sprite = hammerSprite;
                break;
            case IconType.Fire:
                iconImage.sprite = fireSprite;
                break;
            case IconType.Ice:
                iconImage.sprite = iceSprite;
                break;
        }
    }
    public void SetIconPositionWithAnimation(float duration = 0.5f)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 currentPosition = rectTransform.anchoredPosition;
        if (currentPosition.y > 4f)
        {
            //   currentPosition.y = 12f;
        }
        Vector2 targetPosition = new Vector2(-4f, -8f);

        // If current position is not the target, animate
        if (currentPosition != targetPosition)
        {
            LeanTween.value(gameObject, currentPosition, targetPosition, duration)
                .setEase(LeanTweenType.easeInOutQuad)
                .setOnUpdate((Vector2 val) =>
                {
                    rectTransform.anchoredPosition = val;
                });
        }
    }
    // Set the icon's grid position
    public void SetIconPosition(int row, int column)
    {
        gridPosition = new Vector2Int(row, column);
    }

    // Reset icon to its original position
    public void ResetPosition()
    {
        rectTransform.anchoredPosition = originalPosition;
    }

    // Method to destroy or clear the icon when needed
    public void ClearIcon()
    {
        Destroy(gameObject);
    }
}
