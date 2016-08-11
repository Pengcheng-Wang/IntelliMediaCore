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
using System.Collections.Generic;

namespace IntelliMedia.ViewModels
{
	public class PauseMenu : ViewModel
	{
//		private StageManager navigator;
//		private SessionState sessionState;
//		private AuthenticationService authenticator;
//		private SessionService sessionService;
//		private ActivityService activityService;
//		private ActivityLauncher activityLauncher;
//		private EyeTrackingService eyeTrackingService;
//
//		public readonly BindableProperty<string> UsernameProperty = new BindableProperty<string>();	
//		public string Username 
//		{ 
//			get { return UsernameProperty.Value; }
//			set { UsernameProperty.Value = value; }
//		}
//
//		public readonly BindableProperty<List<Activity>> ActivitiesProperty = new BindableProperty<List<Activity>>();	
//		public List<Activity> Activities 
//		{ 
//			get { return ActivitiesProperty.Value; }
//			set { ActivitiesProperty.Value = value; }
//		}
//
//		public readonly BindableProperty<List<ActivityState>> ActivityStatesProperty = new BindableProperty<List<ActivityState>>();	
//		public List<ActivityState> ActivityStates 
//		{ 
//			get { return ActivityStatesProperty.Value; }
//			set { ActivityStatesProperty.Value = value; }
//		}
//
//		public PauseMenuViewModel(
//			StageManager navigator, 
//			SessionState sessionState,
//			AuthenticationService authenticator, 
//			SessionService sessionService,
//			ActivityService activityService,
//			ActivityLauncher activityLauncher,
//			[Zenject.InjectOptional] EyeTrackingService eyeTrackingService)
//		{
//			this.navigator = navigator;
//			this.sessionState = sessionState;
//			this.authenticator = authenticator;
//			this.sessionService = sessionService;
//			this.activityService = activityService;
//			this.activityLauncher = activityLauncher;
//			this.eyeTrackingService = eyeTrackingService;
//		}
//
//
//		public void SignOut()
//		{
//			DebugLog.Info("SignOut...");
//			ProgressIndicatorViewModel.ProgressInfo busyIndicator = null;
//			new AsyncTry(navigator.Reveal<ProgressIndicatorViewModel>())
//				.Then<ProgressIndicatorViewModel>((progressIndicatorViewModel) =>
//				{
//					busyIndicator = progressIndicatorViewModel.Begin("Signing out...");
//					return sessionService.EndSession();
//				})
//				.Then<bool>((success) =>
//				{
//					DebugLog.Info("Session ended");
//					return authenticator.SignOut();
//				})
//				.Then<bool>((success) =>
//				{
//					DebugLog.Info("Signed out");
//					navigator.Transition(this, typeof(SignInViewModel));
//				})
//				.Catch((Exception e) =>
//				{
//					navigator.Reveal<AlertViewModel>(alert =>
//					{
//						alert.Title = "Unable to sign out";
//						alert.Message = e.Message;
//						alert.Error = e;
//						alert.AlertDismissed += ((int index) => DebugLog.Info("Button {0} pressed", index));
//					}).Start();
//				})
//				.Finally(() =>
//				{
//					if (busyIndicator != null)
//					{
//						busyIndicator.Dispose();
//					}
//				}).Start();
//		}
//
//		public void Settings()
//		{            
//			navigator.Transition(this, typeof(SettingsViewModel));
//		}
//
//		public void Resume()
//		{ 
//			navigator.Transition(this, typeof(HudViewModel));
//		}
	}
}
