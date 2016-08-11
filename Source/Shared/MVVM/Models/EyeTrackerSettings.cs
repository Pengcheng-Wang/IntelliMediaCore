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
#endregion

using System.Collections.Generic;

namespace IntelliMedia
{
    public class EyeTrackerSettings
    {
		public enum EyeTrackerSystem
		{
			None,
			Simulated, // Allows caller to set the gaze position.
			SMI,      // http://www.smivision.com/en/gaze-and-eye-tracking-systems/products/red-red250-red-500.html
			EyeTribe, // https://theeyetribe.com/
			// TODO rgtaylor 2015-03-18 Add support for other eye tracking HW
			//Gazepoint // http://www.gazept.com/
		}

        [RepositoryKey]
        public string Id { get; set; }

		public EyeTrackerSystem System { get; set; }
        public bool DisplayAttentionTargetNameEnabled { get; set; }
        public bool DisplayGazeTargetCursorEnabled { get; set; }
		public bool RecordGazeCoordinatesEnabled { get; set; }

        public float SimulatedEyeTrackingFrequency { get; set; }

        public float AttentionThreshold { get; set; }

        public float CameraMovementDetectionFrequency { get; set; }

        public int CalibrationPoints { get; set; }

		public float CalibrationPointSampleDuration { get; set; }

        public string ServerRecvAddress { get; set; }
        public int ServerRecvPort { get; set; }

        public string ServerSendAddress { get; set; }
        public int ServerSendPort { get; set; }

		public EyeTrackerSettings(EyeTrackerSystem system)
		{
			System = system;
			ResetToDefaults();
		}

		public EyeTrackerSettings() : this(EyeTrackerSystem.None)
        {
        }

		public void ResetToDefaults()
		{
			AttentionThreshold = 0.250f; // 250 ms
			CameraMovementDetectionFrequency = 30;
			SimulatedEyeTrackingFrequency = 30;
			
			CalibrationPoints = 9;

			CalibrationPointSampleDuration = 2;

			ServerRecvAddress = "127.0.0.1";
			ServerRecvPort = -1;
			ServerSendAddress = "127.0.0.1";
			ServerSendPort = -1;

			switch(System)
			{
			case EyeTrackerSystem.SMI:
				ServerRecvPort = 5555;
				ServerSendPort = 4444;
				break;

			case EyeTrackerSystem.EyeTribe:
				ServerRecvPort = 6555;
				break;
			}
		}
    }
}

