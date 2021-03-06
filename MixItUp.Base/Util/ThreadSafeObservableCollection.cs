﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.Util
{
    public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
    {
        public ThreadSafeObservableCollection() { }

        public ThreadSafeObservableCollection(IEnumerable<T> collection) : base(collection) { }

        public ThreadSafeObservableCollection(List<T> list) : base(list) { }

        public new void Add(T item)
        {
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                base.Add(item);
            });
        }

        public new void Clear()
        {
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                base.Clear();
            });
        }

        public new IEnumerator<T> GetEnumerator()
        {
            IEnumerator<T> result = null;
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                result = base.GetEnumerator();
            });
            return result;
        }

        public new int IndexOf(T item)
        {
            int index = -1;
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                index = base.IndexOf(item);
            });
            return index;
        }

        public new void Insert(int index, T item)
        {
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                base.Insert(index, item);
            });
        }

        public new bool Remove(T item)
        {
            bool result = false;
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                result = base.Remove(item);
            });
            return result;
        }

        public void AddRange(IEnumerable<T> range)
        {
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                this.AddRangeInternal(range);
            });
        }

        public void ClearAndAddRange(IEnumerable<T> range)
        {
            DispatcherHelper.Dispatcher.Invoke(() =>
            {
                base.Clear();
                this.AddRangeInternal(range);
            });
        }

        private void AddRangeInternal(IEnumerable<T> range)
        {
            if (range != null)
            {
                foreach (var item in range.ToList())
                {
                    base.Add(item);
                }
            }

            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }
    }
}
