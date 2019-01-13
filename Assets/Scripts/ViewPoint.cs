using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GreyRock.LineOfSight{
	[RequireComponent(typeof(MeshRenderer),typeof(MeshFilter))]
	public class ViewPoint : MonoBehaviour {
			public Transform ViewOrigin = null;
			private Mesh mesh = null;
			private MeshRenderer meshRenderer = null;
			private MeshFilter meshFilter = null;

			void Start() {
				if(mesh == null){
					mesh = new Mesh();
					meshRenderer = GetComponent<MeshRenderer>();
					meshFilter = GetComponent<MeshFilter>();
				}
			}

			void Update() {
				Vector2 _viewSource;
				if(ViewOrigin != null)
					_viewSource = ViewOrigin.position;
				else
					_viewSource = Camera.main.ScreenToWorldPoint(Input.mousePosition);

				foreach(RoomVisiblility room in RoomVisiblility.allRooms){
					Profiler.BeginSample("Check Origin Room");
					bool isInRoom = RoomVisiblility.ContainsPt(room, _viewSource);
					Profiler.EndSample();
					if(isInRoom){
						Profiler.BeginSample("Update View Mesh");
						UpdateMesh(room.GenerateViewMeshData(_viewSource));
						Profiler.EndSample();
						break;
					}
				}
			}

			private void UpdateMesh(MeshData[] viewMeshes){

				meshRenderer.enabled = true;

				MeshData roomView = viewMeshes[0];

				int verts = roomView.vertices.Length;
				int tris = roomView.triangles.Length;
				int v = verts;
				int t = tris;
				//foreach(MeshData meshData in viewMeshes){
				for(int i = 1; i<viewMeshes.Length; i++){
					verts += viewMeshes[i].vertices.Length;
					tris += viewMeshes[i].triangles.Length;
				}

				Vector3[] vertices = new Vector3[verts];
				int[] triangles = new int[tris];
				//Array.Resize(ref vertices3D, verts);
				//Array.Resize(ref triangles, tris);

				Array.Copy(viewMeshes[0].vertices, vertices, viewMeshes[0].vertices.Length);
				Array.Copy(viewMeshes[0].triangles, triangles, viewMeshes[0].triangles.Length);

				for(int i = 1; i<viewMeshes.Length; i++){
					//int i = 0;
					int vOffset = v;
					/*Array.Copy(viewMeshes[i].vertices, 0, vertices, vOffset, viewMeshes[i].vertices.Length);
					v += viewMeshes[i].vertices.Length;
					
					int tOffset = t;
					Array.Copy(viewMeshes[i].triangles, 0, triangles, tOffset, viewMeshes[i].triangles.Length);
					t += viewMeshes[i].triangles.Length;*/

					foreach(Vector3 vert in viewMeshes[i].vertices){
						vertices[v++] = vert;
					}

					foreach(int tri in viewMeshes[i].triangles){
						//print(t + "/" + triangles.Length);
						triangles[t++] = tri + vOffset;
					}
				}

				mesh.Clear();
				mesh.vertices = vertices;
				mesh.triangles = triangles;

				meshFilter.mesh = mesh;
			}
	}
}