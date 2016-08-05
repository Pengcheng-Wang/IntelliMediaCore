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
	public class SceneRouter : MonoBehaviour, IResolver
	{
		private readonly Dictionary<Type, ViewInfo> routeMap = new Dictionary<Type, ViewInfo>();

		[Serializable]
		public class ViewInfo
		{
			public string className;
			public string sceneName;
			public string[] viewCapabilities;
		}
		public ViewInfo[] routes;

		private StageManager stageManager;

		[Inject]
		public void Initialize(StageManager stageManager)
		{
			this.stageManager = stageManager;

			foreach (ViewInfo info in routes)
			{
				try
				{
					Type type = TypeFinder.ClassNameToType(info.className);
					routeMap[type] = info;
				}
				catch(Exception e)
				{
					DebugLog.Error("{0}: {1}", Name, e.Message);
				}
			}

			if (routeMap.Keys.Count == 0)
			{
				DebugLog.Warning("{0}: Zero routes defined. Remove this component if not used.", Name);
			}
		}

		public void Start()
		{	
			stageManager.Register(this);
		}			

		public void OnDestroy()
		{
			stageManager.Unregister(this);
		}

		public string Name { get { return gameObject.name; }}

		public IAsyncTask TryResolve<T>() where T : class
		{
			return Resolve(typeof(T), false);
		}

		public IAsyncTask Resolve<T>() where T : class
		{
			return Resolve(typeof(T), true);
		}

		public IAsyncTask TryResolve(Type type)
		{
			return Resolve(type, false);
		}

		public IAsyncTask Resolve(Type type)
		{
			return Resolve(type, true);
		}

		public IAsyncTask TryResolveViewFor(ViewModel vm, string[] capabilities = null)
		{
			return AsyncTask.WithResult(null);
		}

		public IAsyncTask ResolveViewFor(ViewModel vm, string[] capabilities = null)
		{
			return AsyncTask.WithResult(null);
		}

		private IAsyncTask Resolve(Type type, bool throwOnError)
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				if (!routeMap.ContainsKey(type))
				{
					if (throwOnError)
					{
						throw new Exception(string.Format("{0}: Unable to resolve '{1}'. Route not defined for this type.",
							Name, type.Name));
					}
					else
					{
						onCompleted(null);
						return;
					}
				}

				ViewInfo info = routeMap[type];

				DebugLog.Info("{0}: Resolved '{1}' to '{2}' scene. Loading scene...", Name, type.Name, info.sceneName);
				SceneService.Instance.LoadScene(info.sceneName, false, (bool success, string error) =>
				{
					if (success)
					{
						stageManager.Resolve(type).Start(onCompleted, onError);
					}
					else
					{
						onError(new Exception(String.Format("{0}: Unable to load '{1}' scene. {2}",
							Name, info.sceneName, error)));
					}
				});
			});
		}
	}
}