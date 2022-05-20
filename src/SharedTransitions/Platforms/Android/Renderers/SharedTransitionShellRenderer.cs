using Android.Content;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Plugin.SharedTransitions;
using Plugin.SharedTransitions.Platforms.Android;

[assembly:ExportRenderer(typeof(SharedTransitionShell), typeof(SharedTransitionShellRenderer))]
namespace Plugin.SharedTransitions.Platforms.Android
{
	public class SharedTransitionShellRenderer : ShellRenderer
	{
		public SharedTransitionShellRenderer(Context context) : base(context)
		{
		}

		protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem)
		{
			return new SharedTransitionShellItemRenderer(this);
		}

	}
}
