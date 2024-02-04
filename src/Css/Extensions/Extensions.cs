using System;
using System.Collections.Generic;

namespace Css.Extensions;


public static partial class ReadOnlyListExtensions
{
    public static bool ContainsAtWordStart(this string str, string subject) =>
        str.IndexOf(subject, StringComparison.CurrentCultureIgnoreCase) is int idx and > -1 && (idx == 0 || !Char.IsLetter(str[idx - 1]));

    public static ReadOnlyListEnumerable<T> AsValueEnumerable<T>(this IReadOnlyList<T> source) => new(source);

    public static ListSelectEnumerable<T, TResult> SelectValue<T, TResult>(this IReadOnlyList<T> source, Func<T, TResult> selector) => new(source, selector);

    public static ListWhereEnumerable<T> WhereValue<T>(this IReadOnlyList<T> source, Func<T, bool> predicate) => new(source, predicate);


    public readonly ref struct ReadOnlyListEnumerable<T>(IReadOnlyList<T> source)
    {
        public Enumerator GetEnumerator() => new(source);

        public ref struct Enumerator
        {
            internal Enumerator(IReadOnlyList<T> source)
            {
                Current = default!;
                enumerator = source;
                index = -1;
            }

            private readonly IReadOnlyList<T> enumerator;

            public T Current { get; private set; }

            private int index;

            public bool MoveNext()
            {
                if (++index < enumerator.Count)
                {
                    Current = enumerator[index];
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose() { }
        }
    }

    public readonly ref struct ListEnumerable<T>(IList<T> source)
    {
        public Enumerator GetEnumerator() => new(source);

        public ref struct Enumerator
        {
            internal Enumerator(IList<T> source)
            {
                Current = default!;
                enumerator = source;
                index = -1;
            }

            private readonly IList<T> enumerator;

            public T Current { get; private set; }

            private int index;

            public bool MoveNext()
            {
                if (++index < enumerator.Count)
                {
                    Current = enumerator[index];
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose() { }
        }
    }

    public readonly ref struct ListSelectEnumerable<T, TResult>
    {
        public ListSelectEnumerable(IReadOnlyList<T> source, Func<T, TResult> selector)
        {
            this.source = source;
            this.selector = selector;
        }

        private readonly IReadOnlyList<T> source;
        private readonly Func<T, TResult> selector;

        public Enumerator GetEnumerator() => new(source, selector);

        public ref struct Enumerator
        {
            internal Enumerator(IReadOnlyList<T> source, Func<T, TResult> selector)
            {
                Current = default!;
                enumerator = source;
                this.selector = selector;
                index = -1;
            }

            private readonly IReadOnlyList<T> enumerator;
            private readonly Func<T, TResult> selector;

            public TResult Current { get; private set; }

            private int index;

            public bool MoveNext()
            {
                if (++index < enumerator.Count)
                {
                    Current = selector(enumerator[index]);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose() { }
        }
    }

    public readonly ref struct ListWhereEnumerable<T>
    {
        public ListWhereEnumerable(IReadOnlyList<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        private readonly IReadOnlyList<T> source;
        private readonly Func<T, bool> predicate;

        public Enumerator GetEnumerator() => new(source, predicate);

        public ref struct Enumerator
        {
            internal Enumerator(IReadOnlyList<T> source, Func<T, bool> predicate)
            {
                Current = default!;
                enumerator = source;
                this.predicate = predicate;
                index = -1;
            }

            private readonly IReadOnlyList<T> enumerator;
            private readonly Func<T, bool> predicate;

            public T Current { get; private set; }

            private int index;

            public bool MoveNext()
            {
                index++;

                while (index < enumerator.Count)
                {
                    var item = enumerator[index];
                    if (predicate(item))
                    {
                        Current = item;
                        return true;
                    }

                    index++;
                }

                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose() { }
        }
    }

}