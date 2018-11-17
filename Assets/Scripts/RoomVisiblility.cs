using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GreyRock.RoomLighting {
	public class RoomVisiblility : MonoBehaviour {
		public List<Transform> doors;
		public List<RoomVisiblility> adjecentRooms;
		[Tooltip("Optimisation, when set to true changes to the room collider wont be updated automatically")] public bool staticRoomShape = true;
		public int layerMask = 8;
		[Tooltip("Max length of a ray")] public float RayLength = 20f; // Tie to size of room
		[Tooltip("Thicknes of the initial ray to detect the corner"), Range(0.0005f,0.01f)] public float RayThickness = 0.002f;
		[Tooltip("The margin of when a hit is counted as hitting the corner"), Range(0.01f,1.0f)] public float RayCornerHitMargin = 0.2f;
		[Tooltip("How many ray's should be cast per 0.1f of a door"), Range(0.1f,3)] public float DoorDetectionResolution = .5f;
		public Material material;

		private List<EdgeCollider2D> edges = new List<EdgeCollider2D>();
		public List<Door> Doors;
		private List<Vector2> roomCorners = new List<Vector2>();
		
		#if UNITY_EDITOR 
		private Dictionary<Vector2, Color> debugHit = new Dictionary<Vector2, Color>();
		private Dictionary<Vector3, string> debugText = new Dictionary<Vector3, string>();
		#endif

		private static HashSet<RoomVisiblility> allRooms = new HashSet<RoomVisiblility>();
		private static HashSet<BoxCollider2D> allDoors = new HashSet<BoxCollider2D>();
		private static Quaternion adjustmentAngle = Quaternion.AngleAxis(-0.1f, new Vector3(0,0,1));
		private static RoomVisiblility lastRoom;
		private const float TAU = Mathf.PI * 2;
		

		public Vector2 ViewSource = Vector2.zero;
		private bool SourceInRoom = false;
		private Mesh mesh = null;
		
		void Start () {
			PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
			UpdateCornersAndDoors(collider);
			collider.enabled = false;
		}

		void OnEnable(){
			allRooms.Add(this);
		}

		void OnDisable(){
			allRooms.Remove(this);
		}
		
        SortedDictionary<float, Vector2> hits =  new SortedDictionary<float, Vector2>();
		//List<Vector2> hits = new List<Vector2>();
        void Update() {
			Vector2 _viewSource = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if(_viewSource==ViewSource)
				return;

			#if UNITY_EDITOR 
			debugHit.Clear();
			debugText.Clear();
			#endif

            ViewSource = _viewSource;
			hits = new SortedDictionary<float, Vector2>();

            SourceInRoom = ContainsPt(this, ViewSource);

			if(SourceInRoom)
            { // If the view-source is in this room, start constructing view mesh from this room.
                if (lastRoom != this)
                { // While building the mesh in this room we don't want to be annoyed by the edges of other rooms so we'll disable them
                    lastRoom = this;
                    foreach (RoomVisiblility room in allRooms)
                    {
                        room.ToggleEdges(false);
                    }
                    ToggleEdges(true);
                }

                if (!staticRoomShape) // If true, update the room-shape each update I guess. Not supported
                    UpdateCornersAndDoors(GetComponent<PolygonCollider2D>());

                foreach (Vector2 corner in roomCorners) { //
                    Vector2 Source = ViewSource;
                    Vector2 Dir = (corner - ViewSource).normalized;

                    int hitsAfterCorner = 0;
                    bool keepGoing = true;
                    bool afterCorner = false;
                    Vector2 firstHitAfterCorner = Vector2.zero;
                    int i = 0;

                    while (keepGoing)
                    {
                        RaycastHit2D hit = afterCorner ? Physics2D.Raycast(Source, Dir, RayLength/*, layerMask*/ ) : Physics2D.CircleCast(Source, RayThickness, Dir, RayLength/*, layerMask*/);

                        if (hit && hit.collider.gameObject == gameObject)
                        { // IF THERE IS A HIT and that hit is on this room-edge
                            Debug.DrawLine(Source, hit.point);
                            if (Vector3.Distance(hit.point, corner) < RayCornerHitMargin)
                            { // IF THE HIT IS ON A CORNER
                                Source = corner + (Dir * 0.02f); // Next ray should start from the corner
                                afterCorner = true;
                            }
                            else { // IF THE HIT IS NOT ON A CORNER

                                if (afterCorner) { // If that hit is after a corner
                                    hitsAfterCorner++;

                                    if (firstHitAfterCorner == Vector2.zero) { // If it's the first hit after hitting the corner, save it.
                                        firstHitAfterCorner = hit.point;
                                    }
                                    Source = hit.point + (Dir * 0.02f);

                                    /*if(hitsAfterCorner>1){
										keepGoing = false;
										break;
									}*/
                                } else {
                                    RaycastHit2D[] hit2 = Physics2D.RaycastAll(hit.point + (Dir * 0.02f), Dir, RayLength/*, layerMask*/ );
                                    if (hit2.Length == 1) { //IF A RAY HITS A BORDER ON IT'S WAY TO THE CORNER BEFORE REACHING THE CORNER
                                        Source = corner + (Dir * 0.02f); // Next ray should start from the corner
                                        afterCorner = true;
                                    } else {
                                        keepGoing = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (hit.point == Vector2.zero) { // IF THERE IS NO HIT OR NOT ON THIS ROOM
                            keepGoing = false;
                            break;
                        }

                        if (++i > 500) { //PREVENT ETERNAL LOOP
                            Debug.LogError("[RoomVisibility] : Breaking eternal loop, this shouldn't happen");
                            keepGoing = false;
                            return;
                        }
                    }

                    float radian = Mathf.Atan2(Dir.y, Dir.x);
                    radian = radian > 0 ? radian : radian + TAU;

                    if (afterCorner == true && hitsAfterCorner % 2 == 0) { // 
                        hits.Add(radian, corner);
						#if UNITY_EDITOR
                        	debugHit.Add(corner, Color.blue);
						#endif
                    } else if (afterCorner == true && hitsAfterCorner % 2 != 0) {
                        float c = 0.01f;

                        RaycastHit2D hit1 = Physics2D.Raycast(ViewSource, adjustmentAngle * Dir, Vector2.Distance(ViewSource, firstHitAfterCorner) + 1/*, layerMask*/);
                        Debug.DrawLine(ViewSource, hit1.point, Color.blue);

                        if (Vector2.Distance(hit1.point, firstHitAfterCorner) < 0.1f) {
                            c = -c;
                        }

                        if (hit1.point == Vector2.zero) {
                            c = -1;
                        }

                        hits.Add(radian + c, firstHitAfterCorner);
                        hits.Add(radian, corner);
						
						#if UNITY_EDITOR
                        debugHit.Add(firstHitAfterCorner, Color.red);
                        debugHit.Add(corner, Color.blue);
						#endif
                    }
                }


                Vector2[] partHits = hits.Values.ToArray();
                Vector2[] sortedHits = new Vector2[hits.Values.ToArray().Length + 1];
                sortedHits[0] = ViewSource;
                for (int i = 1; i < sortedHits.Length; i++) {
                    sortedHits[i] = partHits[i - 1];
                }

                List<Door> visibleDoors = GetVisibleDoors(ViewSource);
				List<MeshData> doorViews = new List<MeshData>();

				foreach(Door door in visibleDoors){
					Ray ray1, ray2;
					GetDoorRays(door, _viewSource, partHits, out ray1, out ray2);
					door.adjecentRoom.GetViewsTroughDoor(ViewSource, ray1, ray2, new Door[1] { door }, ref doorViews, ref debugHit, ref debugText);
				}

                UpdateMesh(sortedHits, doorViews);
            }
            else if(lastRoom == this){
				foreach(RoomVisiblility room in allRooms){
					room.ToggleEdges(true);
				}
			}
        }

		void GetDoorRays(Door door, Vector2 viewPoint, Vector2[] viewHits, out Ray ray1, out Ray ray2){
			ray1.origin = door.point1;
			ray1.direction = (door.point1 - ViewSource).normalized;
			ray2.origin = door.point2;
			ray2.direction = (door.point2 - ViewSource).normalized;

			bool horizontal = door.point1.x == door.point2.x;
			bool pDouble = false;

			RaycastHit2D[] hits = Physics2D.LinecastAll(viewPoint, door.point1 - ray1.direction/10);
			if(hits.Length > 0) { // If we can't see this doorjamb
                pDouble = true;
                ray1 = GetHitOnDoor(door, viewHits, ray1, viewPoint, horizontal);
            }

            hits = Physics2D.LinecastAll(viewPoint, door.point2 - ray2.direction/10);
			if(hits.Length > 0){ // If we can't see this doorjamb
                ray2 = GetHitOnDoor(door, viewHits, ray1, viewPoint, horizontal);
				if(pDouble){
					//Double non visible jambs
				}
			}
		}



        /// <summary> Gets a list of doors in this room visible from a point </summary>
        /// <param name="viewSource">Point from which to look for doors</param>
        /// <returns>List of visible doors of this room</returns>
        private List<Door> GetVisibleDoors(Vector2 viewSource) {
			List<Door> visibleDoors = new List<Door>();
            foreach (Door door in Doors) {
                int doorSegments = Mathf.RoundToInt(Vector2.Distance(door.point1, door.point2) / (0.1f / DoorDetectionResolution));
                float segmentLength = Vector2.Distance(door.point1, door.point2) / doorSegments;
                Vector2 nextSegmentDir = (door.point2 - door.point1).normalized;

                for (int i = 0; i <= doorSegments; i++) {
                    Vector2 segmentPoint = door.point1 + (nextSegmentDir * (i * segmentLength));
                    Vector2 dir = segmentPoint - viewSource;
                    RaycastHit2D[] hits = Physics2D.RaycastAll(viewSource, dir, Vector2.Distance(viewSource, segmentPoint) - 0.01f);
                    if (hits.Count() == 0) {
						visibleDoors.Add(door);
                        break;
                    }else{
						#if UNITY_EDITOR
							foreach(RaycastHit2D hit in hits){
								//Debug.DrawLine(viewSource, hit.point, Color.gray);
							}
						#endif
					}
                }
            }

			return visibleDoors;
        }

        private void UpdateCornersAndDoors(PolygonCollider2D collider){
			for(int ii = 0; ii < collider.pathCount; ii++){
				Vector2[] path = collider.GetPath(ii);

				for(int i = 0; i < path.Length; i++){
					roomCorners.Add(transform.TransformDirection(path[i]));
					EdgeCollider2D edge = gameObject.AddComponent<EdgeCollider2D>();
					edge.points = new Vector2[2]{path[i],path[i==path.Length-1? 0 : i+1]};
					edges.Add(edge);
					
					foreach(RoomVisiblility adjecentRoom in adjecentRooms){
						foreach(Transform t in doors){
							if( GetDistPointToLine(edge.points[0], edge.points[1], t.position) > 0.1f)
								continue;

							if(adjecentRoom.doors.Contains(t)){
								Vector2 Intersection1, Intersection2;
								int intersections = BetweenLineAndCircle(t.position, 0.34f, edge.points[0], edge.points[1], out Intersection1, out Intersection2 );
								if(intersections==2){
									Door door;
									door.point1 = new Vector2(Intersection1.x, Intersection1.y);
									door.point2 = new Vector2(Intersection2.x, Intersection2.y);
									door.adjecentRoom = adjecentRoom;
									door.thisRoom = this;
									Doors.Add(door);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Outputs all view meshes trough doors, also checks trough doors in the room you're looking in
		/// </summary>
		/// <param name="viewPoint">Point from which to view</param>
		/// <param name="viewRay1">First ray-direction trough door</param>
		/// <param name="viewRay2">Second ray-direction trough door</param>
		/// <param name="doors">Doors trough which to view</param>
		/// <param name="viewMeshes">The resulting meshes</param>
		public void GetViewsTroughDoor(Vector2 viewPoint, Ray viewRay1, Ray viewRay2, Door[] doors, ref List<MeshData> viewMeshes, ref Dictionary<Vector2, Color> debugHit, ref Dictionary<Vector3, string> debugText){
			ToggleEdges(true);
			List<Vector3> vertices = new List<Vector3>();

			foreach(Door door in doors){
				RaycastHit2D hit1 = Physics2D.Raycast(viewRay1.origin + viewRay1.direction/10, viewRay1.direction, RayLength);
				RaycastHit2D hit2 = Physics2D.Raycast(viewRay2.origin + viewRay2.direction/10, viewRay2.direction, RayLength);

				vertices.Add(viewRay1.origin);
				vertices.Add(hit1.point);

				foreach(Vector2 corner in roomCorners){
					Vector2 dir = (corner - viewPoint).normalized;
					if(AngleIsBetween(viewRay1.direction,viewRay2.direction,dir)){ //Corner of the room is between the two outer angles
						RaycastHit2D doorHit = Physics2D.Linecast(viewPoint, corner);
						RaycastHit2D[] cornerCheck = Physics2D.LinecastAll(doorHit.point + dir/10, corner - dir/10);
						if(cornerCheck.Length == 0){
							vertices.Add(doorHit.point);
							vertices.Add(corner);
							#if UNITY_EDITOR
								if(!debugHit.ContainsKey(corner)){
									debugHit.Add(doorHit.point, Color.green);
									debugHit.Add(corner, Color.green);
								}
							#endif
						}
					}
				}

				vertices.Add(viewRay2.origin);
				vertices.Add(hit2.point);

				#if UNITY_EDITOR
					if(!debugHit.ContainsKey(hit1.point) && !debugHit.ContainsKey(hit2.point)){
						Debug.DrawLine(viewRay1.origin + viewRay1.direction/10, hit1.point, Color.cyan);
						Debug.DrawLine(viewRay2.origin + viewRay2.direction/10, hit2.point, Color.cyan);
						debugHit.Add(hit1.point, Color.cyan);
						debugHit.Add(hit2.point, Color.cyan);
					}
				#endif
			}

			viewMeshes.Add(CreateDoorViewMesh(ref vertices));

			ToggleEdges(false);
		}

		private MeshData CreateDoorViewMesh(ref List<Vector3> vertices3D){
			int[] triangles = new int[(vertices3D.Count()-2)*3];
			MeshData meshData;

			int A1 = 0;
			int A2 = 3;
			int A3 = 1;
			int B1 = 0;
			int B2 = 2;
			int B3 = 3;

			for(int i = 0; i <= triangles.Length - 6; i += 6){
				print(i + "<" + (triangles.Length - 6));
				triangles[i] = A1;
				triangles[i+1] = A2;
				triangles[i+2] = A3;
				
				triangles[i+3] = B1;
				triangles[i+4] = B2;
				triangles[i+5] = B3;
				A1 += 2; 
				A2 += 2;
				A3 += 2; 
				B1 += 2;
				B2 += 2; 
				B3 += 2;
			}

			//print(triangles[triangles.Length - 1-6]); // Laatst ingevulde waarde

			meshData.vertices = vertices3D.ToArray();
			meshData.triangles = triangles;

			return meshData;
		}

		List<GameObject> test = new List<GameObject>();
		private void UpdateMesh(Vector2[] vertices2D, List<MeshData> additionalMeshes){
			var vertices3D = System.Array.ConvertAll<Vector2, Vector3>(vertices2D, ve => ve);

			if(mesh == null){
				mesh = new Mesh();
				gameObject.AddComponent<MeshRenderer>();
				GetComponent<MeshRenderer>().material = material;
				gameObject.AddComponent<MeshFilter>();
			}

			if(GetComponent<MeshRenderer>())
				GetComponent<MeshRenderer>().enabled = true;

			var triangles = new int[3 * (vertices3D.Length - 1)];
			int C1 = 0;
			int C2 = 1;
			int C3 = 2;
		
			for(int x = 0; x < triangles.Length-3; x+=3) {
				triangles[x] = C1;
				triangles[x+1] = C3++;
				triangles[x+2] = C2++;
			}
			
			triangles[triangles.Length - 3] = 0;
			triangles[triangles.Length - 2] = 1;
			triangles[triangles.Length - 1] = C3-1;

			int verts = vertices3D.Length;
			int tris = triangles.Length;
			int v = verts;
			int t = tris;
			foreach(MeshData meshData in additionalMeshes){
				verts += meshData.vertices.Length;
				tris += meshData.triangles.Length;
			}

        	Array.Resize(ref vertices3D, verts);
        	Array.Resize(ref triangles, tris);

			foreach(GameObject go in test)
				Destroy(go);

			foreach(MeshData meshData in additionalMeshes){
				int i = 0;
				int offset = v;
				foreach(Vector3 vert in meshData.vertices){
					//print(v + "/" + vertices3D.Length);
					vertices3D[v++] = vert;
					debugText.Add(vert, i++.ToString());
				}

				foreach(int tri in meshData.triangles){
					//print(t + "/" + triangles.Length);
					triangles[t++] = tri + offset;
				}
			}

			mesh.Clear();
			mesh.vertices = vertices3D;
			mesh.triangles = triangles;


			GetComponent<MeshFilter>().mesh = mesh;
		}


		void ToggleEdges(bool state){
			if(GetComponent<MeshRenderer>())
				GetComponent<MeshRenderer>().enabled = false;
			foreach(EdgeCollider2D edge in edges)
				edge.enabled = state;
		}

		#region MATH FUNCTIONS
		static Ray GetHitOnDoor(Door door, Vector2[] viewHits, Ray ray, Vector2 ViewSource, bool horizontal) {
            foreach (Vector2 viewHit in viewHits) {
                if ((horizontal && Mathf.Abs(viewHit.y - door.point1.y) < 0.01f && viewHit.x > Mathf.Min(door.point1.x, door.point2.x) && viewHit.x < Mathf.Max(door.point1.x, door.point2.x))
                || (!horizontal && Mathf.Abs(viewHit.x - door.point1.x) < 0.01f && viewHit.y > Mathf.Min(door.point1.y, door.point2.y) && viewHit.y < Mathf.Max(door.point1.y, door.point2.y))){

                    ray.origin = viewHit;
                    ray.direction = (viewHit - ViewSource).normalized;
                    break;
                }
            }

            return ray;
        }
		static float GetDistPointToLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
		{
			float tI = ((lineEnd.x - lineStart.x) * (point.x - lineStart.x) + (lineEnd.y - lineStart.y) * (point.y - lineStart.y)) / Mathf.Pow(Vector2.Distance(lineStart, lineEnd ), 2);
			float dP = ((lineEnd.x - lineStart.x) * (point.y - lineStart.y) - (lineEnd.y - lineStart.y) * (point.x - lineStart.x)) / Vector2.Distance(lineStart, lineEnd );

			if (tI >= 0d && tI <= 1d)
				return Mathf.Abs(dP);
			else
				return Mathf.Min(Vector2.Distance(lineStart, point ), Vector2.Distance(point, lineEnd ));
		}

		static bool AngleIsBetween(Vector2 direction1, Vector2 direction2, Vector2 checkedDir) {
				float str, end, rad;
				str = Mathf.Atan2(direction1.y, direction1.x); //GETTING ANGLES FROM DIRECTION-VECTORS (in radians)
				end = Mathf.Atan2(direction2.y, direction2.x);
				rad = Mathf.Atan2(checkedDir.y, checkedDir.x);

				str = str > 0 ? str : str + TAU; //NORMALISING ANGLES
				end = end > 0 ? end : end + TAU;
				rad = rad > 0 ? rad : rad + TAU;
				

				if(str <= end) {
					if(end - str <= Mathf.PI) {
						return str <= rad && rad <= end;
					} else {
						return end <= rad || rad <= str;
					}
				} else {
					if(str - end <= Mathf.PI) {
						return end <= rad && rad <= str;
					} else {
						return str <= rad || rad <= end;
					}
				}
		}
		
        static bool ContainsPt(RoomVisiblility room, Vector2 point) {
            Vector2 FarAway = new Vector2(room.transform.position.x, room.transform.position.y + 20);
            Vector2 Dir = (point - FarAway).normalized;
            Vector2 Point = FarAway;
			int hits = 0;

            bool fugg = true;
            int i = 0;

            while (fugg) {
                foreach (RaycastHit2D hit in Physics2D.LinecastAll(Point, point/*, layerMask*/)) {
                    if (hit.collider.gameObject == room.gameObject) {
                        hits++;
                        Point = hit.point + (Dir / 100f);
                    } else {
                        fugg = false;
                    }
                }
                if (++i > 500) {
                    //print("BREAK");
                    fugg = false;
                }
            }
            if (hits % 2 != 0)
                return true;

			return false;
        }

		static int BetweenLineAndCircle( Vector2 circleCenter, float circleRadius, Vector2 point1, Vector2 point2, out Vector2 intersection1, out Vector2 intersection2) {
			float t;
		
			var dx = point2.x - point1.x;
			var dy = point2.y - point1.y;
		
			var a = dx * dx + dy * dy;
			var b = 2 * (dx * (point1.x - circleCenter.x) + dy * (point1.y - circleCenter.y));
			var c = (point1.x - circleCenter.x) * (point1.x - circleCenter.x) + (point1.y - circleCenter.y) * (point1.y - circleCenter.y) - circleRadius * circleRadius;
		
			var determinate = b * b - 4 * a * c;
			if ((a <= 0.0000001) || (determinate < -0.0000001))
			{
				// No real solutions.
				intersection1 = Vector2.zero;
				intersection2 = Vector2.zero;
				return 0;
			}
			if (determinate < 0.0000001 && determinate > -0.0000001)
			{
				// One solution.
				t = -b / (2 * a);
				intersection1 = new Vector2(point1.x + t * dx, point1.y + t * dy);
				intersection2 = Vector2.zero;
				return 1;
			}
			
			// Two solutions.
			t = (float)((-b + Mathf.Sqrt(determinate)) / (2 * a));
			intersection1 = new Vector2(point1.x + t * dx, point1.y + t * dy);
			t = (float)((-b - Mathf.Sqrt(determinate)) / (2 * a));
			intersection2 = new Vector2(point1.x + t * dx, point1.y + t * dy);
		
			return 2;
		}
		#endregion

		#region GIZMO'S
		float min = 10;

		private static Color SetA(Color color, float a){
			return new Color(color.r, color.g, color.b, a);
		}

		void OnDrawGizmos(){
			foreach(Vector2 pt in debugHit.Keys){
				Gizmos.color = debugHit[pt];
				Gizmos.DrawSphere(new Vector3(pt.x, pt.y, 1), 0.08f);
			}

			foreach(Vector3 pt in debugText.Keys)
				TextGizmo.Draw(new Vector3(pt.x, pt.y, 1), debugText[pt]);
		}
		#endregion


        [System.Serializable]
		public struct Door{
			public Vector2 point1;
			public Vector2 point2;
			public RoomVisiblility adjecentRoom;
			public RoomVisiblility thisRoom;
		}

        [System.Serializable]
		public struct Ray{
			public Vector2 origin;
			public Vector2 direction;
		}

        [System.Serializable]
		public struct MeshData{
			public Vector3[] vertices;
			public int[] triangles;
		}
	}

}

