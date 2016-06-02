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

namespace IntelliMedia
{
	public class MainMenuViewModel : ViewModel
	{
		private StageManager navigator;
		private SessionState sessionState;
		private AuthenticationService authenticator;
		private SessionService sessionService;
		private ActivityService activityService;
		private ActivityLauncher activityLauncher;
		private EyeTrackingService eyeTrackingService;

		public readonly BindableProperty<string> UsernameProperty = new BindableProperty<string>();	
		public string Username 
		{ 
			get { return UsernameProperty.Value; }
			set { UsernameProperty.Value = value; }
		}

		public readonly BindableProperty<List<Activity>> ActivitiesProperty = new BindableProperty<List<Activity>>();	
		public List<Activity> Activities 
		{ 
			get { return ActivitiesProperty.Value; }
			set { ActivitiesProperty.Value = value; }
		}

		public readonly BindableProperty<List<ActivityState>> ActivityStatesProperty = new BindableProperty<List<ActivityState>>();	
		public List<ActivityState> ActivityStates 
		{ 
			get { return ActivityStatesProperty.Value; }
			set { ActivityStatesProperty.Value = value; }
		}
		
		public MainMenuViewModel(
			StageManager navigator, 
			SessionState sessionState,
			AuthenticationService authenticator, 
			SessionService sessionService,
			ActivityService activityService,
			ActivityLauncher activityLauncher,
			[Zenject.InjectOptional] EyeTrackingService eyeTrackingService)
		{
			this.navigator = navigator;
			this.sessionState = sessionState;
			this.authenticator = authenticator;
			this.sessionService = sessionService;
			this.activityService = activityService;
			this.activityLauncher = activityLauncher;
			this.eyeTrackingService = eyeTrackingService;
		}

		public override void OnStartReveal()
		{
			base.OnStartReveal();

			RefreshActivityList();
			if (eyeTrackingService != null)
			{
				eyeTrackingService.Initialize(ShowMessageHandler);
			}
		}

		private void RefreshActivityList()
		{
			try
			{
				Username = sessionState.Student.Username;

				Contract.PropertyNotNull("sessionState.CourseSettings", sessionState.CourseSettings);
				
				DebugLog.Info("RefreshActivityList");
				navigator.Reveal<ProgressIndicatorViewModel>().Then((vm, onRevealed, onRevealError) =>
				{
					ProgressIndicatorViewModel progressIndicatorViewModel = vm.ResultAs<ProgressIndicatorViewModel>();
                    ProgressIndicatorViewModel.ProgressInfo busyIndicator = progressIndicatorViewModel.Begin("Loading...");
                    activityService.LoadActivities(sessionState.CourseSettings.CourseId)
					.Then((prevResult, onCompleted, onError) =>
                    {
						DebugLog.Info("Activities loaded");	
						Activities = prevResult.ResultAs<List<Activity>>();
						for (int index = 0; index < Activities.Count; ++index)
						{
							DebugLog.Info("[{0}] {1}", index, Activities[index].Name);
						}
						IEnumerable<string> activityIds = Activities.Select(a => a.Id);
						activityService.LoadActivityStates(sessionState.Student.Id, activityIds).Start(onCompleted, onError);
                    })
					.Then((prevResult, onCompleted, onError) =>
                    {
						DebugLog.Info("Activity States loaded");	
						ActivityStates = prevResult.ResultAs<List<ActivityState>>();
						onCompleted(true);
                    })
                    .Catch((Exception e) =>
                    {
						DebugLog.Error("Can't load activitues: {0}", e.Message);	
                        navigator.Reveal<AlertViewModel>(alert =>
                        {
                             alert.Title = "Unable to load activity information.";
                             alert.Message = e.Message;
                             alert.Error = e;
                             alert.AlertDismissed += ((int index) => DebugLog.Info("Button {0} pressed", index));
						}).Start();

                    }).Finally(() =>
                    {
                        busyIndicator.Dispose();
					}).Start();

					onRevealed(true);
				}).Start();
			}
			catch (Exception e)
			{
				navigator.Reveal<AlertViewModel>(alert => 
				{
					alert.Title = "Unable load activity information";
					alert.Message = e.Message;
					alert.Error = e;
				}).Start();
			}
		}

