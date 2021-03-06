﻿//---------------------------------------------------------------------------------------
// Copyright 2014 North Carolina State University
//
// Center for Educational Informatics
// http://www.cei.ncsu.edu/
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
//   * Redistributions of source code must retain the above copyright notice, this 
//     list of conditions and the following disclaimer.
//   * Redistributions in binary form must reproduce the above copyright notice, 
//     this list of conditions and the following disclaimer in the documentation 
//     and/or other materials provided with the distribution.
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
using System.Collections.Generic;
using System;
using IntelliMedia.Models;
using IntelliMedia.Services;
using IntelliMedia.Utilities;

namespace IntelliMedia.ViewModels
{
	public abstract class ActivityViewModel : ViewModel
	{
		protected StageManager navigator;
		protected ActivityService activityService;

		public Activity Activity { get; set; }
		public ActivityState ActivityState { get; set; }
		private LogEntry startedEntry;
		
		public ActivityViewModel(StageManager navigator, ActivityService activityService)
		{
			Contract.ArgumentNotNull("navigator", navigator);
			Contract.ArgumentNotNull("activityService", activityService);
			
			this.navigator = navigator;
			this.activityService = activityService;
		}

		public override void OnStartReveal()
		{			
			if (Activity == null)
			{
				throw new Exception("Activity not loaded.");
			}				

			DebugLog.Info("Revealed activity:" + Activity.Name);
			startedEntry = TraceLog.Player(TraceLog.Action.Started, "Activity",
			                               "Name", Activity.Name,
			                               "ActivityUri", Activity.Uri,
										   "IsComplete", ActivityState.IsComplete,
										   "CanResume", ActivityState.CanResume,
										   "StartDate", ActivityState.ModifiedDate.ToIso8601(),
										   "TraceId", ActivityState.TraceId);


			base.OnStartReveal();
		}

		public override void OnFinishHide()
		{
			base.OnFinishHide();

			if (startedEntry != null)
			{
				TraceLog.Player(startedEntry, TraceLog.Action.Ended, "Activity",
					"IsComplete", ActivityState.IsComplete,
					"CanResume", ActivityState.CanResume);
				startedEntry = null;
			}
		}
		
		protected TDataModel DeserializeActivityData<TDataModel>() where TDataModel : class, new()
		{
			DebugLog.Info("ActivityViewModel.DeserializeActivityData");

			if (ActivityState.GameData != null)
			{
				return SerializerXml.Instance.Deserialize<TDataModel>(ActivityState.GameData);
			}
			else
			{
				return new TDataModel();
			}
		}

		protected void SerializeActivityData<TDataModel>(TDataModel data) where TDataModel : class, new()
		{
			if (data != null)
			{
				ActivityState.GameData = SerializerXml.Instance.Serialize<TDataModel>(data);
			}
			else
			{
				ActivityState.GameData = null;
			}
		}
		
		protected void SaveActivityStateAndTransition<ToViewModel>()
		{												
			DebugLog.Info("Save state");
			ProgressIndicator.ProgressInfo busyIndicator = null;
			new AsyncTry(navigator.Reveal<ProgressIndicator>())
				.Then<ProgressIndicator>((progressIndicatorViewModel) =>
				{
	                busyIndicator = progressIndicatorViewModel.Begin("Saving...");
					return activityService.SaveActivityState(ActivityState);
				})
				.Then<ActivityState>((activityState) => navigator.Transition(this, typeof(ToViewModel)))
                .Catch((Exception e) =>
                {
                   navigator.Reveal<Alert>(alert =>
                    {
                        alert.Title = "Unable to save";
                        alert.Message = e.Message;
                        alert.Error = e;
                        alert.AlertDismissed += ((int index) => DebugLog.Info("Button {0} pressed", index));
					}).Start();

                }).Finally(() =>
                {
					if (busyIndicator != null)
					{
                   		busyIndicator.Dispose();
					}
				}).Start();
		}
	}
}
