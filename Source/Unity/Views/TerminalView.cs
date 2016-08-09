//---------------------------------------------------------------------------------------
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
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace IntelliMedia
{
	[ViewDescriptor(typeof(Terminal), "Text")]
	public class TerminalView : UnityGuiView<Terminal> 
	{
		public bool debugEchoText;
		public Text output;
		public InputField input;
		public Image backgroundPanel;
		public ScrollRect scrollArea;

		public Color background = Color.black;
		public Color font = Color.green;

		public override void Awake()
		{
			base.Awake();

			Contract.PropertyNotNull("output", output);
			Contract.PropertyNotNull("input", input);
			Contract.PropertyNotNull("backgroundPanel", backgroundPanel);

			backgroundPanel.color = background;
			foreach(Text textComponent in GetComponentsInChildren<Text>())
			{
				textComponent.color = font;
			}

			Scrollbar scrollBar = GetComponentInChildren<Scrollbar>();
			if (scrollBar != null)
			{
				scrollBar.GetComponent<Image>().color = font/2;
				scrollBar.handleRect.GetComponent<Image>().color = font * 0.75f;
			}				
		}
			
		protected override void OnInitialize()
		{
			base.OnInitialize();

			DebugLog.Info("TerminalView.OnInitialize: view='{0}'", this.name);

			// Verify all GUI controls have been assigned in the Unity Editor
			Contract.PropertyNotNull("inputField", input);
			input.interactable = false;

			DebugLog.Info("TerminalView.OnInitialize: bind properties");

			// Bind ViewModel properties to OnChanged methods
			Binder.Add<string>("HistoryProperty", OnHistoryChanged);
			Binder.Add<bool>("InteractableProperty", OnInteractableChanged);
		}

		public override void OnVisible ()
		{
			base.OnVisible();

			if (debugEchoText)
			{
				GetInput();
			}
		}

		private void GetInput()
		{
			ViewModel.ReadLine().Start((line) =>
			{				
				ViewModel.WriteLine(line as string);
				GetInput();
			});
		}

		private void OnHistoryChanged(string oldValue, string newValue)
		{
			output.text = newValue.TrimEnd('\n');
			scrollArea.verticalNormalizedPosition = 0;
		}
			
		private void OnInteractableChanged(bool oldValue, bool newValue)
		{
			StartCoroutine(SetInputInteractable());
		}

		private IEnumerator SetInputInteractable()
		{
			yield return new WaitForEndOfFrame();

			input.interactable = ViewModel.InteractableProperty.Value;
			FocusOnInputField();
		}
			
		public void OnSubmit()
		{
			ViewModel.InputProperty.Value = input.text;
			input.text = "";
		}

		public void FocusOnInputField()
		{
			if (!input.isFocused)
			{
				input.ActivateInputField();		
			}
		}
	}
}
