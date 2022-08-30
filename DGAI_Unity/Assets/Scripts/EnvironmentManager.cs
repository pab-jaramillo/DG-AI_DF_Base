using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Determines the current stage of the application
/// </summary>
public enum AppStage { Neutral = 0, Selecting = 1, Done = 2 }

/// <summary>
/// Class that manages the application environment
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    #region Private Fields

    VoxelGrid _grid;
    Voxel[] _corners;
    AppStage _stage;
    /// <summary>Determines the current height of the boxes to be created</summary>
    int _height;

    /// <summary>Seed to control random numbers</summary>
    int _seed = 666;

    // 09 (p2p) Creates the Pix2Pix model inference object
    /// <summary>Pix2Pix model prediction object</summary>
    Pix2Pix _pix2pix;

    Texture2D _sourceImage;
    UIManager _uiManager;

    #endregion

    #region Unity Methods
    void Start()
    {
        // UIManager
        _uiManager = GameObject.Find("UIManager").transform.GetComponent<UIManager>();
        if (_uiManager == null) Debug.LogError("UIManager not found!");

        // Defines images that can be used manually
        _sourceImage = _uiManager.SetDropdownSources();

        // Starts the random number engine and the application
        Random.InitState(_seed);
        _stage = AppStage.Neutral;

        // Initializes the grid to be worked on
        _corners = new Voxel[2];
        var gridSize = _uiManager.GridSize;
        _height = gridSize.y;
        var maxSize = _uiManager.MaxGridSize;
        _grid = new VoxelGrid(gridSize, maxSize, transform.position, 1f, transform);

        // 10 (p2p) Initializes the Pix2Pix model inference object
        _pix2pix = new Pix2Pix();
    }

    void Update()
    {
        HandleDrawing();
        HandleHeight();
        // Clear the grid using the "C" key
        if (Input.GetKeyDown(KeyCode.C)) _grid.ClearGrid();

        // 05 Create random boxes using the "A" key
        if (Input.GetKeyDown(KeyCode.A))
        {
            // 06 Clear the grid
            _grid.ClearGrid();
            // 07 Create a random box
            //CreateRandomBox(3, 10, 5, 15);

            // 10 Creates several random boxes
            PopulateRandomBoxes(Random.Range(3, 10), 5, 15, 5, 15);
            var image = _grid.ImageFromGrid();
            _uiManager.SetInputImage(Sprite.Create(image, new Rect(0, 0, image.width, image.height), Vector2.one * 0.5f));
        }
    }

    #endregion

    // 01 Method to create random boxes
    /// <summary>
    /// Randomly creates a box on the grid, within the defined size limits
    /// </summary>
    /// <param name="minX"></param>
    /// <param name="maxX"></param>
    /// <param name="minZ"></param>
    /// <param name="maxZ"></param>
    private void CreateRandomBox(int minX, int maxX, int minZ, int maxZ)
    {
        // 02 Sets the origin coordinates randomly on the grid
        var oX = Random.Range(0, _grid.Size.x);
        var oZ = Random.Range(0, _grid.Size.z);

        var origin = new Vector3Int(oX, 0, oZ);

        // 03 Sets the size of the box
        var sizeX = Random.Range(minX, maxX);
        var sizeY = Random.Range(3, _grid.Size.y);
        var sizeZ = Random.Range(minZ, maxZ);
        var size = new Vector3Int(sizeX, sizeY, sizeZ);

        // 04 Create the box
        _grid.BoxFromCorner(_grid.Voxels[origin.x, origin.y, origin.z], size);
    }

    // 08 Method to create several random boxes
    /// <summary>
    /// Create multiple random boxes in the grid with the defined properties
    /// </summary>
    /// <param name="quantity">Quantidade de caixas</param>
    /// <param name="minX">Tamanho m�nimo em X</param>
    /// <param name="maxX">Tamanho m�ximo em X</param>
    /// <param name="minZ">Tamanho m�nimo em Z</param>
    /// <param name="maxZ">Tamanho m�ximo em Z</param>
    private void PopulateRandomBoxes(int quantity, int minX, int maxX, int minZ, int maxZ)
    {
        // 09 Repetir o m�todo de acordo com a quantidade
        for (int i = 0; i < quantity; i++)
        {
            CreateRandomBox(minX, maxX, minZ, maxZ);
        }
    }

    // 11 Method to create the training set
    /// <summary>
    /// Creates multiple random boxes on the grid, several times and
    /// with variable amounts, according to the defined properties,
    /// and save corresponding images to disk
    /// </summary>
    /// <param name="samples">Quantidade de grids/imagens a gerar</param>
    /// <param name="minQuantity">Quantidade m�nima de caixas por sample</param>
    /// <param name="maxQuantity">Quantidade m�xima de caixas por sample</param>
    /// <param name="minX">Tamanho m�nimo em X</param>
    /// <param name="maxX">Tamanho m�ximo em X</param>
    /// <param name="minZ">Tamanho m�nimo em Z</param>
    /// <param name="maxZ">Tamanho m�ximo em Z</param>
    public void PopulateBoxesAndSave(int samples, int minQuantity, int maxQuantity, int minX, int maxX, int minZ, int maxZ)
    {
        // 12 Ensure the destination folder exists
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "Samples");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        // 13 Iterate according to the amount of samples
        for (int i = 0; i < samples; i++)
        {
            // 14 Cleans Grid
            _grid.ClearGrid();
            // 15 Set random quantity and create the boxes randomly
            int quantity = Random.Range(minQuantity, maxQuantity);
            PopulateRandomBoxes(quantity, minX, maxX, minZ, maxZ);

            // 16 Set the image to the current grid state
            var image = _grid.ImageFromGrid(transparent: true);
            // 17 Resize the image to 256 x 256 px
            var resized = ImageReadWrite.Resize256(image, Color.white);

            // 18 Creates the file and save the image to disk
            var fileName = Path.Combine(directory, $"sample_{i.ToString("D4")}.png");
            ImageReadWrite.SaveImage(resized, fileName);
        }
    }

    /// <summary>
    /// Exposes the training set creation method for a button
    /// </summary>
    public void GenerateSampleSet()
    {
        // 19 Create and save random images
        PopulateBoxesAndSave(500, 3, 10, 3, 10, 3, 10);
    }

    // 11 (p2p) Create the grid prediction and update method
    /// <summary>
    /// Run the Pix2Pix model in the current state of <see cref="VoxelGrid"/> and update
    /// the state of the Voxels according to the pixels of the resulting image
    /// </summary>
    public void PredictAndUpdate()
    {
        // 12 (p2p) Clear red voxels and generate image
        _grid.ClearReds();
        var image = _grid.ImageFromGrid();

        // 13 (p2p) Resize image
        var resized = ImageReadWrite.Resize256(image, Color.white);

        // 14 (p2p) Generate prediction
        _sourceImage = _pix2pix.Predict(resized);

        // 15 (p2p) Resize image to grid size and update red voxels
        TextureScale.Point(_sourceImage, _grid.Size.x, _grid.Size.z);
        UpdateReds();

        // 16 (p2p) View the produced images in the UI
        _uiManager.SetInputImage(Sprite.Create(resized, new Rect(0, 0, resized.width, resized.height), Vector2.one * 0.5f));
        _uiManager.SetOutputImage(Sprite.Create(_sourceImage, new Rect(0, 0, _sourceImage.width, _sourceImage.height), Vector2.one * 0.5f));

    }

    public void SaveGrid(string name)
    {
        if (name == "")
        {
            Debug.Log("Unable to save file without name!");
            return;
        }
        var directory = Path.Combine(Directory.GetCurrentDirectory(), $"Grids");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{name}.csv");
        Util.SaveVoxels(_grid, new List<VoxelState>() { VoxelState.Red, VoxelState.Black }, path);

    }

    /// <summary>
    /// Exposes the function of updating voxels with state <see cref="VoxelState.Red"/>
    /// </summary>
    public void UpdateReds()
    {
        _grid.ClearReds();
        _grid.SetStatesFromImage(_sourceImage,
            _uiManager.GetSturctureBase(),
            _uiManager.GetSturctureTop(),
            _uiManager.GetSturctureThickness(),
            _uiManager.GetSturctureSensitivity(),
            setBlacks: false);
    }

    /// <summary>
    /// Manages the box design process in the interface
    /// </summary>
    private void HandleDrawing()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {

                if (hit.transform.CompareTag("Voxel"))
                {
                    Voxel voxel = hit.transform.GetComponent<VoxelTrigger>().Voxel;
                    if (voxel.State == VoxelState.White || voxel.State == VoxelState.Yellow)
                    {
                        if (_stage == AppStage.Neutral)
                        {
                            _uiManager.SetMouseTagText(_height.ToString());
                            _stage = AppStage.Selecting;
                            _corners[0] = voxel;
                            voxel.SetState(VoxelState.Black);
                        }
                        else if (_stage == AppStage.Selecting && voxel != _corners[0])
                        {
                            _corners[1] = voxel;
                            _grid.SetCorners(_corners);
                        }
                    }
                }
            }
        }
        if (_stage == AppStage.Selecting)
        {
            _uiManager.SetMouseTagPosition(Input.mousePosition + new Vector3(50, 0, 15));
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (_stage == AppStage.Selecting) _stage = AppStage.Done;
            _uiManager.HideMouseTag();
        }
        if (_stage == AppStage.Done)
        {
            _grid.MakeBox(_height);
            _stage = AppStage.Neutral;

            // 17 (p2p) Run the prediction after the design process is finished
            PredictAndUpdate();
        }
    }

    /// <summary>
    /// Manages the control of the height of the boxes to be created in the interface
    /// </summary>
    private void HandleHeight()
    {
        if (Input.GetKeyDown(KeyCode.Period))
        {
            _height = Mathf.Clamp(_height + 1, 1, _grid.Size.y);
            _uiManager.SetMouseTagText(_height.ToString());
        }
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            _height = Mathf.Clamp(_height - 1, 1, _grid.Size.y);
            _uiManager.SetMouseTagText(_height.ToString());
        }
    }

    /// <summary>
    /// Update grid size
    /// </summary>
    /// <param name="newSize"></param>
    public void UpdateGridSize(Vector3Int newSize)
    {
        _height = Mathf.Clamp(_height, 1, newSize.y);
        _grid.ChangeGridSize(newSize);
        _grid.ShowPreview(false);
    }

    /// <summary>
    /// Runs the prediction of the new grid size
    /// </summary>
    /// <param name="newSize"></param>
    public void PreviewGridSize(Vector3Int newSize)
    {
        _grid.ShowPreview(true);
        _grid.UpdatePreview(newSize);
    }

    /// <summary>
    /// Exposes the function of reading an image to the UI
    /// </summary>
    public void ReadImage()
    {
        _grid.ClearGrid();
        _grid.SetStatesFromImage(
            _sourceImage,
            _uiManager.GetSturctureBase(),
            _uiManager.GetSturctureTop(),
            _uiManager.GetSturctureThickness(),
            _uiManager.GetSturctureSensitivity());
    }



    /// <summary>
    /// Clears the grid function for the UI
    /// </summary>
    public void ClearGrid()
    {
        _grid.ClearGrid();
    }

    /// <summary>
    /// Update current Image UI
    /// </summary>
    public void UpdateCurrentImage()
    {
        _sourceImage = _uiManager.GetCurrentImage();
    }
}