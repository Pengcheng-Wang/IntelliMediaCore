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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace IntelliMedia
{
	public class ActivityService 
	{
		private AppSettings appSettings;

		public ActivityService(AppSettings appSettings)
		{
			this.appSettings = appSettings;
		}
		
		public IAsyncTask LoadActivities(string courseId)
		{
			Uri serverUri = new Uri(appSettings.ServerURI, UriKind.RelativeOrAbsolute);
			Uri restUri = new Uri(serverUri, "rest/");

			ActivityRepository repo = new ActivityRepository(restUri);
			if (repo == null)
			{
				throw new Exception("ActivityRepository is not initialized.");
			}
						
			return repo.GetActivities(courseId);
		}

		public IAsyncTask LoadActivityStates(string studentId, IEnumerable<string> activityIds)
		{
			Uri serverUri = new Uri(appSettings.ServerURI, UriKind.RelativeOrAbsolute);
			Uri restUri = new Uri(serverUri, "rest/");
			
			ActivityStateRepository repo = new ActivityStateRepository(restUri);
			if (repo == null)
			{
				throw new Exception("ActivityStateRepository is not initialized.");
			}
			
			return repo.GetActivityStates(studentId, activityIds);
		}

		public IAsyncTask LoadActivityState(string studentId, string activityId, bool createNewIfDoesNotExit = false)
		{
			Uri serverUri = new Uri(appSettings.ServerURI, UriKind.RelativeOrAbsolute);
			Uri restUri = new Uri(serverUri, "rest/");
			
			ActivityStateRepository repo = new ActivityStateRepository(restUri);
			if (repo == null)
			{
				throw new Exception("ActivityStateRepository is not initialized.");
			}

			// Load ActivityState (if it exists)
			return new AsyncTry(repo.GetActivityStates(studentId, new string[] { activityId }))
				.Then<List<ActivityState>>((states) =>
				{
					if (states.Count > 0)
					{
						return states.OrderByDescending(s => s.ModifiedDate).First();							
					}
					else
					{
						// Create ActivityState
						ActivityState newState = new ActivityState()
						{
							ActivityId = activityId,
							StudentId = studentId
						};
						return repo.Insert(newState);
					}
				});                   
		}

		public IAsyncTask SaveActivityState(ActivityState activityState)
		{
			Contract.ArgumentNotNull("activityState", activityState);

			Uri serverUri = new Uri(appSettings.ServerURI, UriKind.RelativeOrAbsolute);
			Uri restUri = new Uri(serverUri, "rest/");
			
			ActivityStateRepository repo = new ActivityStateRepository(restUri);
			if (repo == null)
			{
				throw new Exception("ActivityStateRepository is not initialized.");
			}

			return repo.Update(activityState);                   
		}
	}
}
