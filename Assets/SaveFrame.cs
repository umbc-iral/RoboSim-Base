using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveFrame : MonoBehaviour
{
    public int Width = 1920;
    public int Height = 1080;
    public string Path = "./screenshot.png";

    private bool done = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (!done)
        {
            done = true;
            RenderTexture rt = new RenderTexture(Width, Height, 24);
            Camera camera = GetComponent<Camera>();
            camera.targetTexture = rt;
            Texture2D screenshot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            camera.Render();
            RenderTexture.active = rt;
            screenshot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            camera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            byte[] bytes = screenshot.EncodeToPNG();
            System.IO.File.WriteAllBytes(Path, bytes);
        }
    }
}
