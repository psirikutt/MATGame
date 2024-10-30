using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class GridCell : MonoBehaviour
{

    private int _row;    // Backing field for the row
    private int _column; // Backing field for the column

    // Property for row with didSet-like behavior
    public int Row
    {
        get => _row;
        set
        {
            if (_row != value) // Only run if value actually changes
            {
                _row = value;
                // Call the animation method whenever the row changes
                SetPositionWithAnimation();
            } else {
                SetPosition();
            }
        }
    }

    // Property for column with didSet-like behavior
    public int Column
    {
        get => _column;
        set
        {
            if (_column != value) // Only run if value actually changes
            {
                _column = value;
                // Call the animation method whenever the column changes
                SetPositionWithAnimation();
            } else {
                SetPosition();
            }
        }
    }

    // These variables will be used to compute x and y positions
    public float cellWidth;   // Width of the button template
    public float cellHeight;  // Height of the button template
    public float buttonSpacing; // Spacing between buttons

    // Computed property for the X position based on column, width, and spacing
    public float XPosition
    {
        get
        {
            return Column * (cellWidth + buttonSpacing) - 5.4f;
        }
    }

    // Computed property for the Y position based on row, height, and spacing
    public float YPosition
    {
        get
        {
            return -Row * (cellHeight + buttonSpacing) + 8.4f;
        }
    }

    // Method to initialize the grid cell with row, column, width, height, and spacing
    public void Initialize(int row, int column, float width, float height, float spacing)
    {
        this.Row = row;
        this.Column = column;
        this.cellWidth = width;
        this.cellHeight = height;
        this.buttonSpacing = spacing;
        SetPositionWithAnimation();
    }

    // Method to set position with animation
    public void SetPositionWithAnimation(float duration = 0.5f)
{
    RectTransform rectTransform = GetComponent<RectTransform>();
    Vector2 currentPosition = rectTransform.anchoredPosition;
    Vector2 targetPosition = new Vector2(XPosition, YPosition);

    // If current position is not the target, animate
    if (currentPosition != targetPosition)
    {
        LeanTween.value(gameObject, currentPosition, targetPosition, duration)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnUpdate((Vector2 val) => {
                rectTransform.anchoredPosition = val;
            });
    }
}


    // Method to position the button using its RectTransform
    public void SetPosition()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(XPosition, YPosition);
    }
}


public partial class GridGenerator : MonoBehaviour
{
    public GameObject ButtonTemplate; // Drag your ButtonTemplate here in the Inspector
    public int rows = 8;
    public int columns = 8;
    public int buttonSpacing = 0; // Spacing between buttons

    private GameObject[,] buttons; // Store references to all the buttons

    void Start()
    {
        GenerateGrid();
        StartCoroutine(DestroyMatchesSilently());
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
        int tempRow = gridCell1.Row;
        int tempCol = gridCell1.Column;
        gridCell1.Row = gridCell2.Row;
        gridCell1.Column = gridCell2.Column;
        gridCell2.Row = tempRow;
        gridCell2.Column = tempCol;

        // Update draggable script indices after animation completes
        Draggable draggable1 = buttons[row1, col1].GetComponent<Draggable>();
        Draggable draggable2 = buttons[row2, col2].GetComponent<Draggable>();

        draggable1.currentRow = gridCell1.Row;
        draggable1.currentColumn = gridCell1.Column;
        draggable2.currentRow = gridCell2.Row;
        draggable2.currentColumn = gridCell2.Column;
        StartCoroutine(CheckIfSumExists());
    }

    public bool CanSwap(int fromRow, int fromCol, int toRow, int toCol)
    {
        // Make a copy of the grid to simulate the swap
        GameObject[,] tempGrid = (GameObject[,])buttons.Clone();

        // Perform the swap in the temporary grid
        var temp = tempGrid[fromRow, fromCol];
        tempGrid[fromRow, fromCol] = tempGrid[toRow, toCol];
        tempGrid[toRow, toCol] = temp;

        // Check for any matches in the temporary grid
        bool hasMatch = CheckForMatches(tempGrid);
        Debug.Log(hasMatch);
        return hasMatch && !isMoving;
    }

