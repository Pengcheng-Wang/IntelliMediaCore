﻿//---------------------------------------------------------------------------------------
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
using IntelliMedia.ViewModels;
using IntelliMedia.Utilities;

namespace IntelliMedia.Views
{
	[RequireComponent(typeof(Animator), typeof(CanvasGroup))]
	public abstract class UnityGuiView<TViewModel> : UnityView where TViewModel:ViewModel
    {
		protected readonly PropertyBinder<TViewModel> Binder = new PropertyBinder<TViewModel>();
				  
		public override Type ViewModelType { get { return typeof(TViewModel); }}
		public TViewModel ViewModel { get { return (TViewModel)BindingContext; }}

		protected override void OnBindingContextChanged (ViewModel oldViewModel, ViewModel newViewModel)
		{
			base.OnBindingContextChanged (oldViewModel, newViewModel);

			Binder.Unbind((TViewModel)BindingContext);
			Binder.Bind(ViewModel);
		}
			
		protected override void StartAnimatedReveal()
		{
			GetComponent<Animator>().SetTrigger("Show");
		}

		protected override void StartAnimatedHide()
		{
			GetComponent<Animator>().SetTrigger("Hide");
		}
	}
}