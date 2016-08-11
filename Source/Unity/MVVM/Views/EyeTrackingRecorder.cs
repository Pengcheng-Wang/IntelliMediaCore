#region Copyright 2015 North Carolina State University
//---------------------------------------------------------------------------------------
// Copyright 2015 North Carolina State University
//
// Computer Science Department
// Center for Educational Informatics
// http://www.cei.ncsu.edu/
//
// All rights reserved
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
// GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//---------------------------------------------------------------------------------------
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;


#endregion

using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace IntelliMedia
{
    [RequireComponent (typeof (Camera))]
    public class EyeTrackingRecorder : MonoBehaviour
    {
        private class Target
        {
            public Transform ViewedObj { get; set; }

			public float AveragePupilDiameter { get { return (UpdateCount > 0 ? AccumulatedPupilDiameter / UpdateCount : 0); }}
			public float MaxPupilDiameter { get; private set; }
			public float MinPupilDiameter { get; private set; }
			public float AccumulatedPupilDiameter { get; private set; }
			public int UpdateCount { get; private set; }
			public EyeTrackingService Service { get; set; }

            private float GazeStartTime { get; set; }

			public Target()
			{
				Clear();
			}				

			public void Update(Transform obj, float pupilDiameter)
			{
				if (ViewedObj != obj)
                {
					OnTargetSwitching();
					ViewedObj = obj;
                }

				UpdatePupilDiameter(pupilDiameter);
				++UpdateCount;
			}

			private void UpdatePupilDiameter(float pupilDiameter)
			{
				AccumulatedPupilDiameter += pupilDiameter;
				MaxPupilDiameter = Mathf.Max(MaxPupilDiameter, pupilDiameter);
				MinPupilDiameter = Mathf.Min(MinPupilDiameter, pupilDiameter);
			}

			private void OnTargetSwitching()
			{
				// Was there a previous target and was the threshold met?
				if (ThresholdMet)
				{
					LogLookedAtTarget();
				}

				Clear();

				GazeStartTime = Time.time;
			}
			
			public void Clear()
			{
				ViewedObj = null;
				UpdateCount = 0;
				AccumulatedPupilDiameter = 0;
				MaxPupilDiameter = 0;
				MinPupilDiameter = float.MaxValue;
				GazeStartTime = 0;
            }

            public void LogLookedAtTarget()
            {
                List<object> keyValues = new List<object>();

                keyValues.Add("Duration");
                keyValues.Add(Duration);

				keyValues.Add("AveragePupilDiameter");
				keyValues.Add(AveragePupilDiameter);

				keyValues.Add("MaxPupilDiameter");
				keyValues.Add(MaxPupilDiameter);

				keyValues.Add("MinPupilDiameter");
				keyValues.Add(MinPupilDiameter);
					
				keyValues.Add("TargetType");
                keyValues.Add(TargetType);

//                if (Region != null)
//                {
//                    keyValues.Add("Region");
//                    keyValues.Add((Region.Name != null ? Region.Name : "null"));
//
//                    keyValues.Add("ControlType");
//                    keyValues.Add(Region.Type);
//
//                    keyValues.Add("Content");
//                    keyValues.Add((Region.Content != null ? Region.Content : "null"));
//                }

                TraceLog.Player(TraceLog.Action.LookedAt, Name, keyValues);
            }

            public string TargetType
            {
                get
                {
                    string targetType = null;

					if (ViewedObj.GetComponent<RectTransform>() != null)
                    {
                        targetType = "GUI";
                    }
//                    else if (Obj.GetComponent<Human>())
//                    {
//                        targetType = "Human";
//                    }
//                    else if (Obj.GetComponent<BookGUI>())
//                    {
//                        targetType = "Book";
//                    }
//                    else if (Obj.GetComponent<ResearchArticleGUI>())
//                    {
//                        targetType = "ResearchArticle";
//                    }
//                    else if (Obj.GetComponent<PosterGUI>())
//                    {
//                        targetType = "SmallPoster";
//                    }
//					else if (Obj.GetComponent<LargePosterIdentifier>())
//					{
//						targetType = "LargePoster";
//					}
//					else if (Obj.GetComponent<PackableObject>())
//                    {
//                        System.Array array = System.Enum.GetValues(typeof(Foods));
//                        if (array.Cast<Foods>().Any(f => string.Compare(f.ToString(), Obj.name, System.StringComparison.CurrentCultureIgnoreCase) == 0))
//                        {
//                            targetType = "Food";
//                        }
//                        else
//                        {
//                            targetType = "PackableObject";
//                        }
//                    }
                    else
                    {
                        targetType = "GameObject";
                    }

                    return targetType;
                }
            }

            public bool IsNull
            {
                get
                {
                    return ViewedObj == null;
                }
            }

            public float Duration
            {
                get
                {
                    return (IsNull ? 0 : Time.time - GazeStartTime);
                }
            }

            public bool ThresholdMet             
            { 
                get
                {
					return Duration > Service.Settings.AttentionThreshold;
                }
            }

            public string Name
            {
                get
                {
                    AdditionalTraceData additionalInfo = ViewedObj.GetComponent<AdditionalTraceData>();
                    string objName = ViewedObj.name;
                    if (additionalInfo != null && !string.IsNullOrEmpty(additionalInfo.userFriendlyName))
                    {
                        objName = additionalInfo.userFriendlyName;
                    }

                    return string.IsNullOrEmpty(objName) ? "<object doesn't have a name>" : objName;
                }
            }
        }

        public int guiDepth;
        public Texture2D positionIndicator;
        public GUIStyle textStyle;

        private Camera EyeTrackingCamera { get; set; }

        private GazeEventArgs GazeInfo { get; set; }

        private Transform EyeTrackingCameraTransform;
        private Target AttentionTarget = new Target();

        private bool newGazeInfo;
        private bool cameraMoved;

        private Rect positionIndicatorRect = new Rect(428, 45, 64, 64);
		private EyeTrackingService eyeTrackingService;


		// After Dependency Injection, configure the app
		[Inject]
		public void Init(EyeTrackingService eyeTrackingService)
		{
			this.eyeTrackingService = eyeTrackingService;
		}

        void Start()
        {
            if (positionIndicator == null)
            {
                DebugLog.Error("GazeRecorder PositionIndicator texture not set.");
            }
            else
            {
                //positionIndicatorRect = new Rect(0, 0, positionIndicator.width, positionIndicator.height);
            }

            if (GetComponent<Camera>() == null)
            {
                DebugLog.Error("GazeRecorder requires camera component on the same GameObject");
                return;
            }
            // Use local members to speed up access
            EyeTrackingCamera = GetComponent<Camera>();

//			if (eyeTrackingService == null || !eyeTrackingService.IsEnabled)
//            {
//                enabled = false;
//            }
        }

        void OnEnable()
        {
			eyeTrackingService.GazeChanged += HandleGazeChanged;
			AttentionTarget.Service = eyeTrackingService;
        }

        void OnDisable()
        {
            // Record the gaze duration for the last object being viewed
            AttentionTarget.Clear();

			if (eyeTrackingService != null)
			{
				eyeTrackingService.GazeChanged -= HandleGazeChanged;
			}
        }

        void HandleGazeChanged(object sender, GazeEventArgs e)
        {
            GazeInfo = e;
            newGazeInfo = true;
        }

        void UpdateAttentionTarget()
        {
            if (GazeInfo == null)
            {
                AttentionTarget.Clear();
                return;
            }

            // position is defined in screen space. The bottom-left of the screen is (0,0)
            Vector3 screenPoint = new Vector3(GazeInfo.LeftX,
                                              GazeInfo.LeftY,
                                              0);

			GameObject hit = ScreenPointToGameObject(screenPoint);
			if (hit != null)
            {
				AttentionTarget.Update(hit.transform, GazeInfo.LeftPupilDiameter);
			}
			else
            {
                AttentionTarget.Clear();                    
            }

            // Reset dirty flags
            newGazeInfo = false;
            cameraMoved = false;
        }

		GameObject ScreenPointToGameObject(Vector3 screenPoint)
		{
			// Cast ray to detect GUI 
			var pointer = new PointerEventData(EventSystem.current);
			// convert to a 2D position
			pointer.position = screenPoint;
			var raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointer, raycastResults);
			if (raycastResults.Count > 0) {
				GameObject target = raycastResults[0].gameObject;
				GameObject button = FindGuiControl(target);
				target = (button != null ? button : target);
				return target;
			}	

			// Cast ray to detect 3D / 2D object
			Ray ray = EyeTrackingCamera.ScreenPointToRay(screenPoint);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit))
			{
				return hit.transform.gameObject;
			}

			return null;
		}

		GameObject FindGuiControl(GameObject child)
		{
			GameObject control = FindParentOfType<Button>(child);
			if (control == null)
			{
				control = FindParentOfType<Slider>(child);
			}
			if (control == null)
			{
				control = FindParentOfType<Toggle>(child);
			}
			if (control == null)
			{
				control = FindParentOfType<InputField>(child);
			}

			return control;
		}

		GameObject FindParentOfType<T>(GameObject child)
		{
			Contract.ArgumentNotNull("child", child);

			for (Transform current = child.transform; current != null; current = current.parent)
			{
				if (current.GetComponent<T>() != null)
				{
					return current.gameObject;
				}
			}

			return null;
		}

        float lastCameraTransformUpdate;

        void DetectCameraMovememnt()
        {
            if ((Time.time - lastCameraTransformUpdate) > 1f / eyeTrackingService.Settings.CameraMovementDetectionFrequency && EyeTrackingCameraTransform != EyeTrackingCamera.transform)
            {
                EyeTrackingCameraTransform = EyeTrackingCamera.transform;
                cameraMoved = true;
                lastCameraTransformUpdate = Time.time;
            }
        }

        void Update()
        {
			if (eyeTrackingService == null || !eyeTrackingService.IsEnabled || !eyeTrackingService.IsCalibrated)
            {
                return;
            }

            eyeTrackingService.Update();
            DetectCameraMovememnt();

			if (newGazeInfo && eyeTrackingService.Settings.RecordGazeCoordinatesEnabled)
			{
				LogGazeInfo(GazeInfo);
			}

            if (newGazeInfo || cameraMoved)
            {
                UpdateAttentionTarget();
            }
        }

		public void LogGazeInfo(GazeEventArgs gazeEventArgs)
		{
			List<object> keyValues = new List<object>();
			
			keyValues.Add("LeftPupilDiameter");
			keyValues.Add(gazeEventArgs.LeftPupilDiameter);
			
			keyValues.Add("LeftX");
			keyValues.Add(gazeEventArgs.LeftX);
			
			keyValues.Add("LeftY");
			keyValues.Add(gazeEventArgs.LeftY);
			
			keyValues.Add("RightPupilDiameter");
			keyValues.Add(gazeEventArgs.RightPupilDiameter);
			
			keyValues.Add("RightX");
			keyValues.Add(gazeEventArgs.RightX);
			
			keyValues.Add("RightY");
			keyValues.Add(gazeEventArgs.RightY);
			
			TraceLog.Write("System", TraceLog.Action.Sampled, "EyeTrackingData", keyValues);
		}

        // This class is not View-derived since it is recording view information
        // and its debug visualizations should not be recorded.
        void OnGUI()
        {
			if (GazeInfo == null || !eyeTrackingService.IsReceivingGazeData ||
                (!eyeTrackingService.Settings.DisplayGazeTargetCursorEnabled 
                && !eyeTrackingService.Settings.DisplayAttentionTargetNameEnabled))
            {
                // Nothing to display
                return;
            }

            GUI.depth = guiDepth;
            //GUI.matrix = AspectUtility.Current.Matrix;

            // Convert from screen space (0,0) at bottom-left to GUI space (0,0) at the top-left
            positionIndicatorRect.center = GUIUtility.ScreenToGUIPoint(new Vector2(GazeInfo.LeftX, Screen.height - GazeInfo.LeftY));

            if (eyeTrackingService.Settings.DisplayGazeTargetCursorEnabled && GazeInfo != null && positionIndicator != null)
            {
                GUI.DrawTexture(positionIndicatorRect, positionIndicator);
            }

            if (eyeTrackingService.Settings.DisplayAttentionTargetNameEnabled && AttentionTarget.ThresholdMet)
            {
				float y = positionIndicatorRect.y + positionIndicatorRect.height;
				GUI.Label(new Rect(positionIndicatorRect.x, y, 256, 64), 
					string.Format("{0}, {1}", positionIndicatorRect.center.x, positionIndicatorRect.center.y), textStyle);
				
				if (!AttentionTarget.IsNull) 
				{
					y += textStyle.fontSize;
					GUI.Label(new Rect(positionIndicatorRect.x, y, 256, 64), AttentionTarget.Name, textStyle);
				}				
            }
        }              
    }
}