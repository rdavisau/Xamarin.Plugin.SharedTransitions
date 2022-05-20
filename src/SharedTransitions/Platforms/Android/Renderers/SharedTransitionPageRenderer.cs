using System;
using Android.Content;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Plugin.SharedTransitions.Platforms.Android;

[assembly: ExportRenderer(typeof(Page), typeof(SharedTransitionPageRenderer))]
namespace Plugin.SharedTransitions.Platforms.Android
{
	public class SharedTransitionPageRenderer : PageRenderer
	{
		public SharedTransitionPageRenderer(Context context) : base(context)
		{
			
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (Element != null)
				{
					if (Application.Current.MainPage is ISharedTransitionContainer shellPage)
					{
						shellPage.TransitionMap.RemoveFromPage(Element);
					}

					if (Element.Parent is ISharedTransitionContainer navPage)
					{
						navPage.TransitionMap.RemoveFromPage(Element);
					}
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e);
			}

			base.Dispose(disposing);
		}
	}
}
