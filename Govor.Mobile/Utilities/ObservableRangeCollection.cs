using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Govor.Mobile.Utilities;

public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public ObservableRangeCollection() : base(){ }
    public ObservableRangeCollection(IEnumerable<T> items) : base(items){ }
    
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null || !items.Any()) return;

        CheckReentrancy();

        var index = Count;
        var list = items.ToList();

        foreach (var item in list)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add, list, index));
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        if (items == null)
            return;

        var list = items.ToList();
        if (list.Count == 0)
            return;

        if (index < 0 || index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        CheckReentrancy();

        for (int i = 0; i < list.Count; i++)
        {
            Items.Insert(index + i, list[i]);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                list,
                index));
    }
    
    public void ReplaceAll(IEnumerable<T> items)
    {
        CheckReentrancy();
        Items.Clear();
        AddRange(items);
    }
}