    // Checks if there are any matches in the grid.
    private bool CheckForMatches(GameObject[,] grid)
    {
        int gridSizeY = grid.GetLength(0);
        int gridSizeX = grid.GetLength(1);

        for (int row = 0; row < gridSizeY; row++)
        {
            for (int col = 0; col < gridSizeX; col++)
            {
                if (grid[row, col] == null) continue;
                Debug.Log("Checking: " + row + ", " + col + ", " + GetCellValue(grid[row, col].GetComponent<GridCell>()));
                // Check vertical matches
                if (row + 2 < gridSizeY)
                {
                    if (grid[row + 1, col] != null && grid[row + 2, col] != null)
                    {
                        int a = GetCellValue(grid[row, col].GetComponent<GridCell>());
                        int b = GetCellValue(grid[row + 1, col].GetComponent<GridCell>());
                        int c = GetCellValue(grid[row + 2, col].GetComponent<GridCell>());

                        if (CheckOperations(a, b, c))
                        {
                            Debug.Log("Found match: " + a + ", " + b + ", " + c + " at " + row + ", " + col);
                            return true;
                        }
                    }
                }

                // Check horizontal matches
                if (col + 2 < gridSizeX)
                {
                    if (grid[row, col + 1] != null && grid[row, col + 2] != null)
                    {
                        int a = GetCellValue(grid[row, col].GetComponent<GridCell>());
                        int b = GetCellValue(grid[row, col + 1].GetComponent<GridCell>());
                        int c = GetCellValue(grid[row, col + 2].GetComponent<GridCell>());

                        if (CheckOperations(a, b, c))
                        {
                            Debug.Log("Found match: " + a + ", " + b + ", " + c + " at " + row + ", " + col);
                            return true;
                        }
                    }
                }
            }
        }
        return false;
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
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = true;

        // Initialize currentRow and currentColumn from GridCell component
        GridCell gridCell = GetComponent<GridCell>();
        if (gridCell != null)
        {
            currentRow = gridCell.Row;
            currentColumn = gridCell.Column;
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
        transform.position = new Vector3(worldPosition.x - halfCellWidth, worldPosition.y - halfCellWidth, startPosition.z);
    }

    // Checks if swapping two cells results in any matches.

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
                if ((Mathf.Abs(this.currentRow - otherDraggable.currentRow) == 1 && Mathf.Abs(this.currentColumn - otherDraggable.currentColumn) == 0) || (Mathf.Abs(this.currentRow - otherDraggable.currentRow) == 0 && Mathf.Abs(this.currentColumn - otherDraggable.currentColumn) == 1))
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
                // Before swapping, attempt to swap using TrySwapButtons
                if (!gridGenerator.CanSwap(currentRow, currentColumn, otherDraggable.currentRow, otherDraggable.currentColumn))
                {
                    // Swap is not allowed, reset positions
                    GridCell gridCell = GetComponent<GridCell>();
                    gridCell.SetPositionWithAnimation();
                    otherDraggable.GetComponent<GridCell>().SetPositionWithAnimation();
                } else {
                    gridGenerator.SwapButtons(currentRow, currentColumn, otherDraggable.currentRow, otherDraggable.currentColumn);
                }
            }
        }
        else
        {
            // Reset the row and column based on the gridCell data (if needed)
            GridCell gridCell = GetComponent<GridCell>();
            gridCell.SetPositionWithAnimation();
        }

        canvasGroup.blocksRaycasts = true;
    }
}

public partial class GridGenerator : MonoBehaviour
{
    private bool isMoving = false;
    private int score = 0;
    private List<int> combo = new List<int>();
    private bool showKidAnimation = false;

    private IEnumerator DestroyMatchesSilently()
    {
        bool matchesExist = true;
        int count = 0;

        while (matchesExist)
        {
            HashSet<Cell> cellsToDestroy = new HashSet<Cell>();

            // Find all matches
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (buttons[row, col] == null) continue;

                    int a, b, c;

                    // Check vertical operations
                    if (row + 2 < rows)
                    {
                        if (buttons[row + 1, col] != null && buttons[row + 2, col] != null)
                        {
                            a = GetCellValue(buttons[row, col].GetComponent<GridCell>());
                            b = GetCellValue(buttons[row + 1, col].GetComponent<GridCell>());
                            c = GetCellValue(buttons[row + 2, col].GetComponent<GridCell>());

                            if (CheckOperations(a, b, c))
                            {
                                cellsToDestroy.Add(new Cell(row, col));
                                cellsToDestroy.Add(new Cell(row + 1, col));
                                cellsToDestroy.Add(new Cell(row + 2, col));
                            }
                        }
                    }

                    // Check horizontal operations
                    if (col + 2 < columns)
                    {
                        if (buttons[row, col + 1] != null && buttons[row, col + 2] != null)
                        {
                            a = GetCellValue(buttons[row, col].GetComponent<GridCell>());
                            b = GetCellValue(buttons[row, col + 1].GetComponent<GridCell>());
                            c = GetCellValue(buttons[row, col + 2].GetComponent<GridCell>());

                            if (CheckOperations(a, b, c))
                            {
                                cellsToDestroy.Add(new Cell(row, col));
                                cellsToDestroy.Add(new Cell(row, col + 1));
                                cellsToDestroy.Add(new Cell(row, col + 2));
                            }
                        }
                    }
                }
            }

