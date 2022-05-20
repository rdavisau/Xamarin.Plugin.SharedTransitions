﻿using System.ComponentModel;
using Microsoft.Maui.Controls.Platform;
using Plugin.SharedTransitions;

[assembly: ResolutionGroupName(Transition.ResolutionGroupName)]
[assembly: ExportEffect(typeof(Plugin.SharedTransitions.Platforms.iOS.TransitionEffect), Transition.EffectName)]

namespace Plugin.SharedTransitions.Platforms.iOS
{
    public class TransitionEffect : PlatformEffect
    {
        private Page _currentPage;
        private Element _currentElement;

        protected override void OnAttached()
        {
	        if (Application.Current.MainPage is Shell appShell)
	        {
		        _currentPage = appShell.GetCurrentShellPage();
		        UpdateTag();
	        }
	        else
	        {
		        FindContainerPageAndUpdateTag(Element);
	        }
        }

        private void FindContainerPageAndUpdateTag(Element element)
        {
	        _currentElement = element;
	        var parent = _currentElement.Parent;
	        if (parent != null && parent is Page page)
	        {
		        _currentPage = page;
		        UpdateTag();
	        }
	        else if (parent != null)
	        {
		        FindContainerPageAndUpdateTag(parent);
	        }
	        else if (_currentPage == null)
	        {
		        _currentElement.PropertyChanged += CurrentElementOnPropertyChanged;
	        }
        }

        protected void CurrentElementOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
	        if (args.PropertyName == "Parent")
	        {
		        _currentElement.PropertyChanged -= CurrentElementOnPropertyChanged;
		        FindContainerPageAndUpdateTag(_currentElement);
	        }
        }

        protected override void OnDetached()
        {
            if (Element is View element)
                Transition.RemoveTransition(element,_currentPage);
        }

        protected override void OnElementPropertyChanged(PropertyChangedEventArgs args)
        {
	        //Always check the LightSnapshopProperty in case it gets changed later by a binding
	        if ((args.PropertyName == Transition.NameProperty.PropertyName && Transition.GetName(Element) != null) ||
	            (args.PropertyName == Transition.GroupProperty.PropertyName && Transition.GetGroup(Element) != null) ||
	            (args.PropertyName == Transition.LightSnapshotProperty.PropertyName && Transition.GetLightSnapshot(Element) != null))
	        {
		        UpdateTag();
	        }

	        base.OnElementPropertyChanged(args);
        }

        /// <summary>
        /// Update the shared transition name and/or group
        /// </summary>
        void UpdateTag()
        {
            if (Element is View element && _currentPage != null)
            {
                if (Control != null)
                {
                    Transition.RegisterTransition(element, Control, _currentPage);
                } 
                else if (Container != null)
                {
                    Transition.RegisterTransition(element, Container, _currentPage);
                }
            }
        }
    }
}
