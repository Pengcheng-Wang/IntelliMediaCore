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
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using EyeTrackingController;
using System.Text;
using IntelliMedia.Models;
using IntelliMedia.Repositories;
using IntelliMedia.Utilities;
using IntelliMedia.EyeTracking;
using IntelliMedia.ViewModels;
using Zenject;

namespace IntelliMedia.Services
{
	public class EyeTrackingService : IEyeTrackingService
    {
		public EyeTrackerSettings Settings { get; private set; }
		public bool IsCalibrated { get; private set; }
		public event EventHandler<GazeEventArgs> GazeChanged;

		private StageManager stageManager;
		private EyeTrackerSettingsRepository repository;
		private EyeTracker eyeTracker;

		public EyeTrackingService(StageManager stageManager, EyeTrackerSettingsRepository repository)
        {
			this.stageManager = stageManager;
			this.repository = repository;
        }

		#region IInitializable implementation

		public void Initialize ()
		{
			Dispose();

			DebugLog.Info("EyeTrackingService Initialize");
			repository.Get().Start((settings) =>
			{
				Settings = (EyeTrackerSettings)settings;
			},
			(error) =>
			{
				ShowMessage("Unable to initialize eye tracker", error.Message);
			});
		}

		#endregion

		#region IDisposable implementation

		public void Dispose()
		{			
			if (eyeTracker != null) 
			{
				DebugLog.Info("EyeTrackingService Shutdown");

				TraceLog.Player(TraceLog.Action.DisconnectedFrom, "EyeTracker",
					"EyeTrackerSystem", eyeTracker.GetType().Name);

				eyeTracker.GazeChanged -= HandleGazeChanged;
				eyeTracker.Disconnect();
				eyeTracker = null;
				IsCalibrated = false;
			}			
		}

		#endregion

        public bool IsEnabled
        {
            get
            {
                return Settings != null 
                    && Settings.System != EyeTrackerSettings.EyeTrackerSystem.None;
            }
        }

		public bool IsReceivingGazeData
		{
			get
			{
				return (eyeTracker != null ? eyeTracker.IsConnected && eyeTracker.IsCalibrated : false);
			}
		}

        private void HandleGazeChanged(object sender, GazeEventArgs e)
        {
			if (GazeChanged != null) 
			{
				GazeChanged(this, e);
			}
        }

        public void Update()
        {
            if (eyeTracker != null) 
            {
                eyeTracker.Update();
            }
        }	
			
		private void ShowMessage(string title, string message, string[] buttons = null, Action<int> buttonHandler = null)
		{
			stageManager.Reveal<Alert>(alert =>
			{
				alert.Title = title;
				alert.Message = message;
				if (buttons != null)
				{
					alert.ButtonLabels = buttons;
				}
				if (buttonHandler != null)
				{
					alert.AlertDismissed += ((int index) => buttonHandler(index));
				}
			}).Start();			
		}

		public void Connect()
		{
			switch (Settings.System) 
			{
			case EyeTrackerSettings.EyeTrackerSystem.None:
				throw new Exception("Attempting to initialize EyeTrackingService even though eye tracking is not enabled.");

			case EyeTrackerSettings.EyeTrackerSystem.Simulated:
				SimulatedEyeTracker simulated = new SimulatedEyeTracker();
				simulated.SimulatedEyeTrackingFrequency = Settings.SimulatedEyeTrackingFrequency;
				eyeTracker = simulated;
				break;

			case EyeTrackerSettings.EyeTrackerSystem.SMI:
				SmiEyeTracker smi = new SmiEyeTracker();
				smi.ServerRecvAddress = Settings.ServerRecvAddress;
				smi.ServerRecvPort = Settings.ServerRecvPort;
				smi.ServerSendAddress = Settings.ServerSendAddress;
				smi.ServerSendPort = Settings.ServerSendPort;
				smi.CalibrationPoints = Settings.CalibrationPoints;
				eyeTracker = smi;
				break; 

			case EyeTrackerSettings.EyeTrackerSystem.EyeTribe:
				EyeTribeEyeTracker eyeTribe = new EyeTribeEyeTracker();
				eyeTribe.ServerRecvAddress = Settings.ServerRecvAddress;
				eyeTribe.ServerRecvPort = Settings.ServerRecvPort;
				eyeTribe.CalibrationPointSampleDuration = Settings.CalibrationPointSampleDuration;
				eyeTribe.CalibrationPoints = Settings.CalibrationPoints;
				eyeTracker = eyeTribe;
				break; 

			default:
				throw new Exception("Unsupported eye tracking system: " + Settings.System.ToString());
			}


			eyeTracker.GazeChanged += HandleGazeChanged;
			eyeTracker.Connect();

			TraceLog.Player(TraceLog.Action.ConnectedTo, "EyeTracker",
				"EyeTrackerSystem", eyeTracker.GetType().Name);			
		}

		public IAsyncTask Calibrate()  
		{
			IsCalibrated = false;

			return new AsyncTask((onCompleted, onError) => 
			{
				try
				{
					Connect();
					if (eyeTracker.IsCalibrationRequired) 
					{
						ShowMessage("Begin Eye Tracker Calibration", 
									"The next step is to calibrate the eye tracking. After pressing OK, a screen will open with a dot in the center. Look at the dot and press the spacebar when ready. The dot will move around the screen. Follow the dot with your eyes.", 
									null,
									(int index) =>
						{
							try 
							{
								eyeTracker.Calibrate((bool success, string message, Dictionary<string, object> calibrationPropertyResults) =>
								{
									TraceLog.Player(TraceLog.Action.Calibrated, "EyeTracker",
									                "EyeTrackerSystem", eyeTracker.GetType().Name,
									                "Success", success,
									                "Message", message,
									                calibrationPropertyResults);

									if (success)
									{
										IsCalibrated = true;

										// Calibrate inform the user and continue launch
										ShowMessage(
											"Calibration Completed", 
											message, 
											new string[]
											{
												"Start",
												"Recalibrate",
												"Cancel"
											},
											(int option) =>
										{ 
											switch(option)
											{
											case 0:
												onCompleted(true);
												break;

											case 1:
												Calibrate().Start(onCompleted, onError);
												break;

											default:
												// Do nothing
												DebugLog.Info("Start cancelled by user.");
												onCompleted(false);
												break;
											}
										});
									}
									else
									{
										onError(new Exception(string.Format("Calibration Failed. {0}", message)));
									}
								});
							} 
		                    catch (Exception e) 
		                    {
								onError(new Exception(string.Format("Calibration Failed. {0}", e.Message)));
							}
						});
					} 
					else
					{
						// No need to calibrate the eye tracker, go ahead with the launch
						onCompleted(true);
					}
				}
				catch (Exception e) 
				{
					onError(new Exception(string.Format("Unable to connect to eye tracker. {0}", e.Message)));
				}
			});	
        }
    }
}