            if (cellsToDestroy.Count > 0)
            {
                // Step 1: Destroy matched cells silently
                foreach (Cell cell in cellsToDestroy)
                {
                    int row = cell.row;
                    int col = cell.col;

                    if (buttons[row, col] != null)
                    {
                        Destroy(buttons[row, col]);
                        buttons[row, col] = null;
                    }
                }

                // Step 2: Apply gravity silently
                for (int col = 0; col < columns; col++)
                {
                    List<GameObject> newColumn = new List<GameObject>();

                    // Extract non-null blocks from buttons
                    for (int row = rows - 1; row >= 0; row--)
                    {
                        if (buttons[row, col] != null)
                        {
                            newColumn.Add(buttons[row, col]);
                        }
                    }

                    // Calculate the number of empty spaces
                    int emptySpaces = rows - newColumn.Count;

                    // Add new nodes at the top
                    for (int i = 0; i < emptySpaces; i++)
                    {
                        // Instantiate a new button
                        GameObject newButton = Instantiate(ButtonTemplate, transform);
                        newButton.SetActive(true);

                        // Initialize GridCell component
                        GridCell gridCell = newButton.GetComponent<GridCell>();
                        if (gridCell == null)
                            gridCell = newButton.AddComponent<GridCell>();

                        RectTransform templateRect = ButtonTemplate.GetComponent<RectTransform>();
                        gridCell.Initialize(0, col, templateRect.rect.width, templateRect.rect.height, buttonSpacing);

                        // Set the text on the button
                        int buttonNumber = CreateRandomNumber(50);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = buttonNumber.ToString();

                        // Add and initialize Draggable component
                        Draggable draggable = newButton.GetComponent<Draggable>();
                        if (draggable == null)
                            draggable = newButton.AddComponent<Draggable>();

                        draggable.gridGenerator = this;
                        draggable.currentRow = 0;
                        draggable.currentColumn = col;

                        // Add to newColumn list
                        newColumn.Add(newButton);
                    }

                    // Assign back to buttons array
                    for (int row = rows - 1; row >= 0; row--)
                    {
                        int newColumnIndex = rows - 1 - row; // Calculate index in newColumn

                        buttons[row, col] = newColumn[newColumnIndex];

                        GridCell gridCell = buttons[row, col].GetComponent<GridCell>();
                        gridCell.Row = row;
                        gridCell.Column = col;

                        // Update Draggable script indices
                        Draggable draggable = buttons[row, col].GetComponent<Draggable>();
                        draggable.currentRow = row;
                        draggable.currentColumn = col;
                    }
                }

                // Wait a frame to allow Unity to update
                yield return null;

                // Continue the loop to check for new matches
            }
            else
            {
                matchesExist = false;
            }

