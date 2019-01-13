using UnityEngine;
using System.Collections;
using UnityEngine.Profiling;

//[ExecuteInEditMode]
namespace GreyRock.LineOfSight{
    public class ViewRenderer : MonoBehaviour {
        public Material MaskMaterial;
        public Material BlurMaterial;
        public LayerMask ViewLayer;
        [Range(0, 16)] public int BlurCamResDivider = 4;
        public Camera cam1 = null;
        public Camera cam2 = null;

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

            ViewMask = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam2.targetTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam2.forceIntoRenderTexture = true;

            MaskMaterial.SetTexture("_TransitionTex", ViewMask);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            Profiler.BeginSample("Render View Mesh");
            cam2.orthographicSize = cam1.orthographicSize;

            if (BlurMaterial != null)
                Graphics.Blit(cam2.targetTexture, ViewMask, BlurMaterial);

            //MaskMaterial.SetTexture("_TransitionTex", ViewMask); // NO IDEA WHY THIS VARIABLE NEEDS TO BE SET CONSTANTLY NOW?! AND NOW IT DOESNT?!?!?!?! ASJASDEIJ ASWDI
            
            if (MaskMaterial != null)
                Graphics.Blit(src, dst, MaskMaterial);
            Profiler.EndSample();
        }
    }
}