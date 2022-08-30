using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;

/// <summary>
/// This class allows the execution of a Pix2Pix trainer model to
/// make inferences from new images
/// </summary>
public class Pix2Pix
{
    #region Fields and properties

    NNModel _modelAsset;
    Model _loadedModel;
    IWorker _worker;

    #endregion

    #region Construtor

    /// <summary>
    /// Pix2pix inference object builder
    /// </summary>
    public Pix2Pix()
    {
        _modelAsset = Resources.Load<NNModel>("NeuralModels/trained");
        _loadedModel = ModelLoader.Load(_modelAsset);
        _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, _loadedModel);
    }

    #endregion

    #region Public Methods

    // 01 Creates a model prediction method
    /// <summary>
    /// Runs the inference model on an image and generates the prediction of a translated image
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public Texture2D Predict(Texture2D image)
    {
        // 02 Translates the original image to a 3-channel (RGB) tensor
        Tensor imageTensor = new Tensor(image, channels: 3);

        // 03 Normalizes the tensor to the field that the model expects
        var normalisedInput = NormaliseTensor(imageTensor, 0f, 1f, -1f, 1f);

        // 04 Run the model on the tensor
        _worker.Execute(normalisedInput);

        // 05 Returns the model prediction result
        var outputTensor = _worker.PeekOutput();

        // 07 Translate the tensor to an image
        Texture2D prediction = Tensor2Image(outputTensor, image);

        // 08 Discard used tensors
        imageTensor.Dispose();
        normalisedInput.Dispose();
        outputTensor.Dispose();

        return prediction;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Translate a tensor in Texture2D
    /// </summary>
    /// <param name="inputTensor"></param>
    /// <param name="inputTexture"></param>
    /// <returns></returns>
    Texture2D Tensor2Image(Tensor inputTensor, Texture2D inputTexture)
    {
        var tempRT = new RenderTexture(256, 256, 24);
        inputTensor.ToRenderTexture(tempRT);
        RenderTexture.active = tempRT;

        var resultTexture = new Texture2D(inputTexture.width, inputTexture.height);
        resultTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        resultTexture.Apply();

        RenderTexture.active = null;
        tempRT.DiscardContents();

        return resultTexture;
    }

    /// <summary>
    /// Normalizes a tensor to a given field
    /// </summary>
    /// <param name="inputTensor"></param>
    /// <param name="a1">M�nimo original</param>
    /// <param name="a2">M�ximo original</param>
    /// <param name="b1">M�nimo esperado</param>
    /// <param name="b2">M�ximo esperado</param>
    /// <returns></returns>
    Tensor NormaliseTensor(Tensor inputTensor, float a1, float a2, float b1, float b2)
    {
        var data = inputTensor.data.Download(inputTensor.shape);
        float[] normalized = new float[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            normalized[i] = Util.Normalise(data[i], a1, a2, b1, b2);
        }

        return new Tensor(inputTensor.shape, normalized);
    }

    #endregion
}