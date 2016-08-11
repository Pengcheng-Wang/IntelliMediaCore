//---------------------------------------------------------------------------------------
// Copyright 2016 North Carolina State University
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
using System.Text.RegularExpressions;
using IntelliMedia.Services;
using IntelliMedia.Utilities;

namespace IntelliMedia.ViewModels
{
	public class VideoPlayer : ActivityViewModel
	{
		private const string videoUriPattern = @"^urn:video:(?<VideoPath>.+)$";
			
		public readonly BindableProperty<string> VideoPathProperty = new BindableProperty<string>();	
		public string VideoPath 
		{ 
			get { return VideoPathProperty.Value; }
			set { VideoPathProperty.Value = value; }
		}

		public readonly BindableProperty<bool> PlaybackControlsVisibleProperty = new BindableProperty<bool>();	
		public bool PlaybackControlsVisible 
		{ 
			get { return PlaybackControlsVisibleProperty.Value; }
			set { PlaybackControlsVisibleProperty.Value = value; }
		}

		public VideoPlayer(StageManager navigator, ActivityService activityService) : base(navigator, activityService)
		{
		}

		public void OnFinishedPlayingVideos()
		{
			navigator.Reveal<Alert> (alert => 
			{
				alert.Title = "Playback complete";
				alert.Message = "Press OK to return to return to the menu";
				alert.AlertDismissed += (int indexer) =>
				{
					ActivityState.IsComplete = true;
					ActivityState.ModifiedDate = DateTime.Now;
					SaveAndQuit();
				};
			}).Start ();			
		}

		public void HandleError(string title, string error)
		{
			PlaybackControlsVisible = true;

			navigator.Reveal<Alert> (alert => 
			{
				alert.Title = title;
				alert.Message = error;
			}).Start ();			
		}

		public override void OnStartReveal()
		{
			base.OnStartReveal();

			ExtractVideoPathFromActivityUri();
		}

		private void ExtractVideoPathFromActivityUri()
		{
			VideoPath = null;
			Regex uriRegex = new Regex (videoUriPattern, RegexOptions.IgnoreCase);
			MatchCollection matches = uriRegex.Matches (Activity.Uri);
			if (matches.Count > 0 && matches [0].Success)
			{
				VideoPath = matches [0].Groups ["VideoPath"].Value;
			}
			if (String.IsNullOrEmpty (VideoPath))
			{
				HandleError(
					"Activity URI does not specify video path",
					String.Format ("URI ({0}) does not contain video path for {1} activity.", Activity.Uri, Activity.Name));
			}
		}
								
		public void SaveAndQuit()
		{
			SaveActivityStateAndTransition<MainMenu>();
		}
	}
}
