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
		private List<TheatreInstaller> installers = new List<TheatreInstaller>();

		private HashSet<IView> revealedViews = new HashSet<IView>();

		public void Register(TheatreInstaller installer)
		{
			installers.Add(installer);
		}

		public void Unregister(TheatreInstaller installer)
		{
			installers.Remove(installer);
		}

		private ViewModel.Factory ViewModelFactory { get { return installers[0].ViewModelFactory; }}
		private ViewFactory ViewFactory { get { return installers[0].ViewFactory; }}

		public void Transition(ViewModel from, ViewModel to)
		{
			Contract.ArgumentNotNull("from", from);
			Contract.ArgumentNotNull("to", to);

			DebugLog.Info("StageManager.Transition: {0} -> {1}", from.GetType().Name, to.GetType().Name);

			Hide(from, (IView view) =>
			{
				Reveal(to).Start();
			});
		}

		public void Transition(ViewModel from, Type toViewModelType)
		{
			Contract.ArgumentNotNull("toViewModelType", toViewModelType);

			Transition(from, ViewModelFactory.Resolve(toViewModelType));
		}

		public AsyncTask Reveal(ViewModel vm, VisibilityEvent.OnceEventHandler handler = null)
		{
			Contract.ArgumentNotNull("vm", vm);

			return new AsyncTask((prevResult, onCompleted, onError) =>
			{
				try
				{
					DebugLog.Info("StageManager.Reveal: {0}", vm.GetType().Name);

					IView view = revealedViews.FirstOrDefault(v => v.BindingContext == vm);
					if (view == null)
					{
						view = ViewFactory.Resolve(vm);
						view.BindingContext = vm;
						revealedViews.Add(view);
						view.Reveal(false, (IView revealedView) =>
		                {
							onCompleted(revealedView.BindingContext);
		                });
		            }
				}
				catch(Exception e)
				{
					DebugLog.Error("Unable to reveal '{0}'. {1}", vm.GetType().Name, e.Message);
					onError(e);
				}
			});
		}			

		public AsyncTask Reveal<TViewModel>(Action<TViewModel> setStateAction = null) where TViewModel : ViewModel
		{
			TViewModel vm = ViewModelFactory.Resolve<TViewModel>(setStateAction);

			return Reveal(vm);
		}

		public AsyncTask Reveal(string className)
		{
			Contract.ArgumentNotNull("className", className);

			ViewModel vm = ViewModelFactory.Resolve(TypeFinder.ClassNameToType(className));

			return Reveal(vm);
		}

		public ViewModel Hide(ViewModel vm, VisibilityEvent.OnceEventHandler handler = null)
		{
			Contract.ArgumentNotNull("vm", vm);

			DebugLog.Info("StageManager.Hide: {0}", vm.GetType().Name);

			IView view = revealedViews.FirstOrDefault(v => v.BindingContext == vm);
			
			if (view != null)
			{			
				view.Hide(false, handler);
				revealedViews.Remove(view);
			}
			
			return vm;
		}

		public TViewModel Hide<TViewModel>(Action<TViewModel> setStateAction = null) where TViewModel : ViewModel
		{
			return (TViewModel)Hide(ViewModelFactory.Resolve<TViewModel>(setStateAction));
		}

		public bool IsRevealed<TViewModel>() where TViewModel : ViewModel
		{
			TViewModel vm = ViewModelFactory.Resolve<TViewModel>();
			IView view = revealedViews.FirstOrDefault(v => v.BindingContext == vm);
			if (view != null && !vm.IsRevealed)
			{
				/*
				throw new Exception(String.Format("Stage mismatch: '{0}' is in revealed list, but {1}.IsRevealed=false",
				                                  view.GetType().Name,
				                                  typeof(TViewModel).Name));
				                                  */
			} 

			return view != null;
		}
	}
}
