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

        public static ObservableCollectionWhereSwitchableShim<C, T> WhereObservableSwitchable<C, T>(this C collection, Func<T, bool> predicate, bool filterOn) where C : INotifyCollectionChanged, ICollection<T> where T : INotifyPropertyChanged
        {
            return new ObservableCollectionWhereSwitchableShim<C, T>(collection, predicate, filterOn);
        }
    }

    public class ObservableCollectionWhereSwitchableShim<C, T> : ObservableCollectionWhereShim<C, T>, IEnumerable<T> where C : INotifyCollectionChanged, ICollection<T> where T : INotifyPropertyChanged
    {
        protected IEnumerable<T> currentlySwitchedCollection => (FilterOn) ? filteredCollection : BaseCollection;

        #region Constructor
        public ObservableCollectionWhereSwitchableShim(C baseCollection, Func<T, bool> predicate, bool filterOn) : base(baseCollection, predicate)
        {
            this.filterOn = filterOn;
        }
        #endregion

        #region CollectionAndElementChanged
        protected override void Obs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (filterOn) { base.Obs_CollectionChanged(sender, e); }
            else { updateNumberOfFilteredItems(); RaiseCollectionChanged(e); }
        }

        protected override void Sch_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (filterOn) { base.Sch_PropertyChanged(sender, e); }
            else { updateNumberOfFilteredItems(); }
        }
        #endregion

        #region WhereSwitch
        private bool filterOn;
        public bool FilterOn
        {
            get { return filterOn; }
            set
            {
                if(value == filterOn) { return; }
                filterOn = value;

                if (filterOn) { removeFilteredElements();  }
                else { addFilteredElements(); }
            }
        }

        private void removeFilteredElements()
        {
            int indexInFiltered = 0;
            foreach(var element in BaseCollection)
            {
                if (Predicate(element)) { indexInFiltered++; }
                else
                {
                    #if DEBUG
                    Console.WriteLine($"RF:Removed:{element}");
                    #endif
                    RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T> { element }, indexInFiltered));
                }
            }
        }

        private void addFilteredElements()
        {
            int indexInNotFiltered = 0;
            foreach (var element in BaseCollection)
            {
                if (!Predicate(element))
                {
                    #if DEBUG
                    Console.WriteLine($"AF:Added:{element}");
                    #endif
                    RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T> { element }, indexInNotFiltered));
                }
                indexInNotFiltered++;
            }
        }
        #endregion

        #region OtherMethods
        public override int Count => currentlySwitchedCollection.Count();
        public override bool Contains(T item) => currentlySwitchedCollection.Contains(item);
        public override void CopyTo(T[] array, int arrayIndex) => currentlySwitchedCollection.ToList().CopyTo(array, arrayIndex);

        public override IEnumerator<T> GetEnumerator() => currentlySwitchedCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => currentlySwitchedCollection.GetEnumerator();
        #endregion
    }

    public class ObservableCollectionWhereShim<C, T> : INotifyCollectionChanged, ICollection<T> where C : ICollection<T>, INotifyCollectionChanged where T : INotifyPropertyChanged
    {
        #region Variables
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Func<T, bool> Predicate { get; private set; }
        public C BaseCollection { get; private set; }

        protected int numberOfFilteredItems;
        protected IEnumerable<T> filteredCollection => BaseCollection.Where(Predicate);
        #endregion

        #region Constructor
        public ObservableCollectionWhereShim(C baseCollection, Func<T, bool> predicate)
        {
            Predicate = predicate;
            BaseCollection = baseCollection;

            numberOfFilteredItems = filteredCollection.Count();

            baseCollection.CollectionChanged += Obs_CollectionChanged;
            baseCollection.ForEach(sch => sch.PropertyChanged += Sch_PropertyChanged);
        }
        #endregion

        #region ChangedEvents
        protected virtual void Sch_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            int oldNumberOfItems = numberOfFilteredItems;
            updateNumberOfFilteredItems();

            if (oldNumberOfItems < numberOfFilteredItems)
            {
                #if DEBUG
                Console.WriteLine($"AC:Added:{sender}");
                #endif
                var changeIndex = BaseCollection.Take(BaseCollection.IndexOf(sender)).Count(Predicate);
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, sender, changeIndex));
            }
            else if (oldNumberOfItems > numberOfFilteredItems)
            {
                #if DEBUG
                Console.WriteLine($"DC:Added:{sender}");
                #endif
                var changeIndex = BaseCollection.Take(BaseCollection.IndexOf(sender)).Count(Predicate);
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, sender, changeIndex));
            }
        }

        protected virtual void Obs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            updateNumberOfFilteredItems();
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

        protected void updateNumberOfFilteredItems() => numberOfFilteredItems = filteredCollection.Count();
        #endregion

        #region HandleOperations
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
        #endregion

        #region GetNewAndOldElements
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
        #endregion

        #region RaiseCollection
        protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }
        #endregion

        #region OtherMethods
        public virtual int Count => filteredCollection.Count();
        public bool IsReadOnly => ((ICollection<T>)BaseCollection).IsReadOnly;

        public void Add(T item) => BaseCollection.Add(item);

        public void Clear()
        {
            BaseCollection.ForEach(sch => sch.PropertyChanged -= Sch_PropertyChanged);
            BaseCollection.Clear();
        }

        public bool Remove(T item) => BaseCollection.Remove(item);

        public virtual bool Contains(T item) => filteredCollection.Contains(item);

        public virtual void CopyTo(T[] array, int arrayIndex) => filteredCollection.ToList().CopyTo(array, arrayIndex);

        public virtual IEnumerator<T> GetEnumerator() => filteredCollection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => filteredCollection.GetEnumerator();
        #endregion
    }
}
