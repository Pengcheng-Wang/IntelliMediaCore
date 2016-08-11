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
using IntelliMedia.Utilities;
using IntelliMedia.EyeTracking;


#endregion

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using EyeTrackingController;
using System.Text;
using IntelliMedia.Models;
using IntelliMedia.Repositories;

namespace IntelliMedia.Services
{
    public class EyeTrackingService
    {
		public EyeTrackerSettings Settings { get; private set; }
		public bool IsCalibrated { get; private set; }
		public event EventHandler<GazeEventArgs> GazeChanged;

		private EyeTrackerSettingsRepository repository;
		public EyeTracker EyeTracker { get; private set; }

		private EyeTrackingService(EyeTrackerSettingsRepository repository)
        {
			this.repository = repository;
        }

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
				return (EyeTracker != null ? EyeTracker.IsConnected && EyeTracker.IsCalibrated : false);
			}
		}

		public void Initialize(ShowMessageHandler showMessage)
        {
            DebugLog.Info("EyeTrackingService Initialize");
			if (EyeTracker != null) 
			{
				Shutdown();
			}

			repository.Get().Start((settings) =>
			{
				Settings = (EyeTrackerSettings)settings;
			},
			(error) =>
			{
				showMessage(
					"Unable to initialize eye tracker", 
					error.Message);				
			});
        }

        void HandleGazeChanged(object sender, GazeEventArgs e)
        {
			if (GazeChanged != null) 
			{
				GazeChanged(this, e);
			}
        }

        public void Update()
        {
            if (EyeTracker != null) 
            {
                EyeTracker.Update();
            }
        }	

        public void Shutdown()
        {
            DebugLog.Info("EyeTrackingService Shutdown");
			if (EyeTracker != null) 
			{
				TraceLog.Player(TraceLog.Action.DisconnectedFrom, "EyeTracker",
				                "EyeTrackerSystem", EyeTracker.GetType().Name);

				EyeTracker.GazeChanged -= HandleGazeChanged;
				EyeTracker.Disconnect();
				EyeTracker = null;
				IsCalibrated = false;
			}
        }

		public delegate void ButtonHandler(int index); 
		public delegate void ShowMessageHandler(string title, string message, string[] buttons = null, ButtonHandler buttonHandler = null);

		public void Connect()
		{
			switch (Settings.System) 
			{
			case EyeTrackerSettings.EyeTrackerSystem.None:
				throw new Exception("Attempting to initialize EyeTrackingService even though eye tracking is not enabled.");

			case EyeTrackerSettings.EyeTrackerSystem.Simulated:
				SimulatedEyeTracker simulated = new SimulatedEyeTracker();
				simulated.SimulatedEyeTrackingFrequency = Settings.SimulatedEyeTrackingFrequency;
				EyeTracker = simulated;
				break;

			case EyeTrackerSettings.EyeTrackerSystem.SMI:
				SmiEyeTracker smi = new SmiEyeTracker();
				smi.ServerRecvAddress = Settings.ServerRecvAddress;
				smi.ServerRecvPort = Settings.ServerRecvPort;
				smi.ServerSendAddress = Settings.ServerSendAddress;
				smi.ServerSendPort = Settings.ServerSendPort;
				smi.CalibrationPoints = Settings.CalibrationPoints;
				EyeTracker = smi;
				break; 

			case EyeTrackerSettings.EyeTrackerSystem.EyeTribe:
				EyeTribeEyeTracker eyeTribe = new EyeTribeEyeTracker();
				eyeTribe.ServerRecvAddress = Settings.ServerRecvAddress;
				eyeTribe.ServerRecvPort = Settings.ServerRecvPort;
				eyeTribe.CalibrationPointSampleDuration = Settings.CalibrationPointSampleDuration;
				eyeTribe.CalibrationPoints = Settings.CalibrationPoints;
				EyeTracker = eyeTribe;
				break; 

			default:
				throw new Exception("Unsupported eye tracking system: " + Settings.System.ToString());
			}


			EyeTracker.GazeChanged += HandleGazeChanged;
			EyeTracker.Connect();

			TraceLog.Player(TraceLog.Action.ConnectedTo, "EyeTracker",
				"EyeTrackerSystem", EyeTracker.GetType().Name);			
		}

		public void Calibrate(Action launchAction, ShowMessageHandler showMessage)  
		{
			Contract.ArgumentNotNull("launchAction", launchAction);
			Contract.ArgumentNotNull("showMessage", showMessage);

			IsCalibrated = false;

			try
			{
				Connect();
				if (EyeTracker.IsCalibrationRequired) 
				{
					showMessage("Begin Eye Tracker Calibration", 
								"The next step is to calibrate the eye tracking. After pressing OK, a screen will open with a dot in the center. Look at the dot and press the spacebar when ready. The dot will move around the screen. Follow the dot with your eyes.", 
								null,
								(int index) =>
					{
						try 
						{
							EyeTracker.Calibrate((bool success, string message, Dictionary<string, object> calibrationPropertyResults) =>
							{
								TraceLog.Player(TraceLog.Action.Calibrated, "EyeTracker",
								                "EyeTrackerSystem", EyeTracker.GetType().Name,
								                "Success", success,
								                "Message", message,
								                calibrationPropertyResults);

								if (success)
								{
									IsCalibrated = true;

									// Calibrate inform the user and continue launch
									showMessage(
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
											launchAction();
											break;

										case 1:
											Calibrate(launchAction, showMessage);
											break;

										default:
											// Do nothing
											DebugLog.Info("Start cancelled by user.");
											break;
										}
									});
								}
								else
								{
									showMessage(
										"Calibration Failed", 
										message);
								}
							});
						} 
	                    catch (Exception e) 
	                    {
							showMessage(
								"Calibration Failed", 
								e.Message);
						}
					});
				} 
				else
				{
					// No need to calibrate the eye tracker, go ahead with the launch
					launchAction();
				}
			}
			catch (Exception e) 
			{
				showMessage(
					"Unable to connect to eye tracker", 
					e.Message);
			}
			
        }
    }
}
