void Main()
{
	// Generation
	EnumerableX.RandomInt32().Take(10).Dump("Ten RandomInt32s");
	
	EnumerableX.FromValues(1,2,3,4).Dump("From a sequence");

	Func<IEnumerable<int>, IEnumerable<int>> positive =
								source => source.EmptyIfNull().Where(@int => @int > 0);
								
	int count = positive(null).Count().Dump("Number of elements in a null");

	// Concatenation
	EnumerableX.FromValues(0, 1, 2, 3, 4).Join(-137).Dump("Join1");
	EnumerableX.FromValues(0, 1, 2).Join(EnumerableX.FromValues(10, 11, 12)).Dump("Join2");
	EnumerableX.FromValues(0, 1, 2).Append(10, 11, 12).Dump("Join3");
	EnumerableX.FromValues(0, 1, 2).Prepend(10, 11, 12).Dump("Join4");

	var integers1 = Enumerable.Range(0, 5).Append(-1).Prepend(-2,-3,-4).Dump("integers1");
	var integers2 = (-33).PrependTo(integers1).Dump("integers2");
	
	// Partitioning
	integers2.Subsequence(1,3).Dump("integers2.Subsequence");

	// Comparison
	EnumerableX.FromValues(0, 1, 2, 2, 3).OrderBy(i => i, (k1, k2) => k1 - k2).Dump("Order by k1-k2");
	EnumerableX.FromValues(0, 1, 2, 2, 3).OrderBy(i => i, (k1, k2) => k2 - k1).Dump("Order by k2-k1");
}



// "Heavily influenced" by https://weblogs.asp.net/dixin/understanding-linq-to-objects-8-more-useful-queries
public static class EnumerableX
{
	#region Generation
	// from a factory
	public static IEnumerable<TResult> Create<TResult>(Func<TResult> valueFactory, int? count = null)
	{
		if (count == null)
		{
			while (true)
			{
				yield return valueFactory();
			}
		}

		for (int index = 0; index < count; index++)
		{
			yield return valueFactory();
		}
	}

	// from a value, a few values, a null
	public static IEnumerable<TResult> FromValue<TResult>(TResult value)
	{
		yield return value;
	}

	public static IEnumerable<TResult> FromValues<TResult>(params TResult[] values) => values;

	public static IEnumerable<TSource> EmptyIfNull<TSource>
		(this IEnumerable<TSource> source) => source ?? Enumerable.Empty<TSource>();

	#region Examples

	public static IEnumerable<int> RandomInt32(int? seed = null, int min = int.MinValue, int max = int.MaxValue)
	{
		Random random = new Random(seed ?? Environment.TickCount);
		return Create(() => random.Next(min, max));
	}

	public static IEnumerable<double> RandomDouble(int? seed = null)
	{
		Random random = new Random(seed ?? Environment.TickCount);
		return Create(random.NextDouble);
	}


	#endregion

	#endregion
	
	#region Concatenation
	// string-like Join with a single separator
	public static IEnumerable<TSource> Join<TSource>(this IEnumerable<TSource> source, TSource separator)
	{
	    foreach(var element in source)
	    {
            yield return element;
            yield return separator;
		}
	}

