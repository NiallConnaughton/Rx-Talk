<Query Kind="Program">
  <NuGetReference>Rx-Main</NuGetReference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Joins</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.PlatformServices</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Reactive.Threading.Tasks</Namespace>
</Query>

void Main()
{
	// Gathers order amounts (typed into the console) into buffers of 5 seconds, 1 second apart,
	// to give rolling 5 second stats on orders (average, total, count, max).
	// With a rich order/transaction model, you could extend this to extract information like
		// The most active customer in a time window
		// The largest volume from a customer in a window
		// Sudden changes in levels of activity - overall or by customer, type of order, etc
		// Fraud detection (eg more orders in a period than humanly possible to enter, lots of small orders, etc)
		// Increases/decreases in load (to scale up/down resources)
		// Correlations with other event sources, eg:
			// how many extra hits does my Masterchef fan page get while Masterchef is airing
			// how much does my volume of fx deals increase during +/- 5 minutes around interest rate announcements
			// how much uplift does my tv advertising or promotional tweet give me in the first few minutes

	var orders = GetConsoleObservable()
					.SelectMany(ConvertDecimal); 

	var orderStats = orders.Buffer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1))
							.Where(orderBuffer => orderBuffer.Any())
							.Select(orderBuffer => new
												{
													Count = orderBuffer.Count,
													Biggest = orderBuffer.Max(),
													Average = orderBuffer.Average(),
													Total = orderBuffer.Sum(),
													Orders = orderBuffer
												});
//	orderStats.Dump();

	var condensed = orderStats.Select(stats => string.Format("Count: {0}, Biggest: {1}, Average: {2:0.##}, Total: {3}", stats.Count, stats.Biggest, stats.Average, stats.Total));
	condensed.Dump();
}

IObservable<decimal> ConvertDecimal(string input)
{
	decimal orderValue;
	if (decimal.TryParse(input, out orderValue))
		return Observable.Return(orderValue);
		
	return Observable.Empty<decimal>();
}

IObservable<string> GetConsoleObservable()
{
	return Observable.Create<string>(observer =>
	{
		var cancel = false;
		
		while (!cancel)
		{
			observer.OnNext(Console.ReadLine());
		}
		
		return Disposable.Create(() => cancel = true);
	})
	.SubscribeOn(ThreadPoolScheduler.Instance)
	.Publish()
	.RefCount();
}