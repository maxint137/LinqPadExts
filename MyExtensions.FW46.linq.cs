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

	// Statistics
	
	
	// Quantifiers
	EnumerableX.FromValues(0, 1, 2, 2, 3).ContainsAny(EnumerableX.FromValues(0, 11, 12, 13)).Dump("Quantifiers");

	// Iteration
	EnumerableX.FromValues(0, 1, 2, 3).ForEach((i) => { i.Dump("Iteration"); return i < 2; });
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

	#region Returns a new collection - Comparison
	
	public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TSource, TElement> elementSelector,
		Func<TKey, TKey, bool> equality,
		Func<TKey, int> getHashCode = null) =>
			source.ToDictionary(keySelector, elementSelector, equality.Comparer(getHashCode));

	public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TSource, TElement> elementSelector,
		Func<TKey, TKey, bool> equality,
		Func<TKey, int> getHashCode = null) =>
			source.ToLookup(keySelector, elementSelector, equality.Comparer(getHashCode));

	#endregion

	public static int IndexOf<TSource>(
		this IEnumerable<TSource> source,
		TSource search,
		IEqualityComparer<TSource> comparer = null,
		int startIndex = 0,
		int? count = null)
	{
		comparer = comparer ?? EqualityComparer<TSource>.Default;
		source = source.Skip(startIndex);
		if (count != null)
		{
			source = source.Take(count.Value);
		}

		int index = checked(-1 + startIndex);
		foreach (TSource value in source)
		{
			index = checked(index + 1);
			if (comparer.Equals(value, search))
			{
				return index;
			}
		}

		return -1;
	}
	
	#region Statistics
	public static double VariancePopulation<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
		double[] keys = source.Select(key => keySelector(key).ToDouble(formatProvider)).ToArray();
		double mean = keys.Average();
		return keys.Sum(key => (key - mean) * (key - mean)) / keys.Length;
	}

	public static double VarianceSample<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
		double[] keys = source.Select(key => keySelector(key).ToDouble(formatProvider)).ToArray();
		double mean = keys.Average();
		return keys.Sum(key => (key - mean) * (key - mean)) / (keys.Length - 1);
	}

	public static double Variance<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
		return source.VarianceSample(keySelector, formatProvider);
	}

	// Excel STDEV.P/STDEV.S/STDEV functions:
	public static double StandardDeviationPopulation<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
		// Excel STDEV.P function:
		// https://support.office.com/en-us/article/STDEV-P-function-6e917c05-31a0-496f-ade7-4f4e7462f285
		return Math.Sqrt(source.VariancePopulation(keySelector, formatProvider));
	}

	public static double StandardDeviationSample<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
		// Excel STDEV.S function:
		// https://support.office.com/en-us/article/STDEV-S-function-7d69cf97-0c1f-4acf-be27-f3e83904cc23
		return Math.Sqrt(source.VarianceSample(keySelector, formatProvider));
	}

	public static double StandardDeviation<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
		// Excel STDDEV.P function:
		// https://support.office.com/en-us/article/STDEV-function-51fecaaa-231e-4bbb-9230-33650a72c9b0
		return Math.Sqrt(source.Variance(keySelector, formatProvider));
	}

	// Excel PERCENTILE.EXC/PERCENTILE.INC/PERCENTILE functions:
	public static double PercentileExclusive<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		double percentile,
		IComparer<TKey> comparer = null,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
