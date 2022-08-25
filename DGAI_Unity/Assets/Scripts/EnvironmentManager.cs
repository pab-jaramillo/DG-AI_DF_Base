using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Determina o est�gio atual da aplica��o
/// </summary>
public enum AppStage { Neutral = 0, Selecting = 1, Done = 2 }

/// <summary>
/// Classe que gerencia o ambiente da aplica��o
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    #region Campos privados

    VoxelGrid _grid;
    Voxel[] _corners;
    AppStage _stage;
    /// <summary>Determina a altura atual da caixas a serem criadas</summary>
    int _height;

    /// <summary>Seed para controle dos n�meros aleat�rios</summary>
    int _seed = 666;

    // 09 (p2p) Cria o objeto de infer�ncia do modelo Pix2Pix
    /// <summary>Objeto de previs�o do modelo Pix2Pix</summary>
    Pix2Pix _pix2pix;

    Texture2D _sourceImage;
    UIManager _uiManager;

    #endregion

    #region M�todos do Unity
    void Start()
    {
        // Coleta o UIManager
        _uiManager = GameObject.Find("UIManager").transform.GetComponent<UIManager>();
        if (_uiManager == null) Debug.LogError("UIManager n�o foi encontrado!");

        // Define as imagens que podem ser utilizadas manualmente
        _sourceImage = _uiManager.SetDropdownSources();

        // Inicializa o motor de n�meros aleat�rios e a aplica��o
        Random.InitState(_seed);
        _stage = AppStage.Neutral;

        // Inicializa o grid que ser� trabalhado
        _corners = new Voxel[2];
        var gridSize = _uiManager.GridSize;
        _height = gridSize.y;
        var maxSize = _uiManager.MaxGridSize;
        _grid = new VoxelGrid(gridSize, maxSize, transform.position, 1f, transform);

        // 10 (p2p) Inicializa o objeto de infer�ncia do modelo Pix2Pix
        _pix2pix = new Pix2Pix();
    }

    void Update()
    {
        HandleDrawing();
        HandleHeight();
        // Limpar o grid utilizando a tecla "C"
        if (Input.GetKeyDown(KeyCode.C)) _grid.ClearGrid();

        // 05 Cria caixas aleat�rias utilizando a tecla "A"
        if (Input.GetKeyDown(KeyCode.A))
        {
            // 06 Limpa o grid
            _grid.ClearGrid();
            // 07 Cria uma caixa aleat�ria
            //CreateRandomBox(3, 10, 5, 15);

            // 10 Cria diversas caixas aleat�rias
            PopulateRandomBoxes(Random.Range(3, 10), 5, 15, 5, 15);
            var image = _grid.ImageFromGrid();
            _uiManager.SetInputImage(Sprite.Create(image, new Rect(0, 0, image.width, image.height), Vector2.one * 0.5f));
        }
    }

    #endregion

    // 01 Criar m�todo para cria��o de caixas aleat�rias
    /// <summary>
    /// Cria uma caixa aleat�riamente no grid, dentro dos limities de tamanho definidos
    /// </summary>
    /// <param name="minX"></param>
    /// <param name="maxX"></param>
    /// <param name="minZ"></param>
    /// <param name="maxZ"></param>
    private void CreateRandomBox(int minX, int maxX, int minZ, int maxZ)
    {
        // 02 Define as coordenadas de origem aleatoriamente no grid
        var oX = Random.Range(0, _grid.Size.x);
        var oZ = Random.Range(0, _grid.Size.z);

        var origin = new Vector3Int(oX, 0, oZ);

        // 03 Define o tamaho da caixa 
        var sizeX = Random.Range(minX, maxX);
        var sizeY = Random.Range(3, _grid.Size.y);
        var sizeZ = Random.Range(minZ, maxZ);
        var size = new Vector3Int(sizeX, sizeY, sizeZ);

        // 04 Cria a caixa
        _grid.BoxFromCorner(_grid.Voxels[origin.x, origin.y, origin.z], size);
    }

    // 08 Criar o m�todo para cria��o de diversas caixas aleat�rias
    /// <summary>
    /// Cria m�ltiplas caixas aleat�rias no grid com as propriedades definidas
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

    // 11 Criar o m�todo para cria��o do set de treinamento
    /// <summary>
    /// Cria m�ltiplas caixas aleat�rias no grid, diversas vezes e 
    /// com quantidades vari�veis, de acordo com as propriedades definidas,
    /// e salva imagens correspondentes no disco
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
        // 12 Garatir que a pasta de destino existe
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "Samples");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        // 13 Iterar de acordo com a quantidade de samples
        for (int i = 0; i < samples; i++)
        {
            // 14 Limpar o grid
            _grid.ClearGrid();
            // 15 Definir quantidade aleat�ria e criar as caixas aleatoriamente
            int quantity = Random.Range(minQuantity, maxQuantity);
            PopulateRandomBoxes(quantity, minX, maxX, minZ, maxZ);

            // 16 Definir a imagem para o atual estado do grid
            var image = _grid.ImageFromGrid(transparent: true);
            // 17 Redimensionar a imagem para 256 x 256 px
            var resized = ImageReadWrite.Resize256(image, Color.white);

            // 18 Criar o arquivo e salvar a imagem no disco
            var fileName = Path.Combine(directory, $"sample_{i.ToString("D4")}.png");
            ImageReadWrite.SaveImage(resized, fileName);
        }
    }

    /// <summary>
    /// Exp�e o m�todo de cria��o do set de treinamento para um bot�o
    /// </summary>
    public void GenerateSampleSet()
    {
        // 19 Criar e salvar as imagens aleat�rias
        PopulateBoxesAndSave(500, 3, 10, 3, 10, 3, 10);
    }

    // 11 (p2p) Criar o m�todo de previs�o e atualiza��o do grid
    /// <summary>
    /// Executa o modelo Pix2Pix no atual estado do <see cref="VoxelGrid"/> e atualiza
    /// o estado dos Voxels de acordo com os pixels da imagem resultante
    /// </summary>
    public void PredictAndUpdate()
    {
        // 12 (p2p) Limpar voxels vermelhos e gerar imagem
        _grid.ClearReds();
        var image = _grid.ImageFromGrid();

        // 13 (p2p) Redimensionar image
        var resized = ImageReadWrite.Resize256(image, Color.white);

        // 14 (p2p) Gerar previs�o
        _sourceImage = _pix2pix.Predict(resized);

        // 15 (p2p) Redimensionar imagem para o tamnho do grid e atualizar os voxels vermelhos
        TextureScale.Point(_sourceImage, _grid.Size.x, _grid.Size.z);
        UpdateReds();

        // 16 (p2p) Exibir as imagens produzidas na UI
        _uiManager.SetInputImage(Sprite.Create(resized, new Rect(0, 0, resized.width, resized.height), Vector2.one * 0.5f));
        _uiManager.SetOutputImage(Sprite.Create(_sourceImage, new Rect(0, 0, _sourceImage.width, _sourceImage.height), Vector2.one * 0.5f));

    }

    public void SaveGrid(string name)
    {
        if (name == "")
        {
            Debug.Log("N�o � poss�vel salvar arquivo sem nome!");
            return;
        }
        var directory = Path.Combine(Directory.GetCurrentDirectory(), $"Grids");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{name}.csv");
        Util.SaveVoxels(_grid, new List<VoxelState>() { VoxelState.Red, VoxelState.Black }, path);

    }

    /// <summary>
    /// Exp�e a fun��o de atualizar os voxels com estado <see cref="VoxelState.Red"/>
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
    /// Gerencia o processo de desenho de caixas na interface
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

            // 17 (p2p) Executar a previs�o ap�s o t�rmino do processo de desenho
            PredictAndUpdate();
        }
    }

    /// <summary>
    /// Gerencia o controle da altura das caixas a serem criadas na interface
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
    /// Atualiza o tamanho do grid
    /// </summary>
    /// <param name="newSize"></param>
    public void UpdateGridSize(Vector3Int newSize)
    {
        _height = Mathf.Clamp(_height, 1, newSize.y);
        _grid.ChangeGridSize(newSize);
        _grid.ShowPreview(false);
    }

    /// <summary>
    /// Executa a previs�o do novo tamanho do grid
    /// </summary>
    /// <param name="newSize"></param>
    public void PreviewGridSize(Vector3Int newSize)
    {
        _grid.ShowPreview(true);
        _grid.UpdatePreview(newSize);
    }

    /// <summary>
    /// Exp�e a fun��o de ler uma imagem para a UI
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
    /// Exp�e a funl�ao de limpar o grid para a UI
    /// </summary>
    public void ClearGrid()
    {
        _grid.ClearGrid();
    }

    /// <summary>
    /// Exp�e a fun��o de atualizar a imagem atual pela UI
    /// </summary>
    public void UpdateCurrentImage()
    {
        _sourceImage = _uiManager.GetCurrentImage();
    }
}