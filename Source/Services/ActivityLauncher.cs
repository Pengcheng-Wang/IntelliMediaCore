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
		private ActivityService activityService;
		private ViewModel.Factory viewModelFactory;
		private Dictionary<string, Type> urnToViewModelType = new Dictionary<string, Type>();
		private LogEntry startedEntry;

		public ActivityLauncher(
			ActivityService activityService, 
			ViewModel.Factory viewModelFactory)
		{
			this.activityService = activityService;
			this.viewModelFactory = viewModelFactory;
		}
		
		public AsyncTask Start(Student student, Activity activity, bool resetActivityState = false)
		{
			return activityService.LoadActivityState(student.Id, activity.Id, true)
				.Then((prevResult, onCompleted, onError) =>
				{
					ActivityState activityState = prevResult.ResultAs<ActivityState>();
					if (resetActivityState || !activityState.CanResume)
					{
						// Generate a new trace ID for restarts or new games that don't have saved state
						activityState.TraceId = Guid.NewGuid().ToString();
					}			

					activityState.RecordLaunch();
					activityState.ModifiedDate = DateTime.Now;

					onCompleted(viewModelFactory.Resolve<ActivityViewModel>(Resolve(activity.Uri), vm =>
					{
						vm.Activity = activity;
						vm.ActivityState = activityState;
					}));

				});
		}
			
		public Type Resolve(string activityUrn)
		{
			Contract.ArgumentNotNull("activityUrn", activityUrn);

			foreach(string registeredUrnPattern in urnToViewModelType.Keys)
			{
				Regex regex = new Regex(registeredUrnPattern);
				if (regex.IsMatch(activityUrn))
				{
					return urnToViewModelType[registeredUrnPattern];
				}
			}

			throw new Exception(String.Format("Activity URN not registered for '{0}'", activityUrn));
		}
		
		public void Register(string urn, Type viewModel)
		{
			urnToViewModelType[urn] = viewModel;
		}
	}
}
