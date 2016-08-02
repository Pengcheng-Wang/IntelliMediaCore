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
using Zenject;
using IntelliMedia;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IntelliMedia
{
	public class TheatreViewRouter : MonoBehaviour, IViewResolver
	{
		[Serializable]
		public class ViewInfo
		{
			public string viewModelTypeName;
			public string sceneName;
			public string[] viewCapabilities;
		}
		public ViewInfo[] routes;

		private StageManager stageManager;

		[Inject]
		public void Initialize(StageManager stageManager)
		{
			this.stageManager = stageManager;
		}

		public void Start()
		{	
			stageManager.Register(this);
		}			

		public void OnDestroy()
		{
			stageManager.Unregister(this);
		}

		#region IViewResolver implementation

		public string Name { get { return gameObject.name; }}

		public IView Resolve(Type viewModelType, string[] capabilities = null)
		{

				ViewInfo viewInfo = routes.FirstOrDefault(v => String.Compare(v.viewModelTypeName, viewModelType.Name) == 0
					&& ViewDescriptorAttribute.HasCapabilities(capabilities, v.viewCapabilities));
				if (viewInfo != null)
				{
					UnityProxyView proxyView = new UnityProxyView(stageManager);
					proxyView.SceneName = viewInfo.sceneName;
					proxyView.Capabilities = viewInfo.viewCapabilities;
					return proxyView;
				}
				else
				{
				return null;
				}
		}

		#endregion			
	}
}