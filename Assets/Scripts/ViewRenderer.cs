using UnityEngine;
using System.Collections;
using UnityEngine.Profiling;

namespace GreyRock.LineOfSight{
    [ExecuteInEditMode]
    public class ViewRenderer : MonoBehaviour {
        public Material MaskMaterial;
        public Material BlurMaterial;
        public Material NDKMaterial;
        public LayerMask ViewLayer;
        [Range(1, 16)] public int BlurCamResDivider = 4;
        public Camera cam1 = null;
        public Camera cam2 = null;

        public RenderTexture ViewTexture;
        public RenderTexture BoardTexture;

        [Header("Nebel Des Krieges")]
        public Vector2Int BoardSize = new Vector2Int(14,14);
        [Range(8,64)] public int NDKDetail = 8;
        public UnityEngine.UI.RawImage NebelDesKrieges;


        void Start()
        {
            if(cam1 == null)
                cam1 = GetComponent<Camera>();
            
            /*if( cam2 == null){
                cam2 = new GameObject("LowResViewCamera", typeof(Camera)).GetComponent<Camera>();
                cam2.transform.parent = transform;
                cam2.transform.localPosition = Vector3.zero;
                cam2.orthographic = cam1.orthographic;
                cam2.orthographicSize = cam1.orthographicSize;
                cam2.clearFlags = CameraClearFlags.SolidColor;
                cam2.backgroundColor = Color.black;
                cam2.cullingMask = ViewLayer;
                cam2.depth = cam1.depth+1;
            }*/

            BoardTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0, RenderTextureFormat.RGB565);
            //BoardTexture.colorBuffer
            cam1.targetTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam1.forceIntoRenderTexture = true;

            ViewTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam2.targetTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam2.forceIntoRenderTexture = true;

            MaskMaterial.SetTexture("_MainTex", ViewTexture);
            MaskMaterial.SetTexture("_NebelTex", BoardTexture);

            NDKMaterial.SetTexture("_NebelTex", BoardTexture);
            NDKMaterial.SetTexture("_MaskTex", cam2.targetTexture);
            CreateNebelDesKrieges();
        }

        void CreateNebelDesKrieges(){
            NebelDesKrieges.texture = BoardTexture;
            /*NDKMask = new RenderTexture(BoardSize.x * NDKDetail, BoardSize.y * NDKDetail, 0);

            if(NDKMaterial==null)
                return;
                
            NDKMaterial.SetTexture("_TransitionTex", ViewMask);*/

        }

        void Update() {
            if (BlurMaterial != null)
                Graphics.Blit(cam2.targetTexture, ViewTexture, BlurMaterial);
                            
            if (NDKMaterial != null)
                Graphics.Blit(cam1.targetTexture, BoardTexture, NDKMaterial);
        }

        /*void OnRenderImage(RenderTexture src, RenderTexture dst) {
            Profiler.BeginSample("Render View Mesh");
            cam2.orthographicSize = cam1.orthographicSize;

            if (BlurMaterial != null)
                Graphics.Blit(cam2.targetTexture, ViewMask, BlurMaterial);

            if (NDKMaterial != null)
                Graphics.Blit(cam2.targetTexture, NDKMask, NDKMaterial);

            //MaskMaterial.SetTexture("_TransitionTex", ViewMask); // NO IDEA WHY THIS VARIABLE NEEDS TO BE SET CONSTANTLY NOW?! AND NOW IT DOESNT?!?!?!?! ASJASDEIJ ASWDI
            
            if (MaskMaterial != null)
                Graphics.Blit(src, dst, MaskMaterial);

            Profiler.EndSample();
        }*/
    }
}