            count += 1;
            if (count > 100)
            {
                break;
            }
        }
    }

    public IEnumerator CheckIfSumExists()
    {
        List<GridCell> cellsToDestroy = new List<GridCell>();
        int valueToDestroy = 0;
        isMoving = true;

        // Find all cells to destroy
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (buttons[row, col] == null) continue;


                // Get the GridCell instance
                GridCell currentCell = buttons[row, col].GetComponent<GridCell>();

                // Check vertical operations
                if (row + 2 < rows)
                {
                    if (CheckVerticalMatch(currentCell, ref valueToDestroy, cellsToDestroy))
                    {
                        HandleVerticalMatch(currentCell, ref valueToDestroy, cellsToDestroy);
                    }
                }

                // Check horizontal operations
                if (col + 2 < columns)
                {
                    if (CheckHorizontalMatch(currentCell, ref valueToDestroy, cellsToDestroy))
                    {
                        HandleHorizontalMatch(currentCell, ref valueToDestroy, cellsToDestroy);
                    }
                }
            }
        }

        yield return StartCoroutine(HandleMatches(cellsToDestroy, valueToDestroy));
    }
    private bool CheckVerticalMatch(GridCell cell, ref int valueToDestroy, List<GridCell> cellsToDestroy)
    {
        int row = cell.Row;
        int col = cell.Column;

        if (buttons[row + 1, col] == null || buttons[row + 2, col] == null)
            return false;

        GridCell cellB = buttons[row + 1, col].GetComponent<GridCell>();
        GridCell cellC = buttons[row + 2, col].GetComponent<GridCell>();

        int a = GetCellValue(cell);
        int b = GetCellValue(cellB);
        int c = GetCellValue(cellC);

        return CheckOperations(a, b, c);
    }

    private bool CheckHorizontalMatch(GridCell cell, ref int valueToDestroy, List<GridCell> cellsToDestroy)
    {
        int row = cell.Row;
        int col = cell.Column;

        if (buttons[row, col + 1] == null || buttons[row, col + 2] == null)
            return false;

        GridCell cellB = buttons[row, col + 1].GetComponent<GridCell>();
        GridCell cellC = buttons[row, col + 2].GetComponent<GridCell>();

        int a = GetCellValue(cell);
        int b = GetCellValue(cellB);
        int c = GetCellValue(cellC);

        return CheckOperations(a, b, c);
    }


    private int GetCellValue(GridCell cell)
    {
        if (cell == null) return 0;
        var textComponent = cell.GetComponentInChildren<TextMeshProUGUI>();
        return int.Parse(textComponent.text);
    }   


    private bool CheckOperations(int a, int b, int c)
    {
        return CheckPlus(a, b, c) || CheckDiff(a, b, c) || CheckMultiply(a, b, c) || CheckDivide(a, b, c);
    }

    private bool CheckPlus(int lhs, int rhs, int result)
    {
        return lhs + rhs == result;
    }

    private bool CheckDiff(int lhs, int rhs, int result)
    {
        return lhs - rhs == result;
    }

    private bool CheckMultiply(int lhs, int rhs, int result)
    {
        return lhs * rhs == result;
    }

    private bool CheckDivide(int lhs, int rhs, int result)
    {
        if (rhs == 0) return false;
        return lhs / rhs == result && lhs % rhs == 0;
    }
    private void HandleVerticalMatch(GridCell cell, ref int valueToDestroy, List<GridCell> cellsToDestroy)
    {
        int row = cell.Row;
        int col = cell.Column;

        GridCell cellB = buttons[row + 1, col].GetComponent<GridCell>();
        GridCell cellC = buttons[row + 2, col].GetComponent<GridCell>();

        AddCellToDestroy(cell, cellsToDestroy);
        AddCellToDestroy(cellB, cellsToDestroy);
        AddCellToDestroy(cellC, cellsToDestroy);

        valueToDestroy += GetCellValue(cell) + GetCellValue(cellB) + GetCellValue(cellC);
    }

    private void HandleHorizontalMatch(GridCell cell, ref int valueToDestroy, List<GridCell> cellsToDestroy)
    {
        int row = cell.Row;
        int col = cell.Column;

        GridCell cellB = buttons[row, col + 1].GetComponent<GridCell>();
        GridCell cellC = buttons[row, col + 2].GetComponent<GridCell>();

        AddCellToDestroy(cell, cellsToDestroy);
        AddCellToDestroy(cellB, cellsToDestroy);
        AddCellToDestroy(cellC, cellsToDestroy);

        valueToDestroy += GetCellValue(cell) + GetCellValue(cellB) + GetCellValue(cellC);
    }

    private void AddCellToDestroy(GridCell cell, List<GridCell> cellsToDestroy)
    {
        if (!cellsToDestroy.Contains(cell))
        {
            cellsToDestroy.Add(cell);
        }
    }

    public IEnumerator HandleMatches(List<GridCell> cellsToDestroy, int valueToDestroy)
    {
        if (cellsToDestroy.Count > 0)
        {
            // Step 1: Mark matched cells (optional)

            // Step 2: Delay 0.5 sec and count score
            yield return new WaitForSeconds(0.5f);
            score += valueToDestroy;

            // Step 3: Remove matched cells with animation
            foreach (GridCell cell in cellsToDestroy)
            {
                int row = cell.Row;
                int col = cell.Column;

                if (buttons[row, col] != null)
                {
                    GameObject block = buttons[row, col];

                    // Step 1: Create and add the smoke particle effect
                    // Assume you have a smoke effect prefab at Resources/SmokeEffect
                    GameObject smokeEffectPrefab = Resources.Load<GameObject>("SmokeEffect");
                    if (smokeEffectPrefab != null)
                    {
                        GameObject smokeEffect = Instantiate(smokeEffectPrefab, block.transform.position, Quaternion.identity, transform);
                        Destroy(smokeEffect, 0.5f);
                    }

                    // Step 2: Fade out the block and remove it
                    CanvasGroup canvasGroup = block.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = block.AddComponent<CanvasGroup>();
                    }
                    LeanTween.alphaCanvas(canvasGroup, 0f, 0.3f).setOnComplete(() =>
                    {
                        Destroy(block);
                    });

                    // Step 3: Set the block to null to represent its destruction
                    buttons[row, col] = null;
                }
            }

            // Step 4: Apply gravity after destruction animation completes
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(ApplyGravityWithAnimation());

            // Step 5: Check for new matches after gravity animation completes
            if (combo.Count > 0)
            {
                combo[combo.Count - 1] += 1;
            }
            else
            {
                combo.Add(1);
            }

            showKidAnimation = true;
            yield return StartCoroutine(CheckIfSumExists());
        }
        else
        {
            // No matches found, check game over
            // Optionally implement checkGameOver()
        }
            isMoving = false;
    }

    public IEnumerator ApplyGravityWithAnimation()
    {

        for (int col = 0; col < columns; col++)
        {
            int emptyRow = -1;
            for (int row = rows - 1; row >= 0; row--)
            {
                if (buttons[row, col] == null)
                {
                    if (emptyRow == -1)
                    {
                        emptyRow = row;
                    }
                }
                else if (emptyRow != -1)
                {
                    // Move the block down
                    GameObject block = buttons[row, col];
                    buttons[emptyRow, col] = block;
                    buttons[row, col] = null;

                    GridCell gridCell = block.GetComponent<GridCell>();
                    gridCell.Row = emptyRow;

                    // Update Draggable script indices
                    Draggable draggable = block.GetComponent<Draggable>();
                    draggable.currentRow = emptyRow;

                    emptyRow--;
                }
            }
        }

            // Wait for animations to complete
            yield return new WaitForSeconds(0.5f);

            // Optionally, fill empty spaces at the top with new blocks
            yield return StartCoroutine(FillEmptySpaces());
    }

    private IEnumerator FillEmptySpaces()
    {
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (buttons[row, col] == null)
                {
                    // Instantiate a new button
                    GameObject newButton = Instantiate(ButtonTemplate, transform);
                    newButton.SetActive(true);

                    // Initialize GridCell component
                    GridCell gridCell = newButton.GetComponent<GridCell>();
                    if (gridCell == null)
                        gridCell = newButton.AddComponent<GridCell>();

                    RectTransform templateRect = ButtonTemplate.GetComponent<RectTransform>();
                    gridCell.Initialize(row, col, templateRect.rect.width, templateRect.rect.height, buttonSpacing);

                    // Set the text on the button
                    int buttonNumber = CreateRandomNumber(50);
                    newButton.GetComponentInChildren<TextMeshProUGUI>().text = buttonNumber.ToString();

                    // Add and initialize Draggable component
                    Draggable draggable = newButton.GetComponent<Draggable>();
                    if (draggable == null)
                        draggable = newButton.AddComponent<Draggable>();

                    draggable.gridGenerator = this;
                    draggable.currentRow = row;
                    draggable.currentColumn = col;

                    // Store button reference
                    buttons[row, col] = newButton;

                    // Optionally, animate the button appearing
                    CanvasGroup canvasGroup = newButton.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = newButton.AddComponent<CanvasGroup>();
                    }
                    canvasGroup.alpha = 0f;
                    LeanTween.alphaCanvas(canvasGroup, 1f, 0.3f);

                    yield return new WaitForSeconds(0.05f); // Slight delay between spawns
                }
            }
        }
    }
}

public class Cell
{
    public int row;
    public int col;

    public Cell(int row, int col)
    {
        this.row = row;
        this.col = col;
    }

    public override bool Equals(object obj)
    {
        Cell other = obj as Cell;
        if (other == null) return false;
        return row == other.row && col == other.col;
    }

    public override int GetHashCode()
    {
        return row.GetHashCode() ^ col.GetHashCode();
    }
}

