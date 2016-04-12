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
using System;
using System.Collections;

namespace IntelliMedia
{
	public class SessionService 
	{
		private AppSettings appSettings;
		private LogEntry sessionStartedEntry;

		public bool IsSessionStarted { get { return sessionStartedEntry != null; }}

		public SessionService(AppSettings appSettings)
		{
			this.appSettings = appSettings;
		}

		public AsyncTask StartSession(Student student)
		{
			return new AsyncTask((prevResult, onCompleted, onError) =>
			{
				try
				{			
					if (IsSessionStarted)
					{
						throw new Exception("Attempting to start session without ending previous session");
					}

					Uri serverUri = new Uri(appSettings.ServerURI, UriKind.RelativeOrAbsolute);
					Uri restUri = new Uri(serverUri, "rest/");

					SessionRepository repo = new SessionRepository(restUri);
					if (repo == null)
					{
						throw new Exception("SessionRepository is not initialized.");
					}

					// Get session info
					repo.GetByKey(student.SessionGuid, (response) =>
					{
						if (response.Success)
						{
							// Update session info
							Session session = response.Item;
							UpdatePlatformInfo(session);
							StartLogging(student, session);
							repo.Update(session, (SessionRepository.Response updateResponse) =>
							{
								if (updateResponse.Success)
								{
									onCompleted(updateResponse.Item);
								}
								else
								{
									onError(new Exception(updateResponse.Error));
								}
							});
						}
						else
						{
							onError(new Exception(response.Error));
						}
					});
				}
				catch (Exception e)
				{
					onError(e);
				}
			});
		}

		public AsyncTask EndSession()
		{
			return new AsyncTask((prevResult, onCompleted, onError) =>
			{
				try
				{					
					if (!IsSessionStarted)
					{
						throw new Exception("Attempting to end session that has not been started");
					}

					EndLogging((bool success, string error) =>
					{
						if (success)
						{
							onCompleted(true);
						}
						else
						{
							onError(new Exception(error));
						}						
					});
				}
				catch (Exception e)
				{
					onError(e);
				}
			});
		}

		void StartLogging(Student student, Session session)
		{
			// Enable Trace Logging 
			string filename = String.Format ("{0}.{1}", session.Id, SerializerCsv.Instance.FilenameExtension);
			TraceLog.Open (session.Id, new FileLogger (System.IO.Path.Combine (appSettings.TraceDataDirectory, filename), SerializerCsv.Instance, appSettings.WriteTraceDataToLocalFile)//, new RepositoryLogger(TraceDataDirectory, SerializerXml.Instance, true)
			);
			sessionStartedEntry = TraceLog.Player(TraceLog.Action.Started, "Session", 
				"Username", student.Username, 
				"SubjectId", student.SubjectId, 
				"InstitutionName", student.InstitutionName, 
				"InstructorName", student.InstructorName, 
				"CourseName", student.CourseName, 
				"Platform", session.Platform, 
				"OperatingSystem", session.OperatingSystem, 
				"WebBrowser", session.WebBrowser, 
				"GameVersion", session.GameVersion, 
				"GameReleaseType", session.GameReleaseType);
		}

		void EndLogging(IntelliMedia.TraceLog.CloseCallback callback)
		{			
			TraceLog.Player(sessionStartedEntry, TraceLog.Action.Ended, "Session");
			sessionStartedEntry = null;
			TraceLog.Close(callback);
		}

		private void UpdatePlatformInfo(Session session)
		{
			session.Platform = UnityEngine.Application.platform.ToString();
			session.OperatingSystem = UnityEngine.SystemInfo.operatingSystem;

			session.GameVersion = MetaTutorIVH.AssemblyInfo.Version.ToString(); 
//			WebBrowserUtility webBrowser = Global.Component<WebBrowserUtility>();
//			if (UnityEngine.Application.isWebPlayer && webBrowser != null)
//			{
//				session.WebBrowser = webBrowser.DisplayName;
//				session.WebBrowserVersion = webBrowser.DisplayVersion;
//			}

			session.ProcessorType = UnityEngine.SystemInfo.processorType;
			session.ProcessorCount = UnityEngine.SystemInfo.processorCount.ToString();
			session.SystemMemorySize = UnityEngine.SystemInfo.systemMemorySize.ToString();
			session.GraphicsMemorySize = UnityEngine.SystemInfo.graphicsMemorySize.ToString();
			session.GraphicsDeviceName = UnityEngine.SystemInfo.graphicsDeviceName;
			session.GraphicsShaderLevel = UnityEngine.SystemInfo.graphicsShaderLevel.ToString();
		}
	}
}
