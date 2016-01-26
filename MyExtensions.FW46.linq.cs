// comes from %USERPROFILE%\Documents\LINQPad Plugins\Framework 4.6\MyExtensions.FW46.linq

void Main()
{
	EnumerableX.RandomInt32().Take(10).Dump("Ten RandomInt32s");
	
	EnumerableX.FromValues(1,2,3,4).Dump("From a sequence");

	Func<IEnumerable<int>, IEnumerable<int>> positive =
								source => source.EmptyIfNull().Where(@int => @int > 0);
								
	int count = positive(null).Count().Dump("Number of elements in a null");
}



public static class EnumerableX
{
	// "Heavily influenced" by https://weblogs.asp.net/dixin/understanding-linq-to-objects-8-more-useful-queries
	
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
}
