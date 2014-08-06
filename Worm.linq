<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Threading.Tasks.dll</Reference>
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
	// Mimics twitter users voting on PM candidates during a debate (if there ever is one).

	IObservable<string> tweets = GetTweetStream();
	
	IObservable<IList<string>> timeSlices = tweets.Buffer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
	
	var votes =
			from timeSlice in timeSlices
			let kruddVotes = timeSlice.Where(t => t.Contains("#krudd"))
			let abbottVotes = timeSlice.Where(t => t.Contains("#abbott"))
			select new { Krudd = kruddVotes.Count() / (decimal)timeSlice.Count, Abbot = abbottVotes.Count() / (decimal)timeSlice.Count };
	
	IObservable<string> formatted = votes.Select(v => string.Format("KRudd {0:P}, Abbott {1:P}", v.Krudd, v.Abbot));
	formatted.Dump();
}

IObservable<string> GetTweetStream()
{
	var hashtags = new[] { "#krudd", "#abbott" };
	var tweets = hashtags.Select(GetTweetStream).Merge().Publish().RefCount();
	
	return tweets;
}

IObservable<string> GetTweetStream(string hashtag) 
{ 
	var rng = new Random();
	var message = "<some random tweet>";
	Func<int, string> getTweet = _ => string.Format("{0} {1} {2}", string.Format("@someguy{0}", rng.Next(0, 5000)), hashtag, message);
	
	Func<int, TimeSpan> getTimeToTweet = _ => TimeSpan.FromMilliseconds(rng.Next(50, 1000));
	return Observable.Generate(0, _ => true, count => ++count, getTweet, getTimeToTweet); 
}