		public void StartActivity(Activity activity)
		{
			if (eyeTrackingService != null
				&& sessionState.CourseSettings.EyeTrackingEnabled 
				&& eyeTrackingService.IsEnabled
				// TODO rgtaylor 2016-04-05 Use some other approach for disabling eye tracking for web view activities
				&& !activity.Uri.Contains("assessment")
				&& !activity.Uri.Contains("video"))
			{
				eyeTrackingService.Calibrate(
					() => 
					{ 
						TransitionToActivity(activity); 
					},
					ShowMessageHandler);
			}
			else
			{
				TransitionToActivity(activity);
			}			
		}

		public void ShowMessageHandler(string title, string message, string[] buttons = null, EyeTrackingService.ButtonHandler buttonHandler = null)
		{
			navigator.Reveal<AlertViewModel>(alert =>
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

		private void TransitionToActivity(Activity activity)
		{
			try
			{
				Contract.ArgumentNotNull("activity", activity);
				
				DebugLog.Info("Started Activity {0}", activity.Name);

				navigator.Reveal<ProgressIndicatorViewModel>().Then((vm, onRevealed, onRevealError) =>
				{
					ProgressIndicatorViewModel progressIndicatorViewModel = vm.ResultAs<ProgressIndicatorViewModel>();
                    ProgressIndicatorViewModel.ProgressInfo busyIndicator = progressIndicatorViewModel.Begin("Starting...");
                    activityLauncher.Start(sessionState.Student, activity, false)
					.Then((prevResult, onCompleted, onError) =>
                    {
						navigator.Transition(this, prevResult.ResultAs<ActivityViewModel>());
						onCompleted(true);
                    })
                    .Catch((Exception e) =>
                   {
                       navigator.Reveal<AlertViewModel>(alert =>
                        {
                            alert.Title = "Unable to start activity";
                            alert.Message = e.Message;
                            alert.Error = e;
                            alert.AlertDismissed += ((int index) => DebugLog.Info("Button {0} pressed", index));
						}).Start();

                   }).Finally(() =>
                   {
                       busyIndicator.Dispose();
					}).Start();
				}).Start();
			}
			catch (Exception e)
			{
				navigator.Reveal<AlertViewModel>(alert => 
				                                 {
					alert.Title = "Unable to start activity";
					alert.Message = e.Message;
					alert.Error = e;
				}).Start();
			}
		}

		public void SignOut()
		{
			DebugLog.Info("SignOut...");
			navigator.Reveal<ProgressIndicatorViewModel>().Then((vm, onRevealed, onRevealError) =>
			{
				ProgressIndicatorViewModel progressIndicatorViewModel = vm.ResultAs<ProgressIndicatorViewModel>();
				ProgressIndicatorViewModel.ProgressInfo busyIndicator = progressIndicatorViewModel.Begin("Signing out...");
				// TODO rgtaylor 2015-12-10 Replace hardcoded 'domain'
				sessionService.EndSession()
					.Then((prevResult, onCompleted, onError) =>
					{
						DebugLog.Info("Session ended");
						authenticator.SignOut().Start(onCompleted, onError);
					})
					.Then((prevResult, onCompleted, onError) =>
					{
						DebugLog.Info("Signed out");
						navigator.Transition(this, typeof(SignInViewModel));
						onCompleted(true);
					}).Catch((Exception e) =>
					{
						navigator.Reveal<AlertViewModel>(alert =>
						{
							alert.Title = "Unable to sign out";
							alert.Message = e.Message;
							alert.Error = e;
							alert.AlertDismissed += ((int index) => DebugLog.Info("Button {0} pressed", index));
						}).Start();

					}).Finally(() =>
					{
						busyIndicator.Dispose();
					}).Start();

				onRevealed(true);

			}).Start();
		}

        public void Settings()
        {            
			navigator.Transition(this, typeof(MetaTutorIVH.SettingsViewModel));
        }
	}
}
