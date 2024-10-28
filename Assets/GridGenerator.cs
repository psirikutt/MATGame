using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class GridCell : MonoBehaviour
{
    public int row;    // Row in the grid
    public int column; // Column in the grid

    // These variables will be used to compute x and y positions
    public float cellWidth;   // Width of the button template
    public float cellHeight;  // Height of the button template
    public float buttonSpacing; // Spacing between buttons

    // Computed property for the X position based on column, width, and spacing
    public float XPosition
    {
        get
        {
            return column * (cellWidth + buttonSpacing) - 5.4f;
        }
    }

    // Computed property for the Y position based on row, height, and spacing
    public float YPosition
    {
        get
        {
            return row * (cellHeight + buttonSpacing) - 1.2f;
        }
    }

    // Method to initialize the grid cell with row, column, width, height, and spacing
    public void Initialize(int row, int column, float width, float height, float spacing)
    {
        this.row = row;
        this.column = column;
        this.cellWidth = width;
        this.cellHeight = height;
        this.buttonSpacing = spacing;
    }

    // Method to set position with animation
    public void SetPositionWithAnimation(RectTransform buttonRect, float duration = 0.5f)
    {
        Vector3 targetPosition = new Vector3(XPosition, YPosition, 0);
        LeanTween.move(buttonRect, targetPosition, duration).setEase(LeanTweenType.easeInOutQuad);
    }

    // Method to position the button using its RectTransform
    public void SetPosition(RectTransform buttonRect, float duration = 0.5f)
    {
        buttonRect.anchoredPosition = new Vector3(XPosition, YPosition, 0);
    }
}


public class GridGenerator : MonoBehaviour
{
    public GameObject ButtonTemplate; // Drag your ButtonTemplate here in the Inspector
    public int rows = 8;
    public int columns = 8;
    public int buttonSpacing = 0; // Spacing between buttons

    private GameObject[,] buttons; // Store references to all the buttons

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        buttons = new GameObject[rows, columns];
        RectTransform templateRect = ButtonTemplate.GetComponent<RectTransform>();

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                // Instantiate a new button
                GameObject newButton = Instantiate(ButtonTemplate, transform);
                // Set button position
                RectTransform buttonRect = newButton.GetComponent<RectTransform>();
                GridCell gridCell = newButton.AddComponent<GridCell>();
                gridCell.Initialize(i, j, templateRect.rect.width, templateRect.rect.height, buttonSpacing);

                // Set button position using GridCell's SetPosition method
                gridCell.SetPositionWithAnimation(buttonRect);

                // Set the text on the button
                int buttonNumber = CreateRandomNumber(50);
                newButton.GetComponentInChildren<TextMeshProUGUI>().text = buttonNumber.ToString();

                // Add drag component
                Draggable draggable = newButton.AddComponent<Draggable>();
                draggable.gridGenerator = this;
                draggable.currentRow = i;
                draggable.currentColumn = j;

