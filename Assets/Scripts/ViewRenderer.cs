using UnityEngine;
using System.Collections;
using UnityEngine.Profiling;

namespace GreyRock.LineOfSight{
    //[ExecuteInEditMode]
    public class ViewRenderer : MonoBehaviour {
        public Material MaskMaterial;
        public Material BlurMaterial;
        public Material NDKMaterial;
        public LayerMask ViewMaskLayer;
        [Range(1, 16)] public int BlurCamResDivider = 4;
        public Camera cam1 = null;
        public Camera cam2 = null;

        public RenderTexture ViewTexture;
        public RenderTexture BoardTexture;

        [Header("Nebel Des Krieges")]
        public Vector2Int BoardSize = new Vector2Int(14,14);
        [Range(8,64)] public int NDKDetail = 8;
        public UnityEngine.UI.RawImage NebelDesKrieges;

        GameObject canvas;


        void Start() {
            if(cam1 == null)
                cam1 = GetComponent<Camera>();

            cam2.orthographicSize = cam1.orthographicSize;
            cam2.transform.localPosition = Vector3.zero;
            cam2.transform.localRotation = Quaternion.identity;

            BoardTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0, RenderTextureFormat.RGB565);
            cam1.targetTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam1.forceIntoRenderTexture = true;

            ViewTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam2.targetTexture = new RenderTexture(cam1.pixelWidth/BlurCamResDivider, cam1.pixelHeight/BlurCamResDivider, 0);
            cam2.forceIntoRenderTexture = true;

            MaskMaterial.SetTexture("_MainTex", ViewTexture);
            MaskMaterial.SetTexture("_NebelTex", BoardTexture);

            NDKMaterial.SetTexture("_NebelTex", BoardTexture);
            NDKMaterial.SetTexture("_MaskTex", cam2.targetTexture);
            NebelDesKrieges.texture = BoardTexture;
            CreateCanvas();
        }

        void CreateCanvas(){
            canvas = new GameObject();
            canvas.layer = LayerMask.NameToLayer("ViewBlocker");
            //canvas.transform.position = transform.position;
            canvas.name = "ViewBlocker&FOW";
            canvas.transform.SetParent(transform, false);
            var meshRenderer = canvas.AddComponent<MeshRenderer>();
            var meshFilter = canvas.AddComponent<MeshFilter>();
            meshRenderer.material = MaskMaterial;

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[4]{
                new Vector3(-(cam1.aspect*cam1.orthographicSize), cam1.orthographicSize,-0.001f),
                new Vector3((cam1.aspect*cam1.orthographicSize), cam1.orthographicSize,-0.001f),
                new Vector3(-(cam1.aspect*cam1.orthographicSize), -cam1.orthographicSize,-0.001f),
                new Vector3((cam1.aspect*cam1.orthographicSize), -cam1.orthographicSize,-0.001f),
            };
            mesh.triangles = new int[6]{
                0,3,2,0,1,3
            };
            
            mesh.normals = new Vector3[4]{
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            }; 

            mesh.uv = new Vector2[4]{
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0, 0),
                new Vector2(1, 0),
            };

            meshFilter.mesh = mesh;
        }

        /*void OnDestroy() {
            Destroy(canvas);
        }*/

        void Update() {
            if (BlurMaterial != null)
                Graphics.Blit(cam2.targetTexture, ViewTexture, BlurMaterial);
                            
            if (NDKMaterial != null)
                Graphics.Blit(cam1.targetTexture, BoardTexture, NDKMaterial);
        }
    }
}