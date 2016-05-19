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
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Zenject;
using IntelliMedia;
using UnityEngine.Events;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace IntelliMedia
{
	public class VideoPlayerView : UnityGuiView<VideoPlayerViewModel>
	{
		public const int VideoPrevNextCacheSize = 1;
		public static readonly string[] VideoFilenameExtensions = { ".mov", ".mpg", ".mpeg", ".mp4" };

		public RawImage videoImage;
		public Color noVideoColor;
		public AudioSource audioSource;
		public Text title;
		public Text elapsedTime;

		public GameObject buffering;
		public GameObject controls;
		public Button skipBack;
		public Button pause;
		public Button play;
		public Button skipForward;

		public bool IsPaused { get; private set; }
		public bool IsPlaying { get; private set; }
		public bool IsBuffering 
		{ 
			get { return buffering.activeSelf; }
			private set { buffering.SetActive(value); }
		}
			
		private readonly Stopwatch VideoElapsedTimer = new Stopwatch();

		private class VideoInfo
		{
			public string Url { get; set; }
			public MovieTexture MovieTexture { get; set; }
			public bool IsStreaming { get; set; }
		}
		private List<VideoInfo> videoPlaylist = new List<VideoInfo>();

		protected override void OnBindingContextChanged(ViewModel oldViewModel, ViewModel newViewModel)
		{
			base.OnBindingContextChanged(oldViewModel, newViewModel);

			VideoPlayerViewModel oldVm = oldViewModel as VideoPlayerViewModel;
			if (oldVm != null)
			{
				oldVm.VideoPathProperty.ValueChanged -= OnVideoPathChanged;
				oldVm.PlaybackControlsVisibleProperty.ValueChanged -= OnPlaybackControlsVisibleChanged;
			}

			if (ViewModel != null)
			{
				ViewModel.VideoPathProperty.ValueChanged += OnVideoPathChanged;
				ViewModel.PlaybackControlsVisibleProperty.ValueChanged += OnPlaybackControlsVisibleChanged;

				// TODO rgtaylor 2016-04-28 Force all property refresh when binding context changes
				controls.SetActive(ViewModel.PlaybackControlsVisible);
			}				
		}

		private void OnVideoPathChanged(string oldPath, string newPath)
		{
			InitializeVideoPlaylist();
		}

		private void OnPlaybackControlsVisibleChanged(bool oldVisible, bool newVisible)
		{
			if (controls != null)
			{
				controls.SetActive(newVisible);
			}
		}

		private int currentVideoIndex = -1;
		public int CurrentVideoIndex
		{
			get { return currentVideoIndex; }
			set 
			{ 
				int prev = currentVideoIndex; 			
				currentVideoIndex = value;
				OnCurrentVideoIndexChanged(currentVideoIndex, prev);
			}
		}

		private void OnCurrentVideoIndexChanged(int newIndex, int prevIndex)
		{
			Contract.Argument("VideoIndex is out of range", "newIndex", videoPlaylist.Count != 0 && newIndex < videoPlaylist.Count);

			if (videoPlaylist[newIndex].MovieTexture != null)
			{
				CurrentVideoTexture = videoPlaylist[newIndex].MovieTexture;
				int nextVideoIndex = newIndex + 1;
				if (nextVideoIndex < videoPlaylist.Count && videoPlaylist[nextVideoIndex].MovieTexture == null)
				{
					// Load next video
					StartCoroutine(LoadVideo(nextVideoIndex, true));
				}
			}
			else
			{
				// Load current video
				StartCoroutine(LoadVideo(newIndex, true));
			}
		}
			
		#if UNITY_WEBGL
		public class MovieTexture : Texture
		{
			public void Stop() {}
			public void Play() {}
			public void Pause() {}
			public AudioClip audioClip;
			public bool isPlaying;
			public bool isReadyToPlay;
		}
		#endif

		public MovieTexture CurrentVideoTexture
		{
			get { return videoImage.texture as MovieTexture; }
			set
			{
				MovieTexture prevVideoTexture = videoImage.texture as MovieTexture;
				if (prevVideoTexture != null)
				{
					prevVideoTexture.Stop();
					audioSource.Stop();
				}
					
				VideoElapsedTimer.Reset();

				if (value != null)
				{
					// Reset video to beginning
					value.Stop();
					videoImage.texture = value;
					audioSource.clip = value.audioClip;
					title.text = value.name;

					if (IsPlaying)
					{
						Play();
					}
				}
				else
				{
					videoImage.texture = videoImage.texture = NoVideoTexture;
					audioSource.clip = null;
					title.text = "";
				}

				ReleaseVideoTextures();
			}
		}	

		private Texture2D noVideoTexture;
		private Texture2D NoVideoTexture
		{
			get
			{
				if (noVideoTexture == null)
				{
					noVideoTexture = new Texture2D(2, 2);
					noVideoTexture.SetPixels(new Color[]
					{
						noVideoColor,
						noVideoColor,
						noVideoColor,
						noVideoColor
					});
					noVideoTexture.Apply();
				}	

				return noVideoTexture;
			}
		}

		public void Update()
		{
			if (CurrentVideoTexture != null)
			{
				elapsedTime.text = String.Format(@"{0:hh\:mm\:ss\.f}", VideoElapsedTimer.Elapsed);
			}

			if (!IsPaused && IsPlaying && (CurrentVideoTexture != null && !CurrentVideoTexture.isPlaying)) 
			{
				OnCurrentVideoFinished();
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				ViewModel.PlaybackControlsVisible = !ViewModel.PlaybackControlsVisible;
			}
		}

		public void SkipBack()
		{
			if (CurrentVideoIndex - 1 >= 0)
			{
				CurrentVideoIndex--;
			}
			else
			{
				CurrentVideoIndex = 0;
				if (!IsPlaying && !IsPaused)
				{
					Play();
				}
			}
		}

		public void SkipForward()
		{
			if (CurrentVideoIndex + 1 < videoPlaylist.Count)
			{
				CurrentVideoIndex++;
			}
			else
			{
				if (!IsPaused)
				{				
					Stop();
				}
			}				
		}

		public void Pause()
		{
			if (CurrentVideoTexture != null)
			{
				DebugLog.Info("Pause movie: {0}", CurrentVideoTexture.name);
				pause.gameObject.SetActive(false);
				play.gameObject.SetActive(true);
				LogVideoStatus(TraceLog.Action.Paused);
				VideoElapsedTimer.Stop();
				CurrentVideoTexture.Pause();
				audioSource.Pause();	
				IsPaused = true;
			}			
		}

		public void Play()
		{
			if (CurrentVideoTexture != null)
			{
				DebugLog.Info("Play movie: {0}", CurrentVideoTexture.name);
				pause.gameObject.SetActive(true);
				play.gameObject.SetActive(false);
				LogVideoStatus(TraceLog.Action.Started);
				VideoElapsedTimer.Start();
				CurrentVideoTexture.Play();
				audioSource.Play();
				IsPaused = false;
				IsPlaying = true;
			}			
		}

		public void Stop()
		{
			pause.gameObject.SetActive(false);
			play.gameObject.SetActive(true);			
			LogVideoStatus(TraceLog.Action.Ended);
			CurrentVideoTexture = null;
			IsPlaying = false;
			IsPaused = false;
		}

		public void Eject()
		{
			Stop();
			ViewModel.SaveAndQuit();
		}

		private void LogVideoStatus(TraceLog.Action action)
		{
			Dictionary<string, object> context = new Dictionary<string, object>();
			context["ElapsedTime"] = VideoElapsedTimer.Elapsed.TotalSeconds;
			if (CurrentVideoTexture != null)
			{
				context["VideoFile"] = System.IO.Path.GetFileName(CurrentVideoTexture.name);
			}

			TraceLog.Write("System", action, "Video", context);
		}

		private void InitializeVideoPlaylist()
		{
			Contract.PropertyNotNull("ViewModel", ViewModel);

			DebugLog.Info("Initialize video play list from: {0}", 
				(ViewModel.VideoPath != null ? ViewModel.VideoPath : "null"));

			// Reset video controls
			pause.gameObject.SetActive(false);
			skipBack.interactable = false;
			play.interactable = false;
			skipForward.interactable = false;

			CurrentVideoTexture = null;
			title.text = "No video";
			elapsedTime.text = "-- : --";

			videoPlaylist.Clear();

			if (String.IsNullOrEmpty(ViewModel.VideoPath))
			{
				return;
			}

			string videoSubdirectoryOrUrl = ViewModel.VideoPath;

			try
			{
				Uri uri = new Uri(videoSubdirectoryOrUrl, UriKind.RelativeOrAbsolute);
				if (uri.IsAbsoluteUri)
				{
					videoPlaylist.Add(new VideoInfo()
					{
						Url = videoSubdirectoryOrUrl,
						IsStreaming = true
					});
				}
				else
				{
					string playlistPath = Path.Combine("Video", videoSubdirectoryOrUrl);
					videoPlaylist.AddRange(FindVideosInResources(playlistPath));
					videoPlaylist.AddRange(FindVideosInStreamingAssets(playlistPath));
				}
			}
			catch(Exception e)
			{
				DebugLog.Error("Unable to initialize video playlist. {0}", e.Message);
			}
				
			if (videoPlaylist.Count > 0) 
			{
				skipBack.interactable = true;
				play.interactable = true;
				skipForward.interactable = true;
				skipForward.gameObject.SetActive(videoPlaylist.Count > 1);

				CurrentVideoIndex = 0;
				ViewModel.PlaybackControlsVisible = false;
				Play();
			}				
		}

		private IEnumerable<VideoInfo> FindVideosInResources(string path)
		{
			List<VideoInfo> foundVideos = new List<VideoInfo>();

			foreach (MovieTexture movieTexture in Resources.LoadAll<MovieTexture>(path))
			{
				foundVideos.Add(new VideoInfo()
				{
					Url = movieTexture.name,
					MovieTexture = movieTexture,
					IsStreaming = false
				});
			}

			return foundVideos;
		}

		private IEnumerable<VideoInfo> FindVideosInStreamingAssets(string path)
		{
			List<VideoInfo> foundVideos = new List<VideoInfo>();

			string fullPath = Path.Combine(Application.streamingAssetsPath, path);
			if (Directory.Exists(fullPath))
			{
				foreach(string videoFilename in System.IO.Directory.GetFiles(fullPath, "*.*"))
				{
					foreach(string ext in VideoFilenameExtensions)
					{
						if (videoFilename.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
						{
							foundVideos.Add(new VideoInfo()
							{
								Url = "file://" + videoFilename,
								IsStreaming = true
							});
							break;
						}
					}
				}
			}

			return foundVideos;
		}			

		public IEnumerator LoadVideo(int videoIndex, bool backgroundLoad)
		{
			Contract.Argument("videoIndex is out of range", "videoIndex", videoIndex >=0 && videoIndex < videoPlaylist.Count);

			string videoUrl = videoPlaylist[videoIndex].Url;

			if (videoPlaylist[videoIndex].MovieTexture == null)
			{
				WWW www = null;
				try
				{
					IsBuffering = true;
					DebugLog.Info("Load video: {0}", videoUrl);
					www = new WWW(videoUrl);
					#if !UNITY_WEBGL
					videoPlaylist[videoIndex].MovieTexture = www.movie;
					#endif
					yield return www;
					DebugLog.Info ("Video load isDone = {0}", www.isDone);
				}
				finally
				{
					IsBuffering = false;
				}

				if (www == null || !String.IsNullOrEmpty(www.error))
				{
					ViewModel.HandleError("Load failed",
						String.Format("Failed to load '{0}'. {1}", videoUrl,
							(www != null ? www.error : "Unable to allocate download object.")));
					
					yield return null;
				}				
			}


			if (!videoPlaylist[videoIndex].MovieTexture.isReadyToPlay)
			{
				DebugLog.Info("Waiting for video isReadyToPlay: {0}", videoUrl);
				while(!videoPlaylist[videoIndex].MovieTexture.isReadyToPlay)
				{
					yield return new WaitForEndOfFrame();
				}
			}

			if (videoIndex == CurrentVideoIndex)
			{
				CurrentVideoTexture = videoPlaylist[videoIndex].MovieTexture;
			}						

			DebugLog.Info ("Video '{0}' isReadyToPlay = {1}", 
				videoUrl, videoPlaylist[videoIndex].MovieTexture.isReadyToPlay);
		}

		private void ReleaseVideoTextures()
		{
			for (int index = 0; index < videoPlaylist.Count; ++index)
			{
				// Is dynamically loaded video outside cache window?
				if (videoPlaylist[index].IsStreaming
					&& Math.Abs(CurrentVideoIndex - index) > VideoPrevNextCacheSize
					&& videoPlaylist[index].MovieTexture != null)
				{
					DebugLog.Info("Release video: {0}",  videoPlaylist[index].MovieTexture.name);
					Destroy(videoPlaylist[index].MovieTexture);
					videoPlaylist[index].MovieTexture = null;
				}
			}				
		}

		private void OnCurrentVideoFinished()
		{
			DebugLog.Info("Finished current movie: {0}", CurrentVideoTexture.name);
			if ((CurrentVideoIndex + 1) < videoPlaylist.Count) 
			{
				CurrentVideoIndex++;
			}
			else
			{
				OnLastVideoFinished();
			}
		}

		private void OnLastVideoFinished()
		{
			DebugLog.Info("All movies finished");
			Stop();
			if (ViewModel != null)
			{
				ViewModel.OnFinishedPlayingVideos();
			}
		}
	}
}