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

		public void Reveal(bool immediate = false, OnceEvent<IView>.OnceEventHandler handler = null)
		{
			SceneService.Instance.LoadScene(SceneName, false, (bool success, string error) =>
			{
				if (success)
				{
					stageManager.Hide(BindingContext, (IView sceneView) =>
					{
						stageManager.Reveal(BindingContext, (IView view) =>
						{
							if (handler != null)
							{
								handler(view);
							}
						}).Start();						
					});
				}
				else
				{
					DebugLog.Error("Unable to load '{0}' scene. {1}", SceneName, error);
				}
			});

			if (handler != null)
			{
				handler(this);
			}
		}

		public void Hide (bool immediate = false, OnceEvent<IView>.OnceEventHandler handler = null)
		{
			if (handler != null)
			{
				handler(this);
			}
		}

		public ViewModel BindingContext { get; set; }

		#endregion

		public class Factory : Factory<UnityProxyView>
		{
		}
	}
}