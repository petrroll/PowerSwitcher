using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Petrroll.Helpers
{

    public static class ObservableLINQShim
    {
        public static ObservableCollectionWhereShim<C, T> WhereObservable<C, T>(this C collection, Func<T, bool> predicate) where C : INotifyCollectionChanged, ICollection<T> where T : INotifyPropertyChanged
        {
            return new ObservableCollectionWhereShim<C, T>(collection, predicate);
        }
    }

    public class ObservableCollectionWhereShim<C, T> : INotifyCollectionChanged, ICollection<T> where C : ICollection<T>, INotifyCollectionChanged where T : INotifyPropertyChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Func<T, bool> Predicate { get; private set; }
        public C BaseCollection { get; private set; }

        private int numberOfItems;
        private IEnumerable<T> filteredCollection => BaseCollection.Where(Predicate);

        public ObservableCollectionWhereShim(C baseCollection, Func<T, bool> predicate)
        {
            Predicate = predicate;
            BaseCollection = baseCollection;

            numberOfItems = filteredCollection.Count();

            baseCollection.CollectionChanged += Obs_CollectionChanged;
            baseCollection.ForEach(sch => sch.PropertyChanged += Sch_PropertyChanged);
        }

        private void Sch_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            int oldNumberOfItems = numberOfItems;
            int newNumberOfItems = filteredCollection.Count();

            numberOfItems = newNumberOfItems;
            if (oldNumberOfItems < newNumberOfItems)
            {
                var changeIndex = BaseCollection.Take(BaseCollection.IndexOf(sender)).Count(Predicate);
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, sender, changeIndex));
            }
            else if (oldNumberOfItems > newNumberOfItems)
            {
                var changeIndex = BaseCollection.Take(BaseCollection.IndexOf(sender)).Count(Predicate);
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, sender, changeIndex));
            }
        }

        private void Obs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            numberOfItems = filteredCollection.Count();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    handleAdd(e);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    handleRemove(e);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    handleReplace(e);
                    break;
                case NotifyCollectionChangedAction.Move:
                    handleMove(e);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    handleReset();
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (e.NewItems != null) { e.NewItems.ForEach(sch => ((T)sch).PropertyChanged += Sch_PropertyChanged); }
            if (e.OldItems != null) { e.OldItems.ForEach(sch => ((T)sch).PropertyChanged -= Sch_PropertyChanged); }
        }

        private void handleMove(NotifyCollectionChangedEventArgs e)
        {
            var newItems = getNewItems(e);
            var delItems = getOldItems(e);

            if (newItems.Count < 1 && delItems.Count < 1) { return; }

            var newItemsIndex = getNewItemsIndex(e);
            var oldItemsIndex = getOldItemsIndex(e);

            if (newItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, delItems, oldItemsIndex)); }
            else if (delItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, newItemsIndex)); }
            else { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, newItems, newItemsIndex, oldItemsIndex)); }

        }

        private void handleReplace(NotifyCollectionChangedEventArgs e)
        {
            var newItems = getNewItems(e);
            var delItems = getOldItems(e);

            if (newItems.Count < 1 && delItems.Count < 1) { return; }

            var changeItemIndex = getNewItemsIndex(e);
            if (newItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, delItems, changeItemIndex)); }
            else if (delItems.Count < 1) { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, changeItemIndex)); }
            else { RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, delItems, changeItemIndex)); }
        }

        private void handleAdd(NotifyCollectionChangedEventArgs e)
        {
            IList newItems = getNewItems(e);
            if (newItems.Count < 1) { return; }

            var newItemsIndex = getNewItemsIndex(e);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, newItemsIndex));
        }

        private void handleRemove(NotifyCollectionChangedEventArgs e)
        {
            var delItems = getOldItems(e);
            if (delItems.Count < 1) { return; }

            var delItemsIndex = getOldItemsIndex(e);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, delItems, delItemsIndex));
        }

        private void handleReset()
        {
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }


        private IList getOldItems(NotifyCollectionChangedEventArgs e)
        {
            return e.OldItems.Cast<T>().Where(Predicate).ToList();
        }

        private int getOldItemsIndex(NotifyCollectionChangedEventArgs e)
        {
            return (e.OldStartingIndex < 0) ? e.OldStartingIndex : BaseCollection.Take(e.OldStartingIndex).Count(Predicate);
        }

        private IList getNewItems(NotifyCollectionChangedEventArgs e)
        {
            return e.NewItems.Cast<T>().Where(Predicate).ToList();
        }

        private int getNewItemsIndex(NotifyCollectionChangedEventArgs e)
        {
            return (e.NewStartingIndex < 0) ? e.NewStartingIndex : BaseCollection.Take(e.NewStartingIndex).Count(Predicate);
        }

        protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        public int Count => BaseCollection.Count(Predicate);
        public bool IsReadOnly => ((ICollection<T>)BaseCollection).IsReadOnly;

        public void Add(T item) => BaseCollection.Add(item);

        public void Clear()
        {
            BaseCollection.ForEach(sch => sch.PropertyChanged -= Sch_PropertyChanged);
            BaseCollection.Clear();
        }

        public bool Contains(T item) => filteredCollection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => filteredCollection.ToList().CopyTo(array, arrayIndex);

        public bool Remove(T item) => BaseCollection.Remove(item);

        public IEnumerator<T> GetEnumerator() => filteredCollection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => filteredCollection.GetEnumerator();
    }
}
