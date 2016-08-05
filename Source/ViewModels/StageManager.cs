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
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace IntelliMedia
{
	public class StageManager
	{
		private readonly List<IResolver> resolvers = new List<IResolver>();

		private HashSet<IView> revealedViews = new HashSet<IView>();

		public void Register(IResolver resolver)
		{
			DebugLog.Info("StageManager registered resolver: {0}", resolver.Name);
			resolvers.Add(resolver);
		}

		public void Unregister(IResolver resolver)
		{
			DebugLog.Info("StageManager unregistered resolver: {0}", resolver.Name);
			resolvers.Remove(resolver);
		}			

		public IAsyncTask Resolve(Type type)
		{ 
			Contract.ArgumentNotNull("type", type);

			return new AsyncTry(resolvers.Select(r => r.TryResolve(type)).ForEach())
				.Then<IEnumerable<IAsyncTask>>((tasks) =>
				{
					IAsyncTask[] array = tasks.ToArray();
					int totalFound = tasks.Count(t => t.Result != null);
					if (totalFound == 0)
					{
						throw new Exception(string.Format("Unable to resolve '{0}' class", type.Name));
					}
					else if (totalFound > 1)
					{
						throw new Exception(string.Format("Found {0} matches for '{1}' class. Expected exactly 1.", 
							totalFound, type.Name));
					}

					return tasks.FirstOrDefault(t => t.Result != null).Result;
				});
		}

		private IAsyncTask ResolveViewType(ViewModel vm)
		{ 
			Contract.ArgumentNotNull("viewModelType", vm);

			return new AsyncTry(resolvers.Select(r => r.TryResolveViewFor(vm)).ForEach())
				.Then<IEnumerable<IAsyncTask>>((tasks) =>
				{
					IAsyncTask[] array = tasks.ToArray();
					int totalFound = tasks.Count(t => t.Result != null);
					if (totalFound == 0)
					{
						throw new Exception(string.Format("Unable to resolve view for '{0}'", vm.GetType().Name));
					}
					else if (totalFound > 1)
					{
						throw new Exception(string.Format("Found {0} views for '{1}' class. Expected exactly 1.", 
							totalFound, vm.GetType().Name));
					}

					return tasks.FirstOrDefault(t => t.Result != null).Result;
				});
		}

		public void Transition(ViewModel from, ViewModel to)
		{
			Contract.ArgumentNotNull("from", from);
			Contract.ArgumentNotNull("to", to);

			DebugLog.Info("StageManager.Transition: {0} -> {1}", from.GetType().Name, to.GetType().Name);

			Hide(from).Start((vm) =>
			{
				Reveal(to).Start();
			});
		}

		public void Transition(ViewModel from, Type toViewModelType)
		{
			Contract.ArgumentNotNull("toViewModelType", toViewModelType);

			Resolve(toViewModelType).Start((result) => 
			{
				Transition(from, (ViewModel)result);
			});
		}

		public IAsyncTask Reveal(ViewModel vm)
		{
			Contract.ArgumentNotNull("vm", vm);

			DebugLog.Info("StageManager.Reveal: {0}", vm.GetType().Name);

			IView revealedView = revealedViews.FirstOrDefault(v => v.BindingContext == vm);
			if (revealedView == null)
			{
				return new AsyncTry(ResolveViewType(vm))
					.Then<Type>((viewType) => Resolve(viewType))
					.Then<IView>((view) =>
					{
						view.BindingContext = vm;
						view.HiddenEvent.EventTriggered += OnViewHidden;
						return view.Reveal(true);
					})						
					.Then<IView>((view) =>
	                {
						revealedViews.Add(view);
						return view.BindingContext;
	                });
            }
			else
			{
				return AsyncTask.WithResult(revealedView.BindingContext);
			}		
		}

		public IAsyncTask Reveal<TViewModel>(Action<TViewModel> setStateAction = null) where TViewModel : ViewModel
		{
			return new AsyncTry(Resolve(typeof(TViewModel)))
				.Then<TViewModel>((vm) =>
				{					
					if (setStateAction != null)
					{
						setStateAction(vm);
					}
					return Reveal(vm);
				})
				.Catch((e) =>
				{
					DebugLog.Error("Unable to reveal '{0}'. {1}", typeof(TViewModel).Name, e.Message);
				});
		}			

		public IAsyncTask Hide(ViewModel vm)
		{
			Contract.ArgumentNotNull("vm", vm);

			DebugLog.Info("StageManager.Hide: {0}", vm.GetType().Name);

			IView view = revealedViews.FirstOrDefault(v => v.BindingContext == vm);			
			if (view != null)
			{			
				return new AsyncTry(view.Hide(true))
					.Then<IView>((hiddenView) =>
					{
						revealedViews.Remove(hiddenView);
						return vm;
					});
			}
			else
			{
				return AsyncTask.WithResult(null);
			}
		}

		private void OnViewHidden(IView view)
		{
			if (revealedViews.Contains(view))
			{
				revealedViews.Remove(view);
			}
		}
	}
}
