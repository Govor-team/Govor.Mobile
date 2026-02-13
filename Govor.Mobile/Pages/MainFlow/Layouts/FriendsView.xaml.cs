using CommunityToolkit.Maui.Core.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Govor.Mobile.Pages.MainFlow.Layouts;

public partial class FriendsView : ContentView
{
    public FriendsView()
    {
        InitializeComponent();
    }
    
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView cv)
            cv.SelectedItem = null;

        FriendSearchEntry.HideKeyboardAsync(CancellationToken.None);
    }
}