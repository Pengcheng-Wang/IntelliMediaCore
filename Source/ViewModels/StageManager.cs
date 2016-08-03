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
		private List<ViewModelFactory> viewModelFactories = new List<ViewModelFactory>();
		private List<IViewResolver> viewFactories = new List<IViewResolver>();

		private HashSet<IView> revealedViews = new HashSet<IView>();

		public void Register(ViewModelFactory factory)
		{
			DebugLog.Info("StageManager registered ViewModel factory: {0}", factory.Name);
			viewModelFactories.Add(factory);
		}

		public void Unregister(ViewModelFactory factory)
		{
			DebugLog.Info("StageManager unregistered ViewModel factory: {0}", factory.Name);
			viewModelFactories.Remove(factory);
		}

		public void Register(IViewResolver factory)
		{
			DebugLog.Info("StageManager registered View factory: {0}", factory.Name);
			viewFactories.Add(factory);
		}

		public void Unregister(IViewResolver factory)
		{
			DebugLog.Info("StageManager unregistered View factory: {0}", factory.Name);
			viewFactories.Remove(factory);
		}

		public IAsyncTask ResolveViewModel(Type viewModelType)
		{ 
			Contract.ArgumentNotNull("viewModelType", viewModelType);

			return new AsyncTry(viewModelFactories.Select(r => r.TryResolve(viewModelType)).ForEach((result) => result != null))
				.Then<object>((result) =>
				{
					if (!viewModelType.IsInstanceOfType(result))
					{
						throw new Exception(string.Format("Unable to resolve '{0}' class", viewModelType.Name));
					}
					return result;
				});
		}

		private IView ResolveViewForViewModel(Type viewModelType)
		{ 
			Contract.ArgumentNotNull("viewModelType", viewModelType);

			foreach(IViewResolver factory in viewFactories)
			{
				IView view = factory.Resolve(viewModelType);
				if (view != null)
				{
					return view;
				}
			}

			throw new Exception("Unable to resolve View for: " + viewModelType.Name);
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

			ResolveViewModel(toViewModelType).Start((result) => 
			{
				Transition(from, (ViewModel)result);
			});
		}

		public IAsyncTask Reveal(ViewModel vm)
		{
			Contract.ArgumentNotNull("vm", vm);

			DebugLog.Info("StageManager.Reveal: {0}", vm.GetType().Name);

			IView view = revealedViews.FirstOrDefault(v => v.BindingContext == vm);
			if (view == null)
			{
				view = ResolveViewForViewModel(vm.GetType());
				view.BindingContext = vm;
				return new AsyncTry(view.Reveal(true))
					.Then<IView>((revealedView) =>
	                {
						revealedViews.Add(revealedView);
						return revealedView.BindingContext;
	                });
            }
			else
			{
				return AsyncTask.WithResult(view.BindingContext);
			}		
		}

		public IAsyncTask Reveal<TViewModel>(Action<TViewModel> setStateAction = null) where TViewModel : ViewModel
		{
			return new AsyncTry(ResolveViewModel(typeof(TViewModel)))
				.Then<TViewModel>((vm) =>
				{					
					if (setStateAction != null)
					{
						setStateAction(vm);
					}
					return vm;
				})
				.Then<TViewModel>((vm) => Reveal(vm))
				.Catch((e) =>
				{
					DebugLog.Error("Unable to reval '{0}'. {1}", typeof(TViewModel).Name, e.Message);
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
	}
}
