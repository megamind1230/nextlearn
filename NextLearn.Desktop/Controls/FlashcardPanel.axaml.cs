using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace NextLearn.Desktop.Controls;

public partial class FlashcardPanel : UserControl
{
    public FlashcardPanel()
    {
        InitializeComponent();
    }

    public void FocusSearch()
    {
        DispatcherTimer.RunOnce(() => FlashcardSearchBox?.Focus(), TimeSpan.FromMilliseconds(50));
    }

    public void ScrollBy(double dx, double dy)
    {
        var sv = this.FindControl<ScrollViewer>("FlashcardScrollViewer");
        if (sv is not null)
        {
            sv.Offset = new Vector(sv.Offset.X + dx, sv.Offset.Y + dy);
        }
    }
}
