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
using IntelliMedia.ViewModels;
using IntelliMedia.Services;
using IntelliMedia.Utilities;
using IntelliMedia.Views;

namespace IntelliMedia.Utilities
{
	public class TypeProxySceneResolver : ITheatreResolver
	{
		public StageManager StageManager { get; set; }
		public SceneService SceneService { get; set; }

		[Serializable]
		public class RoutingInfo
		{
			public string typeName;
			public string sceneName;
		}

		private readonly Dictionary<Type, RoutingInfo> TypeRoutes = new Dictionary<Type, RoutingInfo>();

		public string Name { get; private set; }

		public TypeProxySceneResolver(string name, IEnumerable<RoutingInfo> routes)
		{
			Contract.ArgumentNotNull("name", name);
			Contract.ArgumentNotNull("routes", routes);

			Name = name;

			foreach(RoutingInfo route in routes)
			{
				Type type = TypeFinder.ClassNameToType(route.typeName);
				if (TypeRoutes.ContainsKey(type))
				{
					throw new Exception("Attempting to define two routes for same type: " + type.Name);
				}
				TypeRoutes[type] = route;
			}
		}

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
			return ResolveViewFor( vm.GetType(), capabilities, false);
		}

		public IAsyncTask ResolveViewFor(ViewModel vm, string[] capabilities = null)
		{
			return ResolveViewFor( vm.GetType(), capabilities, true);
		}

		private IAsyncTask ResolveViewFor(Type vmType, string[] capabilities, bool throwOnError)
		{
			return new AsyncTask((onCompleted, onError) =>
			{			
				Type viewType = null;
				ViewDescriptorAttribute attribute = null;
				foreach(Type iviewType in TypeRoutes.Keys)
				{
					DebugLog.Info("IView: {0}", iviewType.Name);
					attribute = ViewDescriptorAttribute.FindOn(iviewType);
					if (attribute != null && attribute.ViewModelType == vmType 
						&& attribute.HasCapabilities(capabilities))
					{
						viewType = iviewType;
						break;
					}
				}

				if (viewType == null && throwOnError)
				{
					throw new Exception(string.Format("Unable to find IView with ViewDescriptorAttribute.ViewModelType = '{0}'",
						vmType.Name));
				}

				onCompleted(viewType);
			});
		}

		private IAsyncTask Resolve(Type type, bool throwOnError)
		{
			if (!TypeRoutes.ContainsKey(type) && !throwOnError)
			{
				return AsyncTask.WithResult(null);
			}
			else
			{
				return new AsyncTry(
					new AsyncTask((onCompleted, onError) =>
					{
						if (!TypeRoutes.ContainsKey(type))
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

						DebugLog.Info("{0}: Resolved '{1}' to '{2}' scene. Loading scene...", Name, type.Name, TypeRoutes[type].sceneName);
						onCompleted(TypeRoutes[type]);
					}))
				.Then<RoutingInfo>((info) => SceneService.LoadScene(info.sceneName, false))
				.Then<bool>((success) =>
				{
					return StageManager.Resolve(type);
				});
			}
		}
	}
}