	// string-like Join with a separator which is an enumerable
	public static IEnumerable<TSource> Join<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> separator)
	{
		separator = separator ?? Enumerable.Empty<TSource>();
		foreach (var element in source)
		{
			yield return element;
			foreach (TSource value in separator)
			{
				yield return value;
			}
		}
	}
	
	// append/prepend a few elements
	public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, params TSource[] append)
	{
		return source.Concat(append);
	}

	public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, params TSource[] prepend)
	{
		return prepend.Concat(source);
	}

	// AppendTo/PrependTo a single element (inspired from jQuery):
	public static IEnumerable<TSource> AppendTo<TSource>(this TSource append, IEnumerable<TSource> source)
	{
		return source.Append(append);
	}

	public static IEnumerable<TSource> PrependTo<TSource>(this TSource prepend, IEnumerable<TSource> source)
	{
		return source.Prepend(prepend);
	}
	#endregion

	#region Partitioning
	public static IEnumerable<TSource> Subsequence<TSource>
		(this IEnumerable<TSource> source, int startIndex, int count)
	{
		return source.Skip(startIndex).Take(count);
	}
	#endregion

	#region Comparison
	public class ComparerWrapper<T> : IComparer<T>
	{
		private readonly Func<T, T, int> compare;

		public ComparerWrapper(Func<T, T, int> compare)
		{
			this.compare = compare;
		}

		public int Compare(T x, T y) => this.compare(x, y);
	}

	public class EqualityComparerWrapper<T> : IEqualityComparer<T>
	{
		private readonly Func<T, T, bool> equality;
		private readonly Func<T, int> getHashCode;

		public EqualityComparerWrapper(Func<T, T, bool> equality, Func<T, int> getHashCode = null)
		{
			this.equality = equality;
			this.getHashCode = getHashCode ?? (value => value.GetHashCode());
		}

		public bool Equals(T x, T y) => this.equality(x, y);

		public int GetHashCode(T obj) => this.getHashCode(obj);
	}	
	
	public static IComparer<T> Comparer<T>(this Func<T, T, int> compare) => new EnumerableX.ComparerWrapper<T>(compare);

	public static IEqualityComparer<T> Comparer<T>
		(this Func<T, T, bool> equality,
		Func<T, int> getHashCode = null) => new EnumerableX.EqualityComparerWrapper<T>(equality, getHashCode);

	public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TKey, TKey, int> compare) => source.OrderBy(keySelector, compare.Comparer());

	public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TKey, TKey, int> compare) => source.OrderByDescending(keySelector, compare.Comparer());

	public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(
		this IOrderedEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TKey, TKey, int> compare) => source.ThenBy(keySelector, compare.Comparer());

	public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(
		this IOrderedEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TKey, TKey, int> compare) => source.ThenByDescending(keySelector, compare.Comparer());

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TSource, TElement> elementSelector,
		Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
		Func<TKey, TKey, bool> equality,
		Func<TKey, int> getHashCode = null)
		=> source.GroupBy(keySelector, elementSelector, resultSelector, equality.Comparer(getHashCode));

	public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
		this IEnumerable<TOuter> outer,
		IEnumerable<TInner> inner,
		Func<TOuter, TKey> outerKeySelector,
		Func<TInner, TKey> innerKeySelector,
		Func<TOuter, TInner, TResult> resultSelector,
		Func<TKey, TKey, bool> equality,
		Func<TKey, int> getHashCode = null)
		=> outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector, equality.Comparer(getHashCode));

	public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
		this IEnumerable<TOuter> outer,
		IEnumerable<TInner> inner,
		Func<TOuter, TKey> outerKeySelector,
		Func<TInner, TKey> innerKeySelector,
		Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
		Func<TKey, TKey, bool> equality,
		Func<TKey, int> getHashCode = null)
		=>
			outer.GroupJoin(
				inner,
				outerKeySelector,
				innerKeySelector,
				resultSelector,
				equality.Comparer(getHashCode));

	public static IEnumerable<TSource> Distinct<TSource>(
		this IEnumerable<TSource> source,
		Func<TSource, TSource, bool> equality,
		Func<TSource, int> getHashCode = null) => source.Distinct(equality.Comparer(getHashCode));

	public static IEnumerable<TSource> Union<TSource>(
		this IEnumerable<TSource> first,
		IEnumerable<TSource> second,
		Func<TSource, TSource, bool> equality,
		Func<TSource, int> getHashCode = null) => first.Union(second, equality.Comparer(getHashCode));

	public static IEnumerable<TSource> Intersect<TSource>(
		this IEnumerable<TSource> first,
		IEnumerable<TSource> second,
		Func<TSource, TSource, bool> equality,
		Func<TSource, int> getHashCode = null) => first.Intersect(second, equality.Comparer(getHashCode));

	public static IEnumerable<TSource> Except<TSource>(
		this IEnumerable<TSource> first,
		IEnumerable<TSource> second,
		Func<TSource, TSource, bool> equality,
		Func<TSource, int> getHashCode = null) => first.Except(second, equality.Comparer(getHashCode));
	#endregion
}

