using System;
using System.Diagnostics;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Plugin.SharedTransitions.Platforms.iOS.Renderers;

[assembly: ExportRenderer(typeof(Page), typeof(SharedTransitionPageRenderer))]
namespace Plugin.SharedTransitions.Platforms.iOS.Renderers
{
	public class SharedTransitionPageRenderer : PageRenderer
	{
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (Element != null)
				{
					if (Application.Current.MainPage is ISharedTransitionContainer shellPage)
					{
						shellPage.TransitionMap.RemoveFromPage((Page)Element);
					}
					if (Element.Parent is ISharedTransitionContainer navPage)
					{
						navPage.TransitionMap.RemoveFromPage((Page)Element);
					}
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}

			base.Dispose(disposing);
		}
	}
}
