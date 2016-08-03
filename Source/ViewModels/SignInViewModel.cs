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

namespace IntelliMedia
{
	public class SignInViewModel : ViewModel
	{
		private StageManager navigator;
		private AppSettings appSettings;
		private SessionState sessionState;
		private AuthenticationService authenticator;
		private SessionService sessionService;
		private CourseSettingsService courseSettingsService;

		public SignInViewModel(
			StageManager navigator, 
			AppSettings appSettings,
			SessionState sessionState,
			AuthenticationService authenticator,
			SessionService sessionService,
			CourseSettingsService courseSettingsService)
		{
			this.navigator = navigator;
			this.appSettings = appSettings;
			this.sessionState = sessionState;
			this.authenticator = authenticator;
			this.sessionService = sessionService;
			this.courseSettingsService = courseSettingsService;
		}

		public string Version
		{
			get { return appSettings.Version != null ? appSettings.Version : "unknown version"; }
		}

		public void SignIn(string group, string username, string password)
		{
			try
			{
				if (string.IsNullOrEmpty(username))
				{
					throw new Exception("Username is blank");
				}

				if (string.IsNullOrEmpty(password))
				{
					throw new Exception("Password is blank");
				}

				sessionState.Student = null;
				sessionState.Session = null;

				DebugLog.Info("SignIn {0}...", username);
				ProgressIndicatorViewModel.ProgressInfo busyIndicator = null;
				new AsyncTry(navigator.Reveal<ProgressIndicatorViewModel>())
					.Then<ProgressIndicatorViewModel>((progressIndicatorViewModel) =>
                	{
                    	busyIndicator = progressIndicatorViewModel.Begin("Signing in...");
						return authenticator.SignIn(group, username, password);
					})
					.Then<Student>((student) =>
                    {
						DebugLog.Info("Signed in {0}", username);
						sessionState.Student = student;
						return sessionService.StartSession(sessionState.Student);
                    })
					.Then<Session>((session) =>
                    {
						DebugLog.Info("Session started");
						sessionState.Session = session;
						return courseSettingsService.LoadSettings(sessionState.Student.Id);
                    })
					.Then<CourseSettings>((settings) =>
                    {
						return new AsyncTask((onComplete, onError) =>
						{
							DebugLog.Info("Settings loaded");
							sessionState.CourseSettings = settings;
                        	navigator.Transition(this, typeof(MainMenuViewModel));
							onComplete(true);
						});

                    }).Catch((Exception e) =>
                    {
                        navigator.Reveal<AlertViewModel>(alert =>
                        {
                            alert.Title = "Unable to sign in";
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
			catch (Exception e)
			{
				navigator.Reveal<AlertViewModel>(alert => 
				{
					alert.Title = "Unable to sign in";
					alert.Message = e.Message;
					alert.Error = e;
				}).Start();
			}
		}
	}
}
