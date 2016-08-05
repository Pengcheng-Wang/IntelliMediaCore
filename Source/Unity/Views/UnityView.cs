//---------------------------------------------------------------------------------------
// Copyright 2016 North Carolina State University
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
	public abstract class UnityView : MonoBehaviour, IView
	{
		private OnceEvent<IView> revealedEvent = new OnceEvent<IView>();
		public OnceEvent<IView> RevealedEvent { get { return revealedEvent; }}
		private OnceEvent<IView> hiddenEvent = new OnceEvent<IView>();
		public OnceEvent<IView> HiddenEvent { get { return hiddenEvent; }}

		public bool modal;
		public bool destroyOnHide;

		private bool IsInitialized { get; set; }

		public abstract Type ViewModelType { get; }

		public readonly BindableProperty<ViewModel> ViewModelProperty = new BindableProperty<ViewModel>();	
		public ViewModel BindingContext 
		{ 
			get { return ViewModelProperty.Value; }
			set 
			{
				if (!IsInitialized)
				{
					OnInitialize();
					IsInitialized = true;
				}
				ViewModelProperty.Value = value; 
			}
		}

		protected virtual void OnBindingContextChanged(ViewModel oldViewModel, ViewModel newViewModel)
		{
			DebugLog.Info("UnityView.OnBindingContextChanged: view='{0}' old='{1}' new='{2}'",
				this.name,
				(oldViewModel != null ? oldViewModel.GetType().Name : "null"),
				(newViewModel != null ? newViewModel.GetType().Name : "null"));

		}	

		protected virtual void OnInitialize()
		{
			DebugLog.Info("UnityView.OnInitialize: view='{0}'", this.name);
			ViewModelProperty.ValueChanged += OnBindingContextChanged;
		}	

		public virtual void OnDestroy()
		{
			DebugLog.Info("UnityView.OnDestroy: view='{0}'", this.name);
			if (BindingContext.IsRevealed)
			{
				OnHidden();
			}
			BindingContext = null;
			ViewModelProperty.ValueChanged = null;
		}

		public IAsyncTask Reveal(bool immediate = false)
		{
			DebugLog.Info("UnityView.Reveal: {0} (immediate={1})", this.name, immediate);
			return new AsyncTask((onCompleted, onError) =>
			{
				RevealedEvent.EventTriggered += (view) => onCompleted(view);

				OnAppearing();
				if (immediate || BindingContext.IsRevealed)
				{
					OnVisible();
				}
				else
				{
					StartAnimatedReveal();
				}
			});
		}

		public IAsyncTask Hide(bool immediate = false)
		{
			DebugLog.Info("UnityView.Hide: {0} (immediate={1})", this.name, immediate);
			return new AsyncTask((onCompleted, onError) =>
			{
				HiddenEvent.EventTriggered += (view) => onCompleted(view);

				OnDisappearing();
				if (immediate || !BindingContext.IsRevealed)
				{
					OnHidden();
				}
				else
				{
					StartAnimatedHide();
				}
			});
		}

		protected virtual void StartAnimatedReveal()
		{
			DebugLog.Info("UnityView.StartAnimatedReveal: {0}", this.name);
			GetComponent<Animator>().SetTrigger("Show");
		}

		protected virtual void StartAnimatedHide()
		{
			DebugLog.Info("UnityView.StartAnimatedHide: {0}", this.name);
			GetComponent<Animator>().SetTrigger("Hide");
		}

		public virtual void OnAppearing()
		{
			gameObject.SetActive(true);
			BindingContext.OnStartReveal();
		}

		public virtual void OnVisible()
		{
			BindingContext.OnFinishReveal();
			RevealedEvent.Trigger(this);
		}

		public virtual void OnDisappearing()
		{
			BindingContext.OnStartHide();
		}

		public virtual void OnHidden()
		{
			gameObject.SetActive(false);
			BindingContext.OnFinishHide();
			HiddenEvent.Trigger(this);
			if (destroyOnHide)
			{
				Destroy(this.gameObject);
			}
		}			
	}
}