                // Store button reference
                buttons[i, j] = newButton;
            }
        }

        // Optionally disable the template button (only if you don't want to see the original)
        ButtonTemplate.SetActive(false);
    }

    /// <summary>
    /// Swaps two buttons in the grid and updates their positions and indices.
    /// </summary>
    public void SwapButtons(int row1, int col1, int row2, int col2)
    {
        // Swap the buttons in the grid array
        GameObject temp = buttons[row1, col1];
        buttons[row1, col1] = buttons[row2, col2];
        buttons[row2, col2] = temp;

        // Get GridCell components
        GridCell gridCell1 = buttons[row1, col1].GetComponent<GridCell>();
        GridCell gridCell2 = buttons[row2, col2].GetComponent<GridCell>();

        // Swap their row and column values
        int tempRow = gridCell1.row;
        int tempCol = gridCell1.column;
        gridCell1.row = gridCell2.row;
        gridCell1.column = gridCell2.column;
        gridCell2.row = tempRow;
        gridCell2.column = tempCol;

        // Animate the positions using the SetPositionWithAnimation method
        RectTransform rect1 = buttons[row1, col1].GetComponent<RectTransform>();
        RectTransform rect2 = buttons[row2, col2].GetComponent<RectTransform>();

        gridCell1.SetPositionWithAnimation(rect1);
        gridCell2.SetPositionWithAnimation(rect2);

        // Update draggable script indices after animation completes
        Draggable draggable1 = buttons[row1, col1].GetComponent<Draggable>();
        Draggable draggable2 = buttons[row2, col2].GetComponent<Draggable>();

        draggable1.currentRow = gridCell1.row;
        draggable1.currentColumn = gridCell1.column;
        draggable2.currentRow = gridCell2.row;
        draggable2.currentColumn = gridCell2.column;
    }



    

    // Existing CreateRandomNumber method...
    int CreateRandomNumber(int N)
    {
        int m = N > 99 ? 99 : N;

        // Set initial Probability weights
        int pp20 = 10;
        int pp50 = 5;
        int pp100 = 1;

        if (N < 30)
        {
            pp20 = 1;
            pp50 = 1;
            pp100 = 1;
        }

        // Define the ranges and their corresponding weights
        (int rangeStart, int rangeEnd, int weight)[] rangesWithWeights = new (int, int, int)[]
        {
            (1, m / 5, pp20),
            (m / 5 + 1, m / 2, pp50),
            (m / 2 + 1, m, pp100)
        };

        // Calculate the total weight
        int totalWeight = 0;
        foreach (var range in rangesWithWeights)
        {
            totalWeight += (range.rangeEnd - range.rangeStart + 1) * range.weight;
        }

        // Generate a random number between 1 and totalWeight
        int randomWeight = Random.Range(1, totalWeight + 1);

        int cumulativeWeight = 0;
        foreach (var range in rangesWithWeights)
        {
            int rangeWeight = (range.rangeEnd - range.rangeStart + 1) * range.weight;
            if (randomWeight <= cumulativeWeight + rangeWeight)
            {
                int offset = randomWeight - cumulativeWeight - 1;
                int number = range.rangeStart + (offset / range.weight);
                return number;
            }
            cumulativeWeight += rangeWeight;
        }

        // Fallback (shouldn't occur)
        return 99;
    }
}

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GridGenerator gridGenerator;
    public int currentRow;
    public int currentColumn;

    private Vector3 startPosition;
    private CanvasGroup canvasGroup;
    private GameObject objectToSwap;

    void Start()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        // Initialize currentRow and currentColumn from GridCell component
        GridCell gridCell = GetComponent<GridCell>();
        if (gridCell != null)
        {
            currentRow = gridCell.row;
            currentColumn = gridCell.column;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = transform.position;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(eventData.position);
        // Get half of the cell's width
        RectTransform rectTransform = GetComponent<RectTransform>();
        float halfCellWidth = rectTransform.rect.width / 2;
        transform.position = new Vector3(worldPosition.x - halfCellWidth, worldPosition.y - halfCellWidth, +1f);
    }
    private bool CanSwap(Draggable current, Draggable other)
    {
        int rowDifference = Mathf.Abs(current.currentRow - other.currentRow);
        int columnDifference = Mathf.Abs(current.currentColumn - other.currentColumn);
        Debug.Log("rowDifference:" + rowDifference + " columnDifference:" + columnDifference);

        // The two cells can swap if they are exactly one row or one column apart, but not diagonally
        return (rowDifference == 1 && columnDifference == 0) || (rowDifference == 0 && columnDifference == 1);
    }
    public void OnEndDrag(PointerEventData eventData)
    {   
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(eventData.position);
        // Check if we are overlapping another button
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition);
        foreach (var collider in colliders)
        {
            if (collider.gameObject != this.gameObject && collider.GetComponent<Draggable>() != null)
            {
                Draggable otherDraggable = collider.gameObject.GetComponent<Draggable>();

                // Check if the other grid cell is adjacent (row + 1, row - 1, column + 1, column - 1)
                if (CanSwap(this, otherDraggable))
                {
                    objectToSwap = collider.gameObject;
                    break;
                }
            }
        }
        if (objectToSwap != null)
        {
            Draggable otherDraggable = objectToSwap.GetComponent<Draggable>();
            if(otherDraggable != null && gridGenerator != null)
            {
                gridGenerator.SwapButtons(currentRow, currentColumn, otherDraggable.currentRow, otherDraggable.currentColumn);
            }
        }
        else
        {
            // Reset the row and column based on the gridCell data (if needed)
            GridCell gridCell = GetComponent<GridCell>();
            gridCell.SetPositionWithAnimation(GetComponent<RectTransform>());
        }

        canvasGroup.blocksRaycasts = true;
    }
}

