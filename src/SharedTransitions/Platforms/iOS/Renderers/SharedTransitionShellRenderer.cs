using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Plugin.SharedTransitions;
using Plugin.SharedTransitions.Platforms.iOS;

[assembly:ExportRenderer(typeof(SharedTransitionShell), typeof(SharedTransitionShellRenderer))]
namespace Plugin.SharedTransitions.Platforms.iOS
{
	public class SharedTransitionShellRenderer : ShellRenderer
	{
		protected override IShellSectionRenderer CreateShellSectionRenderer(ShellSection shellSection)
		{
			return new SharedTransitionShellSectionRenderer(this);
		}
	}
}
