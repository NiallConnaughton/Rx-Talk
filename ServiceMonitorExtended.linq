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
	
	// This is a modification of the code in the demo that handles heartbeats for any number of servers
	// and produces lists of the state of all servers each time any of the servers change state.
	
	// It also shows how you could define events that represent all your dependent services being up,
	// and any of your dependent services being down. This could be used to delay handling new requests
	// at startup until all dependencies are up, and then to stop handling new requests when dependencies are down.
	
	// This can allow a system to dynamically stop and resume processing when it sees its dependencies go down and
	// come back up. This is very useful for designing tolerant systems that don't need to be bounced when their
	// dependencies crash/restart, and to avoid having to have a specific order and timing of starting related services.

	var consoleObservable = GetConsoleObservable();
	consoleObservable.Connect();

	var services = new[] { "1", "2", "3" };
	
	var combinedStatuses = services.Select(serviceName => GetStatusStream(serviceName, consoleObservable))
								   .CombineLatest()
								   .Publish().RefCount();

	// Subscribe to updates where all services are up - this is our signal to begin accepting connections/orders/transactions
	combinedStatuses.Where(serviceStatuses => serviceStatuses.All(s => s.State == ServiceState.Up))
					.Subscribe(_ => "All services are up, we're open for business!".Dump());
					
	// Subscribe to events where any service is down - this is our signal to stop accepting connections/orders/transactions
	// First, from each update of statuses for all servers, select the names of servers that are down in that snapshot
	// Now we have a stream of a list of names of servers that are down at any given point. When that list has at least
	// one entry, then at least one server is down.
	combinedStatuses.Select(serviceStatuses => serviceStatuses.Where(s => s.State == ServiceState.Down)
															  .Select(s => s.ServiceName))
					.Where(downServices => downServices.Any())
					.Subscribe(downServices => string.Format("Closed for business because servers ({0}) are down",
															 string.Join(", ", downServices)).Dump());

	combinedStatuses.Dump();
}

IObservable<ServiceUpdate> GetStatusStream(string serviceName, IObservable<string> allHeartbeats)
{
	var displayName = "Service " + serviceName;
	var serviceHeartbeats = allHeartbeats.Where(hb => hb == serviceName);
	
	var heartbeatTolerance = TimeSpan.FromSeconds(10);
	
	var down = Observable.Return(new ServiceUpdate { State = ServiceState.Down, ServiceName = serviceName })
						.Delay(heartbeatTolerance);
//						.Do(_ => (displayName + " missed heartbeat!").Dump());

	var heartbeats = serviceHeartbeats
//							.Do(_ => (displayName + " heartbeat!").Dump())
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