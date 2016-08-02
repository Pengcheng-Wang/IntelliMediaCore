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

namespace IntelliMedia
{
	public class ActivityLauncher
	{
		private StageManager stageManager;
		private ActivityMapping activityMapping;
		private ActivityService activityService;
		private ActivityViewModel activityViewModel;
		private LogEntry startedEntry;

		public ActivityLauncher(
			StageManager stageManager,
			ActivityMapping activityMapping,
			ActivityService activityService)
		{
			this.stageManager = stageManager;
			this.activityMapping = activityMapping;
			this.activityService = activityService;
		}
		
		public IAsyncTask Start(Student student, Activity activity, bool resetActivityState = false)
		{
			DebugLog.Info("ActivityLauncher: Start  '{0}' ({1})", activity.Name, activity.Uri);

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

					ActivityViewModel vm = Resolve(activity.Uri);
					vm.Activity = activity;
					vm.ActivityState = activityState;

					return vm;
				});
		}
			
		public ActivityViewModel Resolve(string activityUrn)
		{
			Contract.ArgumentNotNull("activityUrn", activityUrn);

			string viewModelTypeName = activityMapping.FindViewModelByUrn(activityUrn);
			if (string.IsNullOrEmpty(viewModelTypeName))
			{
				throw new Exception(String.Format("Activity URN not defined in ActivityMapping for '{0}'", activityUrn));
			}

			Type viewModelType = TypeFinder.ClassNameToType(viewModelTypeName);
			if (viewModelType == null)
			{
				throw new Exception(String.Format("Unable to find class with type name '{0}'", viewModelTypeName));
			}

			ActivityViewModel viewModel = stageManager.ResolveViewModel(viewModelType) as ActivityViewModel;
			if (viewModel == null)
			{
				throw new Exception(String.Format("Unable to resolve '{0}'. Confirm that TheatreInstaller has created binding.", viewModelTypeName));
			}

			return viewModel;
		}
	}
}
