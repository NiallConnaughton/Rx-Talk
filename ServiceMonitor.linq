<Query Kind="Program">
  <NuGetReference>Rx-Main</NuGetReference>
  <Namespace>System</Namespace>
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
	// Uses the console to simulate heartbeats arriving from two servers, and displays
	// a stream of their combined Up/Down state.

	var consoleObservable = GetConsoleObservable();
	consoleObservable.Connect();
		
	var service1Heartbeats = consoleObservable.Where(s => s == "1");
	var service2Heartbeats = consoleObservable.Where(s => s == "2");

	var service1Status = GetStatusStream("Service1", service1Heartbeats);
	var service2Status = GetStatusStream("Service2", service2Heartbeats);

	service1Status
		.CombineLatest(service2Status, (s1, s2) => new { Service1 = s1.State, Service2 = s2.State })
		.Dump();
}

IObservable<ServiceUpdate> GetStatusStream(string serviceName, IObservable<string> sourceHeartBeats)
{
	var heartbeatTolerance = TimeSpan.FromSeconds(10);
	
	var down = Observable.Return(new ServiceUpdate { State = ServiceState.Down, ServiceName = serviceName })
						.Delay(heartbeatTolerance)
						.Do(_ => (serviceName + " missed heartbeat!").Dump());

	var heartbeats = sourceHeartBeats
							.Do(_ => (serviceName + " heartbeat!").Dump())
							.Select(_ => new ServiceUpdate { ServiceName = serviceName, State = ServiceState.Up });
	
	var statusStream = heartbeats
							.Merge(down)  // This was Amb in the talk, but Merge is a more typical approach
							.Take(1)
							.Repeat()
							.DistinctUntilChanged();
	
	return statusStream;
}

class ServiceUpdate
{
	public ServiceState State { get; set; }
	public string ServiceName { get; set; }
	
	public override bool Equals(object other)
	{
		if (other is ServiceUpdate)
		{
			var su = (ServiceUpdate) other;
			return su.State == State && su.ServiceName == ServiceName;
		}
		
		return false;
	}
}

enum ServiceState { Up, Down }

IConnectableObservable<string> GetConsoleObservable()
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
	.Publish();
}