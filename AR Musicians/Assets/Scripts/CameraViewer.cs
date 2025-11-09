using Meta.XR;
using System;
using UnityEngine;

public class CameraViewer : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess cameraAccess;
    [SerializeField] private Renderer quadRenderer;
    [SerializeField] private ModelManager modelManager;

    private Texture2D picture;

    private void Start()
    {
        UnityEngine.Android.Permission.RequestUserPermission("horizonos.permission.HEADSET_CAMERA");
    }

    // Update is called once per frame
    void Update()
    {
        //if (cameraAccess.enabled)
        //{
        //    Texture texture = cameraAccess.GetTexture();
        //    quadRenderer.material.mainTexture = texture;
        //}


        if (cameraAccess.IsPlaying)
        {
            PassthroughCameraAccess.CameraIntrinsics intrinsics = cameraAccess.Intrinsics;
            Pose pose = cameraAccess.GetCameraPose();
            // Ray ray = cameraAccess.ViewportPointToRay(normalizedViewportPoint);

            // Newly added properties:
            Vector2Int resolution = cameraAccess.CurrentResolution;
            DateTime timestamp = cameraAccess.Timestamp;
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                TakePicture();
            }
        }
    }

    void TakePicture()
    {
        Vector2Int resolution = cameraAccess.CurrentResolution;
        int width = resolution[0];
        int height = resolution[1];


        if (picture == null)
        {
            picture = new Texture2D(width, height);
        }

        Color32[] colors = new Color32[width * height];
        colors = cameraAccess.GetColors().ToArray();
        picture.SetPixels32(colors);
        picture.Apply();

        //Vector2[] kpts = modelManager.RunInference(cameraAccess.GetTexture());
        modelManager.RunInferenceAsync(cameraAccess.GetTexture());

        //drawQuad(picture, kpts);
        //picture.Apply();
        quadRenderer.material.mainTexture = picture;
    }

    void drawQuad(Texture2D picture, Vector2[] kpts)
    {
        int width = 3;
        foreach (Vector2 kpt in kpts)
        {
            int x = Mathf.FloorToInt(kpt.x);
            int y = Mathf.FloorToInt(kpt.y);
            for (int i = -width; i < width; i++)
            {
                for (int j = -width; j < width; j++)
                {
                    picture.SetPixel(x + i, y + j, Color.red);
                }
            }
        }
    }
}
