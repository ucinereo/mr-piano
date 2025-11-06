//using PassthroughCameraSamples;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Networking;

public class ModelManager : MonoBehaviour
{

    [SerializeField] private ModelAsset modelAsset;
    //[SerializeField] private WebCamTextureManager webcamTextureManager;
    [SerializeField] private TMP_Text speedText;

    private Model runtimeModel;
    private Worker worker;

    // State variables
    private bool isModelLoaded = false;

    [Header("[Editor Only] Convert to Sentis")]
    public ModelAsset OnnxModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeModel();
        //string outputDir = Path.Combine(Application.streamingAssetsPath, "Models");
        //if (!Directory.Exists(outputDir))
        //{
        //    Directory.CreateDirectory(outputDir);
        //}

        //string outputPath = Path.Combine(outputDir, "Model.nn");
        //ModelWriter.Save(outputPath, runtimeModel);
        //UnityEngine.Debug.Log("Model Serialized!");
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            RunInference(null);
        }
    }

    void InitializeModel()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        UnityEngine.Debug.Log("model loaded correctly!");
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
        UnityEngine.Debug.Log("Sentis Model Initialized!");
        WarmUpSentis();
        isModelLoaded = true;
    }

    void RunInference(Texture targetTexture)
    {
        // Run with random tensor

        //Texture texture = webcamTextureManager.WebCamTexture;
        //UnityEngine.Debug.Log(texture);
        //Tensor<float> tensor = TextureConverter.ToTensor(texture, 224, 244, 3);
        //UnityEngine.Debug.Log($"texture shape: {tensor.shape}");

        //using var tensor = LoadNormalizedTensor();

        TensorShape shape = new TensorShape(1, 3, 224, 224);
        using var tensor = new Tensor<float>(shape, clearOnInit: false);

        Stopwatch watch = Stopwatch.StartNew();
        worker.Schedule(tensor);
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        var cpuTensor = output.ReadbackAndClone();
        watch.Stop();



        UnityEngine.Debug.Log($"Inference took {watch.ElapsedMilliseconds}, output shape: {cpuTensor.shape}");

        speedText.text = watch.ElapsedMilliseconds.ToString() + " ms";

        tensor.Dispose();
        cpuTensor.Dispose();
    }

    void WarmUpSentis()
    {
        TensorShape shape = new TensorShape(1, 3, 224, 224);
        using var tensor = new Tensor<float>(shape, clearOnInit: false);
        //using var tensor = LoadNormalizedTensor();
        worker.Schedule(tensor);
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        UnityEngine.Debug.Log("Sentis warmed up!");
    }

    public static Tensor LoadNormalizedTensor()
    {
        string[] files = { "example_image_0.txt", "example_image_1.txt", "example_image_2.txt" };
        int H = 224, W = 224, C = 3;
        float[][] channels = new float[C][];

        for (int i = 0; i < C; i++)
        {
            string path = Path.Combine(Application.streamingAssetsPath, files[i]);
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            string text = File.ReadAllText(path);
            channels[i] = text
                .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();

            if (channels[i].Length != H * W)
                throw new Exception($"Channel {i} has {channels[i].Length} elements, expected {H * W}");
        }

        TensorShape shape = new TensorShape(1, C, H, W);
        Tensor<float> t = new Tensor<float>(shape);

        for (int c = 0; c < C; c++)
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    t[0, c, y, x] = channels[c][y * W + x];

        return t;
    }
}
