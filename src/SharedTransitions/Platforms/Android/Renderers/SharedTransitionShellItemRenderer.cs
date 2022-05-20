﻿using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Android.OS;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Plugin.SharedTransitions.Platforms.Android.Extensions;

using View = Android.Views.View;
#if __ANDROID_29__
using AndroidX.Fragment.App;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;
using SupportTransitions = AndroidX.Transitions;
#else
using Android.Support.V4.App;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using SupportTransitions = Android.Support.Transitions;
#endif

namespace Plugin.SharedTransitions.Platforms.Android
{
	public class SharedTransitionShellItemRenderer: ShellItemRenderer, ITransitionRenderer
	{
		public FragmentManager SupportFragmentManager { get; set; }
		public string SelectedGroup { get; set; }
		public BackgroundAnimation BackgroundAnimation { get; set; }
		public bool IsInTabbedPage { get; set; } = false;

		/// <summary>
		/// Track the page we need to get the custom properties for the shared transitions
		/// </summary>
		Page _propertiesContainer;
		bool _isPush;
		public Page PropertiesContainer
		{
			get => _propertiesContainer;
			set
			{
				if (_propertiesContainer == value)
					return;

				//container has a different value from the one we are passing.
				//We need to unsubscribe event, set the new value, then resubscribe for the new container
				if (_propertiesContainer != null)
					_propertiesContainer.PropertyChanged -= PropertiesContainerOnPropertyChanged;

				_propertiesContainer = value;

				if (_propertiesContainer != null)
				{
					_propertiesContainer.PropertyChanged += PropertiesContainerOnPropertyChanged;
					UpdateBackgroundTransition();
					UpdateTransitionDuration();
					UpdateSelectedGroup();
				}
			}
		}
		public Page LastPageInStack { get; set; }
		public ITransitionMapper TransitionMap { get; set; }

		ShellSection _oldShellSection;
		int _transitionDuration;

		/// <summary>
		/// Apply the custom transition in context
		/// </summary>
		public SupportTransitions.Transition InflateTransitionInContext()
		{
			return SupportTransitions.TransitionInflater.From(Context)
			                         .InflateTransition(Resource.Transition.navigation_transition)
			                         .SetDuration(_transitionDuration)
			                         .AddListener(new NavigationTransitionListener(this));
		}

		bool _popToRoot;
		NavigationTransition _navigationTransition;
		readonly IShellContext _shellContext;

		public SharedTransitionShellItemRenderer(IShellContext shellContext) : base(shellContext)
		{
			_shellContext = shellContext;
			TransitionMap = ((ISharedTransitionContainer) shellContext.Shell).TransitionMap;
			_navigationTransition = new NavigationTransition(this);
		}