//		Contract.Requires<ArgumentNullException>(source != null);
//		Contract.Requires<ArgumentNullException>(keySelector != null);
//		Contract.Requires<ArgumentOutOfRangeException>(
//			percentile >= 0 && percentile <= 1,
//			"percentile must be in the range of 1 / source.Count() and 1 - 1 / (source.Count() + 1).");

		// Excel PERCENTILE.EXC function:
		// https://support.office.com/en-us/article/PERCENTILE-EXC-function-bbaa7204-e9e1-4010-85bf-c31dc5dce4ba
		comparer = comparer ?? System.Collections.Generic.Comparer<TKey>.Default;
		
		TKey[] orderedKeys = source.Select(keySelector).OrderBy(key => key, comparer).ToArray();
		int length = orderedKeys.Length;
		if (percentile < (double)1 / length || percentile > 1 - (double)1 / (length + 1))
		{
			throw new ArgumentOutOfRangeException(
				nameof(percentile),
				$"{nameof(percentile)} must be in the range of 1/source.Count() and 1 - 1 / source.Count().");
		}

		double index = percentile * (length + 1) - 1;
		int integerComponentOfIndex = (int)index;
		double decimalComponentOfIndex = index - integerComponentOfIndex;
		double keyAtIndex = orderedKeys[integerComponentOfIndex].ToDouble(formatProvider);

		double keyAtNextIndex = orderedKeys[integerComponentOfIndex + 1].ToDouble(formatProvider);
		return keyAtIndex + (keyAtNextIndex - keyAtIndex) * decimalComponentOfIndex;
	}

	public static double PercentileInclusive<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		double percentile,
		IComparer<TKey> comparer = null,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
//		Contract.Requires<ArgumentNullException>(source != null);
//		Contract.Requires<ArgumentNullException>(keySelector != null);
//		Contract.Requires<ArgumentOutOfRangeException>(
//			percentile >= 0 && percentile <= 1, "percentile must be between 0 and 1.");

		// Excel PERCENTILE.INC function:
		// https://support.office.com/en-us/article/PERCENTILE-INC-Function-DAX-15f69af8-1588-4863-9acf-2acc00384ffd
		comparer = comparer ?? System.Collections.Generic.Comparer<TKey>.Default;
		TKey[] orderedKeys = source.Select(keySelector).OrderBy(key => key, comparer).ToArray();
		int length = orderedKeys.Length;

		double index = percentile * (length - 1);
		int integerComponentOfIndex = (int)index;
		double decimalComponentOfIndex = index - integerComponentOfIndex;
		double keyAtIndex = orderedKeys[integerComponentOfIndex].ToDouble(formatProvider);

		if (integerComponentOfIndex >= length - 1)
		{
			return keyAtIndex;
		}

		double keyAtNextIndex = orderedKeys[integerComponentOfIndex + 1].ToDouble(formatProvider);
		return keyAtIndex + (keyAtNextIndex - keyAtIndex) * decimalComponentOfIndex;
	}

	public static double Percentile<TSource, TKey>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		double percentile,
		IComparer<TKey> comparer = null,
		IFormatProvider formatProvider = null)
		where TKey : IConvertible
	{
//		Contract.Requires<ArgumentNullException>(source != null);
//		Contract.Requires<ArgumentNullException>(keySelector != null);
//		Contract.Requires<ArgumentOutOfRangeException>(
//			percentile >= 0 && percentile <= 1, "percentile must be between 0 and 1.");

		// Excel PERCENTILE function:
		// https://support.office.com/en-us/article/PERCENTILE-function-91b43a53-543c-4708-93de-d626debdddca
		// https://en.wikipedia.org/wiki/Percentile#Definition_of_the_Microsoft_Excel_method
		return PercentileInclusive(source, keySelector, percentile, comparer, formatProvider);
	}
	#endregion


	#region Quantifiers
	
	public static bool IsNullOrEmpty<TSource>(this IEnumerable<TSource> source) => source == null || !source.Any();

	public static bool IsNotNullOrEmpty<TSource>(this IEnumerable<TSource> source) => source != null && source.Any();

	public static bool ContainsAny<TSource>(
							this IEnumerable<TSource> source,
							IEnumerable<TSource> values,
							IEqualityComparer<TSource> comparer = null)
							{
								return source.Any(value => values.Contains(value, comparer));
							}
	#endregion


	#region Iteration

	public static void ForEach<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> onNext)
	{
		foreach (TSource value in source)
		{
			if (!onNext(value))
			{
				break;
			}
		}
	}

	public static void ForEach<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> onNext)
	{
		int index = 0;
		foreach (TSource value in source)
		{
			if (!onNext(value, index))
			{
				break;
			}

			index = checked(index + 1); // Not checked in the source code.
		}
	}

	public static void ForEach<TSource>(this IEnumerable<TSource> source)
	{
		using (IEnumerator<TSource> iterator = source.GetEnumerator())
		{
			while (iterator.MoveNext())
			{
			}
		}
	}
	#endregion
}
