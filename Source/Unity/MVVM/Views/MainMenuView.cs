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
using System.Linq;
using IntelliMedia.Models;
using IntelliMedia.ViewModels;
using IntelliMedia.Utilities;

namespace IntelliMedia.Views
{
	[ViewDescriptor(typeof(MainMenu))]
	public class MainMenuView : UnityGuiView<MainMenu>
	{
		public Text usernameLabel;
		public Transform buttonPanel;

		[Serializable]
		public class ActivityIcon
		{
			public string uri;
			public Sprite icon;
		}
		public ActivityIcon[] activityIcons;        

		protected override void OnBindingContextChanged(ViewModel oldViewModel, ViewModel newViewModel)
		{
			Contract.PropertyNotNull("buttonPanel", buttonPanel);

			MainMenu oldMainMenuViewModel = oldViewModel as MainMenu;
			if (oldMainMenuViewModel != null)
			{
				oldMainMenuViewModel.ActivitiesProperty.ValueChanged -= ActivitiesChanged;
			}

			if (ViewModel != null)
			{
				ViewModel.ActivitiesProperty.ValueChanged += ActivitiesChanged;
			}

			base.OnBindingContextChanged(oldViewModel, newViewModel);
			
			UpdateControls();
		}

		private void ActivitiesChanged(List<Activity> oldActivties, List<Activity> newActivities)
		{
			UpdateControls();
		}
		
		private void UpdateControls()
		{
			if (ViewModel == null || ViewModel.Activities == null)
			{
				return;
			}

			if (usernameLabel != null)
			{
				usernameLabel.text = ViewModel.Username;
			}

			// Use existing button as a prefab for creating new buttons
			Transform buttonPrefab = (buttonPanel.childCount > 0 ? buttonPanel.GetChild (0) : null);
			if (buttonPrefab == null) 
			{
				throw new System.Exception ("ButtonRow in AlertView prefab is missing button object");
			}
			
			// Remove any additional buttons in the row
			for (int index = 1; index < buttonPanel.childCount; ++index) 
			{
				Destroy (buttonPanel.GetChild (index).gameObject);
			}
			
			buttonPanel.DetachChildren();
			for (int index = 0; index < ViewModel.Activities.Count; ++index) 
			{
				Activity currentActivity = ViewModel.Activities[index];
				// Use local variable to ensure the correct value is passed lambda function below
				string buttonLabel = currentActivity.Name;
				Transform newButton = GameObject.Instantiate(buttonPrefab);
				newButton.name = buttonLabel + "Button";
				newButton.GetComponent<Button>().onClick.AddListener (() => OnClicked(currentActivity));
				newButton.GetComponentInChildren<Text>().text = buttonLabel;
				Transform iconObject = newButton.transform.Find("Panel/IconImage");
				if (iconObject != null)
				{
					Sprite iconSprite = GetIconForActivity(currentActivity);
					if (iconSprite != null)
					{
						iconObject.GetComponent<Image>().sprite = iconSprite;
					}
				}
				newButton.transform.SetParent(buttonPanel);
			}
			
			Destroy (buttonPrefab.gameObject);
		}

		private Sprite GetIconForActivity(Activity activity)
		{
			return activityIcons.Where(i => activity.Uri.StartsWith(i.uri)).Select(i => i.icon).First();
		}
		
		public void OnClicked(Activity activity)
		{
			ViewModel.StartActivity(activity);
		}
		public void SignOut()
		{
			ViewModel.SignOut();
		}

//        public void OnSettingsButtonClicked()
//        {
//            ViewModel.Settings();
//        }
	}
}