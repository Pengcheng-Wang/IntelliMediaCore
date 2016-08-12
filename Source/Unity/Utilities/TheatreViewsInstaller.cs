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
using IntelliMedia.Views;

namespace IntelliMedia.Utilities
{
	public class TheatreViewsInstaller : MonoInstaller
	{
		[Serializable]
		public class PrefabViewInfo
		{
			public UnityView prefab;
			public string gameObjectGroupName;			
			public bool singleton;
		}
		public PrefabViewInfo[] viewPrefabs;

		protected int TotalBindings { get; set; }

		public override void InstallBindings()
		{	
			InstallViewPrefabBindings();
			InstallViewBindings();
		}			

		void InstallViewBindings()
		{
			StringBuilder views = new StringBuilder();
			foreach(UnityView view in this.transform.GetComponentsInChildren<UnityView>(true))
			{
				Container.Bind(typeof(IView), view.GetType()).To(view.GetType()).FromInstance(view).AsSingle();
				++TotalBindings;
				views.AppendFormat("{0}, ", view.name);
			}
			DebugLog.Info ("{0}: Installed View bindings: {1}", 
				gameObject.name, views.ToString().TrimEnd(','));
		}

		void InstallViewPrefabBindings()
		{						
			StringBuilder prefabs = new StringBuilder();
			foreach (PrefabViewInfo prefabInfo in viewPrefabs)
			{
				if (prefabInfo.prefab == null)
				{
					throw new Exception("Missing view prefab");
				}

				Type viewType = prefabInfo.prefab.GetType();

				// By default, the view should be hidden until revealed by the StageManager
				if (prefabInfo.prefab.gameObject.activeSelf)
				{
					prefabInfo.prefab.gameObject.SetActive(false);
				}

				if (!string.IsNullOrEmpty(prefabInfo.gameObjectGroupName))
				{
					Container.Bind(typeof(IView), viewType).To(viewType).FromPrefab(prefabInfo.prefab.gameObject).UnderGameObjectGroup(prefabInfo.gameObjectGroupName).AsSingle();
				}
				else
				{
					Container.Bind(typeof(IView), viewType).To(viewType).FromPrefab(prefabInfo.prefab.gameObject).AsSingle();					
				}
				++TotalBindings;
				prefabs.AppendFormat("{0}, ", viewType.Name);
			}
			DebugLog.Info ("{0}: Installed Prefab View bindings: {1}", 
				gameObject.name, prefabs.ToString().TrimEnd(','));
		}
	}
}