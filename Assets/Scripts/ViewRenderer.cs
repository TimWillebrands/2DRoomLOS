using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class ViewRenderer : MonoBehaviour
{
    public Material TransitionMaterial;
    public Material BlurMaterial;
    public Camera cam1 = null;
    public Camera cam2 = null;

    public RenderTexture ViewMask;


    void Start()
    {
        
        if( cam2 == null || cam1 == null){
            cam1 = GetComponent<Camera>();
            cam2 = Instantiate<Camera>(cam1, Vector3.zero, Quaternion.identity, transform);
            Destroy(cam2.GetComponent<ViewRenderer>());
            Debug.Log("cam2", cam2);
        }

        ViewMask = new RenderTexture(cam1.pixelWidth/4, cam1.pixelHeight/4, 0);
        //ViewMask.Create();

        cam2.targetTexture = new RenderTexture(cam1.pixelWidth/4, cam1.pixelHeight/4, 0);
        cam2.forceIntoRenderTexture = true;

        TransitionMaterial.SetTexture("_TransitionTex", ViewMask);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst) {    
        if (BlurMaterial != null)
            Graphics.Blit(cam2.targetTexture, ViewMask, BlurMaterial);
        
        if (TransitionMaterial != null)
            Graphics.Blit(src, dst, TransitionMaterial);
    }
}
