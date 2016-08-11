//---------------------------------------------------------------------------------------
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IntelliMedia.Models;
using IntelliMedia.ViewModels;
using IntelliMedia.Views;
using IntelliMedia.Utilities;

namespace IntelliMedia.Services
{
	public class ActivityLauncher
	{
		private StageManager stageManager;
		private SessionState sessionState;
		private ActivityMapping activityMapping;
		private ActivityService activityService;
		private IEyeTrackingService eyeTrackingService;

		public ActivityLauncher(
			StageManager stageManager,
			SessionState sessionState,
			ActivityMapping activityMapping,
			ActivityService activityService,
			[Zenject.InjectOptional] IEyeTrackingService eyeTrackingService)
		{
			this.stageManager = stageManager;
			this.sessionState = sessionState;
			this.activityMapping = activityMapping;
			this.activityService = activityService;
			this.eyeTrackingService = eyeTrackingService;
		}
		
		public IAsyncTask Start(Student student, Activity activity, bool resetActivityState = false)
		{
			DebugLog.Info("ActivityLauncher: Start  '{0}' ({1})", activity.Name, activity.Uri);

			ActivityMapping.ActivityInfo activityInfo = null;
			ActivityState currentState = null;
			ActivityViewModel activityViewModel = null;

			return new AsyncTry(activityService.LoadActivityState(student.Id, activity.Id, true))
				.Then<ActivityState>((activityState) =>
				{
					if (resetActivityState || !activityState.CanResume)
					{
						// Generate a new trace ID for restarts or new games that don't have saved state
						activityState.TraceId = Guid.NewGuid().ToString();
					}			

					activityState.RecordLaunch();
					activityState.ModifiedDate = DateTime.Now;
					currentState = activityState;

					activityInfo = activityMapping.FindViewModelByUrn(activity.Uri);
					if (activityInfo == null || string.IsNullOrEmpty(activityInfo.viewModel))
					{
						throw new Exception(String.Format("Could not find urn in ActivityMapping for '{0}'", activity.Uri));
					}

					Type viewModelType = TypeFinder.ClassNameToType(activityInfo.viewModel);
					if (viewModelType == null)
					{
						throw new Exception(String.Format("Unable to find class with type name '{0}'", activity.Uri));
					}

					return stageManager.Resolve(viewModelType);
				})
				.Then<ActivityViewModel>((vm) =>
				{
					activityViewModel = vm;

					if (eyeTrackingService != null
						&& sessionState.CourseSettings.EyeTrackingEnabled 
						&& eyeTrackingService.IsEnabled
						// TODO rgtaylor 2016-04-05 Use some other approach for disabling eye tracking for web view activities
						&& !activity.Uri.Contains("assessment")
						&& !activity.Uri.Contains("video"))
					{
						return eyeTrackingService.Calibrate();
					}
					else
					{
						return true;
					}									
				})
				.Then<bool>((calibrateSuccess) =>
				{
					if (!calibrateSuccess)
					{
						throw new Exception("Eye tracking calibration was not successful. Activity cannot be launched.");
					}

					activityViewModel.Activity = activity;
					activityViewModel.ActivityState = currentState;
					activityViewModel.ViewPreferences = activityInfo.viewCapabilities;

					return activityViewModel;
				});
		}
	}
}