		protected override void SetupAnimation(ShellNavigationSource navSource, FragmentTransaction t, Page page)
		{
			if (_popToRoot || Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
			{
				base.SetupAnimation(navSource, t, page);
			}
			else
			{
				LastPageInStack = page;
				_navigationTransition.SetupPageTransition(t,navSource == ShellNavigationSource.Push);
			}
		}

		protected override void OnDisplayedPageChanged(Page newPage, Page oldPage)
		{
			base.OnDisplayedPageChanged(newPage, oldPage);
			if (ShellSection != null && ShellSection.Stack.Count == 1 && (_oldShellSection == null || _oldShellSection != ShellSection))
			{
				_oldShellSection = ShellSection;
				PropertiesContainer = newPage;
			}
		}

		protected override IShellObservableFragment CreateFragmentForPage(Page page)
		{
			var fragment = base.CreateFragmentForPage(page);
			fragment.Fragment.SharedElementEnterTransition = InflateTransitionInContext();
			return fragment;
		}

		protected override async void OnNavigationRequested(object sender, NavigationRequestedEventArgs e)
		{
			_isPush = e.RequestType == NavigationRequestType.Push;
			SupportFragmentManager ??= ChildFragmentManager;
			PropertiesContainer    =   ((IShellContentController) ShellSection.CurrentItem).Page;

			if (e.RequestType == NavigationRequestType.PopToRoot)
			{
				//Check if is a "true" PopToRoot or only a GotoAsync("../") to the first page
				var shell = (SharedTransitionShell)_shellContext.Shell;
				if (shell.LastNavigating.Location.ToString() == "..")
				{
					//This is not a PopToRoot!!! execute animation
					e.Animated    = true;
				}
				else
				{
					FixNotAnimatedPop(PropertiesContainer);
					_popToRoot = true;
				}
			}
			else if (e.RequestType == NavigationRequestType.Pop && !e.Animated)
			{
				FixNotAnimatedPop(PropertiesContainer);
			}
			else if (e.RequestType == NavigationRequestType.Push)
			{
				/*
				 * IMPORTANT!
				 *
				 * Fix for TransitionGroup selected with binding (ONLY if we have a transition with groups registered)
				 * The binding system is a bit too slow and the Group Property get valorized after the navigation occours
				 * I dont know how to solve this in an elegant way. If we set the value directly in the page it may works
				 * After a lot of test it seems that with Task.Yield we have basicaly the same performance as without
				 * This add no more than 5ms to the navigation i think is largely acceptable
				 */
				var mapStack = TransitionMap.GetMap(PropertiesContainer, null, true);
				if (mapStack?.Count > 0 && mapStack.Any(x=>!string.IsNullOrEmpty(x.TransitionGroup)))
					await Task.Yield();
			}

			base.OnNavigationRequested(sender, e);

			if (_popToRoot)
				_popToRoot = false;
		}

		void PropertiesContainerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == SharedTransitionShell.BackgroundAnimationProperty.PropertyName)
			{
				UpdateBackgroundTransition();
			}
			else if (e.PropertyName == SharedTransitionShell.TransitionDurationProperty.PropertyName)
			{
				UpdateTransitionDuration();
			}
			else if (e.PropertyName == SharedTransitionShell.TransitionSelectedGroupProperty.PropertyName)
			{
				UpdateSelectedGroup();
			}
		}

		void UpdateBackgroundTransition()
		{
			BackgroundAnimation = SharedTransitionShell.GetBackgroundAnimation(PropertiesContainer);
		}

		void UpdateTransitionDuration()
		{
			_transitionDuration = (int)SharedTransitionShell.GetTransitionDuration(PropertiesContainer);
		}

		void UpdateSelectedGroup()
		{
			SelectedGroup = SharedTransitionShell.GetTransitionSelectedGroup(PropertiesContainer);
		}

		public void SharedTransitionStarted()
		{
			((ISharedTransitionContainer) _shellContext.Shell).SendTransitionStarted(TransitionArgs());
		}

		public void SharedTransitionEnded()
		{
			((ISharedTransitionContainer) _shellContext.Shell).SendTransitionEnded(TransitionArgs());
		}

		public void SharedTransitionCancelled()
		{
			((ISharedTransitionContainer) _shellContext.Shell).SendTransitionCancelled(TransitionArgs());
		}

		/*
		 * Without animation, we need Detach->Attach to recreate the fragment ui
		 * Because wh are using SetReorderingAllowed  that cause mess when popping without animation or PopToRoot
		 * NOTE: we don't use "remove" here so we can maintain the state of the root view
		 */
		public void FixNotAnimatedPop(Page page)
		{
			var mapper = TransitionMap.TransitionStack.FirstOrDefault(x => x.Page == page);
			if (mapper?.Transitions != null)
			{
				var fromView          = (View) mapper.Transitions.FirstOrDefault()?.NativeView.Target;
				var fragmentToDisplay = fromView?.ParentFragment(SupportFragmentManager);

				if (fragmentToDisplay != null)
				{
					var transaction = SupportFragmentManager.BeginTransaction();
					transaction.Detach(fragmentToDisplay);
					transaction.Attach(fragmentToDisplay);
					transaction.CommitAllowingStateLoss();
				}
			}
		}

		SharedTransitionEventArgs TransitionArgs()
		{
			if (_isPush)
			{
				return new SharedTransitionEventArgs
				{
					PageFrom     = PropertiesContainer,
					PageTo       = LastPageInStack,
					NavOperation = NavOperation.Push
				};
			}

			return new SharedTransitionEventArgs
			{
				PageFrom     = LastPageInStack,
				PageTo       = PropertiesContainer,
				NavOperation = NavOperation.Pop
			};
		}
	}
}
