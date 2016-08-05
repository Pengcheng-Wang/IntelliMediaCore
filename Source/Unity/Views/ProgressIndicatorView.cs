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
using IntelliMedia;
using UnityEngine.Events;

namespace IntelliMedia
{
	[ViewDescriptor(typeof(ProgressIndicatorViewModel))]
	public class ProgressIndicatorView : UnityGuiView<ProgressIndicatorViewModel>
	{
		public Text message;
		public Button cancelButton;

		protected override void OnBindingContextChanged(ViewModel oldViewModel, ViewModel newViewModel)
		{
			Contract.PropertyNotNull("message", message);
			Contract.PropertyNotNull("cancelButton", cancelButton);

			base.OnBindingContextChanged(oldViewModel, newViewModel);
			ProgressIndicatorViewModel previousViewModel = oldViewModel as ProgressIndicatorViewModel;
			if (previousViewModel != null)
			{
				previousViewModel.ProgressUpdated -= UpdateControls;
			}

			if (ViewModel != null)
			{
				ViewModel.ProgressUpdated += UpdateControls;
			}

			UpdateControls();
		}

		public override void OnDestroy()
		{
			if (ViewModel != null)
			{
				ViewModel.ProgressUpdated -= UpdateControls;
			}

			base.OnDestroy();
		}

		private void UpdateControls()
		{
			if (ViewModel != null && ViewModel.CurrentProgress != null && !string.IsNullOrEmpty(ViewModel.CurrentProgress.Message))
			{
				message.text = ViewModel.CurrentProgress.Message;
			}
		}

		public void OnClicked()
		{
			ViewModel.Cancel();	
		}
	}
}