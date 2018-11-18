using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class ViewRenderer : MonoBehaviour
{
    public Material MaskMaterial;
    public Material BlurMaterial;
    public LayerMask ViewLayer;
    public Camera cam1 = null;
    public Camera cam2 = null;
    bool a = true;

    public RenderTexture ViewMask;


    void Start()
    {
        if(cam1 == null)
            cam1 = GetComponent<Camera>();
        
        if( cam2 == null){
            cam2 = new GameObject("LowResViewCamera", typeof(Camera)).GetComponent<Camera>();
            cam2.transform.parent = transform;
            cam2.transform.localPosition = Vector3.zero;
            cam2.orthographic = cam1.orthographic;
            cam2.orthographicSize = cam1.orthographicSize;
            cam2.clearFlags = CameraClearFlags.SolidColor;
            cam2.backgroundColor = Color.black;
            cam2.cullingMask = ViewLayer;
            cam2.depth = cam1.depth+1;
        }

        ViewMask = new RenderTexture(cam1.pixelWidth/4, cam1.pixelHeight/4, 0);
        cam2.targetTexture = new RenderTexture(cam1.pixelWidth/4, cam1.pixelHeight/4, 0);
        cam2.forceIntoRenderTexture = true;

        MaskMaterial.SetTexture("_TransitionTex", ViewMask);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst) {
        cam2.orthographicSize = cam1.orthographicSize;

        if (BlurMaterial != null)
            Graphics.Blit(cam2.targetTexture, ViewMask, BlurMaterial);

        MaskMaterial.SetTexture("_TransitionTex", ViewMask); // NO IDEA WHY THIS VARIABLE NEEDS TO BE SET CONSTANTLY NOW?!
        
        if (MaskMaterial != null)
            Graphics.Blit(src, dst, MaskMaterial);
    }
}
