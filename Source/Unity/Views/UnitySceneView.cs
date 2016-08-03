//---------------------------------------------------------------------------------------
// Copyright 2015 North Carolina State University
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
using System.Collections.Generic;
using System;
using IntelliMedia;
using UnityEngine.EventSystems;

namespace IntelliMedia
{	
	public class UnityProxyView : IView
	{
		private StageManager stageManager;
		public string SceneName { get; set; }
		public string[] Capabilities { get; set; }

		public UnityProxyView(StageManager stageManager)
		{
			Contract.ArgumentNotNull("stageManager", stageManager);
			this.stageManager = stageManager;
		}

		#region IView implementation

		public IAsyncTask Reveal(bool immediate = false)
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				SceneService.Instance.LoadScene(SceneName, false, (bool success, string error) =>
				{
					if (success)
					{
						new AsyncTry(stageManager.Hide(BindingContext))
							.Then<ViewModel>((vm) => stageManager.Reveal(BindingContext))
							.Catch((e) =>
							{
								onError(new Exception(String.Format("Unable to reveal '{0}' in '{1}' scene. {2}", 
									BindingContext.GetType().Name, SceneName, e.Message)));
							})
							.Start();
					}
					else
					{
						onError(new Exception(String.Format("Unable to load '{0}' scene. {1}", SceneName, error)));
					}
				});
			});
		}

		public IAsyncTask Hide (bool immediate = false)
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				onCompleted(this);
			});
		}

		public ViewModel BindingContext { get; set; }

		#endregion

		public class Factory : Factory<UnityProxyView>
		{
		}
	}
}