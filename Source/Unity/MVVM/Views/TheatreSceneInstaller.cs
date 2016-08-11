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
using UnityEngine;

namespace IntelliMedia
{
	public class TheatreSceneInstaller : TheatreViewsInstaller
	{
		public string[] viewModels;
		public string[] models;
		public string[] services;
		public string[] repositories;

		public TypeProxySceneResolver.RoutingInfo[] SceneProxyTypes;

		protected bool DontRegisterTheatreResolvers { get; set; }

		public class PostInitializeRegister : IDisposable
		{
			private Resolver Resolver { get; set; }
			private TypeProxySceneResolver TypeProxySceneResolver { get; set; }
			private StageManager StageManager { get; set; }

			public PostInitializeRegister(Resolver resolver, TypeProxySceneResolver typeProxySceneResolver, StageManager stageManager, SceneService sceneService)
			{
				Resolver = resolver;
				TypeProxySceneResolver = typeProxySceneResolver;
				StageManager = stageManager;

				StageManager.Register(Resolver);
				TypeProxySceneResolver.StageManager = StageManager;
				TypeProxySceneResolver.SceneService = sceneService;
				StageManager.Register(TypeProxySceneResolver);
			}

			#region IDisposable implementation

			public void Dispose ()
			{
				StageManager.Unregister(TypeProxySceneResolver);
				StageManager.Unregister(Resolver);
			}

			#endregion
		}

		private Resolver Resolver { get; set; }
		private TypeProxySceneResolver TypeProxySceneResolver { get; set; }

		public override void InstallBindings()
		{	
			base.InstallBindings();

			InstallBindingsByName("ViewModels", viewModels);
			InstallBindingsByName("Models", models);	
			InstallBindingsByName("Services", services);	
			InstallBindingsByName("Repositories", repositories);

			InstallTheatreResolverBindings();

			if (TotalBindings == 0)
			{
				DebugLog.Warning("{0} doesn't specify any classes for binding.", this.gameObject.name);
			}				
		}

		public override void Start ()
		{
			base.Start ();

			Type[] views = Container.ResolveTypeAll(typeof(IView)).ToArray();
			if (views != null)
			{
			}

			ProgressIndicatorView view = Container.Resolve(typeof(ProgressIndicatorView)) as ProgressIndicatorView;
			if (view != null)
			{
			}
		}

		void InstallTheatreResolverBindings()
		{
			if (!DontRegisterTheatreResolvers)
			{
				Resolver = new Resolver(gameObject.name, Container);
				TypeProxySceneResolver = new TypeProxySceneResolver(Resolver.Name + "ProxyResolver", SceneProxyTypes);

				Container.Bind(typeof(IDisposable)).To<PostInitializeRegister>().AsSingle().WithArguments(Resolver, TypeProxySceneResolver).NonLazy();
			}
		}

		void InstallBindingsByName(string groupName, string[] classNames)
		{
			if (classNames == null || classNames.Length == 0)
			{
				return;
			}
				
			foreach (string className in classNames)
			{
				Type type = TypeFinder.ClassNameToType (className);
				if (type.IsSubclassOf(typeof(MonoBehaviour)))
				{
					Container.Bind(type).FromGameObject().WithGameObjectName(type.Name).AsSingle();
				}
				else
				{
					Container.Bind(type).AsSingle();
				}
				++TotalBindings;
			}
			DebugLog.Info ("{0}: Installed {1} bindings: {2}", 
				gameObject.name, groupName, string.Join(", ", classNames));
		}
	}
}