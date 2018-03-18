﻿// this source is derived from Livet project.
// see more detail and original source: https://github.com/ugaya40/Livet

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

// ReSharper disable CheckNamespace
namespace Livet
    // ReSharper restore CheckNamespace
{
    /// <summary>
    /// スレッドセーフな変更通知コレクションです。
    /// </summary>
    /// <typeparam name="T">コレクションアイテムの型</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
        "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [Serializable]
    public class ObservableSynchronizedCollectionEx<T> : IList<T>, ICollection, INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private readonly IList<T> _list;

        [NonSerialized]
        private readonly object _syncRoot = new object();

        [NonSerialized]
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ObservableSynchronizedCollectionEx()
        {
            _list = new List<T>();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="source">初期値となるソース</param>
        public ObservableSynchronizedCollectionEx(IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _list = new List<T>(source);
        }

        /// <summary>
        /// 指定したオブジェクトを検索し、最初に見つかった位置の 0 から始まるインデックスを返します。
        /// </summary>
        /// <param name="item">検索するオブジェクト</param>
        /// <returns>最初に見つかった位置のインデックス</returns>
        public int IndexOf(T item)
        {
            return ReadWithLockAction(() => _list.IndexOf(item));
        }

        /// <summary>
        /// 指定したインデックスの位置に要素を挿入します。
        /// </summary>
        /// <param name="index">指定するインデックス</param>
        /// <param name="item">挿入するオブジェクト</param>
        public void Insert(int index, T item)
        {
            ReadAndWriteWithLockAction(() => _list.Insert(index, item),
                () =>
                {
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    OnCollectionChanged(
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                });
        }

        /// <summary>
        /// 位置プロバイダを使ってアイテムを指定位置に挿入します。
        /// </summary>
        /// <param name="indexProvider">位置プロバイダ</param>
        /// <param name="item">挿入するアイテム</param>
        public void Insert(Func<IEnumerable<T>, int> indexProvider, T item)
        {
            ReadAndWriteWithLockAction(() =>
                    indexProvider(_list),
                i => _list.Insert(i, item),
                i =>
                {
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, item, i));
                });
        }

        /// <summary>
        /// 指定したインデックスにある要素を削除します。
        /// </summary>
        /// <param name="index">指定するインデックス</param>
        public void RemoveAt(int index)
        {
            ReadAndWriteWithLockAction(() => _list[index],
                removeItem => _list.RemoveAt(index),
                removeItem =>
                {
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        removeItem, index));
                });
        }

        public T this[int index]
        {
            get { return ReadWithLockAction(() => _list[index]); }
            set
            {
                ReadAndWriteWithLockAction(() => _list[index],
                    oldItem => { _list[index] = value; },
                    oldItem =>
                    {
                        OnPropertyChanged("Item[]");
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                            _list[index], oldItem, index));
                    });
            }
        }

        public bool TryIndexOf(int index, out T item)
        {
            var flag = false;
            item = ReadWithLockAction(() =>
            {
                if (index < _list.Count)
                {
                    flag = true;
                    return _list[index];
                }
                flag = false;
                return default(T);
            });
            return flag;
        }

        /// <summary>
        /// 末尾にオブジェクトを追加します。
        /// </summary>
        /// <param name="item">追加するオブジェクト</param>
        public void Add(T item)
        {
            ReadAndWriteWithLockAction(() => _list.Add(item),
                () =>
                {
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item,
                        _list.Count - 1));
                });
        }

        /// <summary>
        /// すべての要素を削除します。
        /// </summary>
        public void Clear()
        {
            ReadAndWriteWithLockAction(
                () => _list.Clear(),
                () =>
                {
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                });
        }

        /// <summary>
        /// ある要素がこのコレクションに含まれているかどうかを判断します。
        /// </summary>
        /// <param name="item">コレクションに含まれているか判断したい要素</param>
        /// <returns>このコレクションに含まれているかどうか</returns>
        public bool Contains(T item)
        {
            return ReadWithLockAction(() => _list.Contains(item));
        }

        /// <summary>
        /// 全体を互換性のある1次元の配列にコピーします。コピー操作は、コピー先の配列の指定したインデックスから始まります。
        /// </summary>
        /// <param name="array">コピー先の配列</param>
        /// <param name="arrayIndex">コピー先の配列のどこからコピー操作をするかのインデックス</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ReadWithLockAction(() => _list.CopyTo(array, arrayIndex));
        }

        public T[] ToArray()
        {
            return ReadWithLockAction(() => _list.ToArray());
        }

        public T[] SynchronizedToArray(Action finishHandler)
        {
            return ReadWithLockAction(() =>
            {
                var result = _list.ToArray();
                finishHandler();
                return result;
            });
        }

        public void SynchronizedToArray(Action<T[]> resultHandler)
        {
            ReadWithLockAction(() => resultHandler(_list.ToArray()));
        }

        /// <summary>
        /// 実際に格納されている要素の数を取得します。
        /// </summary>
        public int Count
        {
            get { return ReadWithLockAction(() => _list.Count); }
        }

        /// <summary>
        /// このコレクションが読み取り専用かどうかを取得します。
        /// </summary>
        public bool IsReadOnly => _list.IsReadOnly;

        /// <summary>
        /// 最初に見つかった特定のオブジェクトを削除します。
        /// </summary>
        /// <param name="item">削除したいオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        public bool Remove(T item)
        {
            var result = false;

            ReadAndWriteWithLockAction(() => _list.IndexOf(item),
                index => { result = _list.Remove(item); },
                index =>
                {
                    if (!result) return;
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    OnCollectionChanged(
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                });

            return result;
        }

        /// <summary>
        /// 指定した条件に合致するアイテムを全て除去します。
        /// </summary>
        /// <param name="predicate">除去するアイテムの条件</param>
        public void RemoveWhere(Func<T, bool> predicate)
        {
            ReadAndWriteWithLockAction(() =>
                {
                    var indexAndItems = new Dictionary<int, T>();
                    _list
                        .Select((item, i) => new { i, item })
                        .Where(i => predicate(i.item))
                        .ForEach(i => indexAndItems.Add(i.i, i.item));
                    return indexAndItems.Reverse();
                },
                indexAndItems => indexAndItems.ForEach(i => _list.RemoveAt(i.Key)),
                indexAndItems =>
                {
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    indexAndItems.ForEach(kvp =>
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove, kvp.Value, kvp.Key)));
                });
        }

        /// <summary>
        /// 指定されたインデックスの要素を指定されたインデックスに移動します。
        /// </summary>
        /// <param name="oldIndex">移動したい要素のインデックス</param>
        /// <param name="newIndex">移動先のインデックス</param>
        public void Move(int oldIndex, int newIndex)
        {
            ReadAndWriteWithLockAction(() => _list[oldIndex],
                item =>
                {
                    _list.RemoveAt(oldIndex);
                    _list.Insert(newIndex, item);
                },
                item =>
                {
                    OnPropertyChanged("Item[]");
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item,
                        newIndex, oldIndex));
                });
        }

        /// <summary>
        /// 反復処理するためのスナップショットの列挙子を返します。
        /// </summary>
        /// <returns>列挙子</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return ReadWithLockAction(() => ((IEnumerable<T>)_list.ToArray()).GetEnumerator());
        }

        /// <summary>
        /// 反復処理するためのスナップショットの列挙子を返します。
        /// </summary>
        /// <returns>列挙子</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ReadWithLockAction(() => ((IEnumerable<T>)_list.ToArray()).GetEnumerator());
        }

        /// <summary>
        /// 全体を互換性のある1次元の配列にコピーします。コピー操作は、コピー先の配列の指定したインデックスから始まります。
        /// </summary>
        /// <param name="array">コピー先の配列</param>
        /// <param name="index">コピー先の配列のどこからコピー操作をするかのインデックス</param>
        public void CopyTo(Array array, int index)
        {
            CopyTo(array.Cast<T>().ToArray(), index);
        }

        /// <summary>
        /// このコレクションがスレッドセーフであるかどうかを取得します。(常にtrueを返します)
        /// </summary>
        public bool IsSynchronized => true;

        /// <summary>
        /// このコレクションへのスレッドセーフなアクセスに使用できる同期オブジェクトを返します。
        /// </summary>
        public object SyncRoot => _syncRoot;

        /// <summary>
        /// CollectionChangedイベントを発生させます。
        /// </summary>
        /// <param name="args">NotifyCollectionChangedEventArgs</param>
        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var threadSafeHandler = Interlocked.CompareExchange(ref CollectionChanged, null, null);

            threadSafeHandler?.Invoke(this, args);
        }

        /// <summary>
        /// PropertyChangedイベントを発生させます。
        /// </summary>
        /// <param name="propertyName">変更されたプロパティの名前</param>
        protected void OnPropertyChanged(string propertyName)
        {
            var threadSafeHandler = Interlocked.CompareExchange(ref PropertyChanged, null, null);

            threadSafeHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void ReadWithLockAction(Action readAction)
        {
            if (!_lock.IsReadLockHeld)
            {
                _lock.EnterReadLock();
                try
                {
                    readAction();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                readAction();
            }
        }

        protected TResult ReadWithLockAction<TResult>(Func<TResult> readAction)
        {
            if (!_lock.IsReadLockHeld)
            {
                _lock.EnterReadLock();
                try
                {
                    return readAction();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return readAction();
        }

        protected void ReadAndWriteWithLockAction(Action writeAction, Action readAfterWriteAction)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                _lock.EnterWriteLock();
                try
                {
                    writeAction();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                _lock.EnterReadLock();

                try
                {
                    readAfterWriteAction();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        protected void ReadAndWriteWithLockAction<TResult>(Func<TResult> readBeforeWriteAction,
            Action<TResult> writeAction, Action<TResult> readAfterWriteAction)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                var readActionResult = readBeforeWriteAction();

                _lock.EnterWriteLock();

                try
                {
                    writeAction(readActionResult);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                _lock.EnterReadLock();

                try
                {
                    readAfterWriteAction(readActionResult);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// コレクションが変更された際に発生するイベントです。
        /// </summary>
        [field: NonSerialized]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// プロパティが変更された際に発生するイベントです。
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
    }
}