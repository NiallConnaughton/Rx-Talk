Rx-Talk
=======

In 2013, I gave a talk at Sydney's Alt.Net user group on the .Net Reactive Extensions framework. Learning Rx from scratch takes a considerable amount of time, and there are already good resources on the net to help people get started (especially http://introtorx.com). So rather than teaching the code from scratch, the aim of the talk was to try to get people thinking about how Rx could apply to their domain, and how they it can be used to model complex domain related events. 

To that end, I used a few examples to demonstrate handling events on the fly, such as:

* Keeping track of the up/down state of services you're dependent on so you can react when they go down or come back up
* A couple of examples that show aggregating data over windows of time, eg:
  * Tweets from voters in favour of politicians in a live debate to see in realtime which politician is leading
  * Orders from customers in real time to see average, total, count, etc in 5 second windows
  
There is also an extended version of the ServiceMonitor example that allows you to monitor an arbitrary number of services.

**The examples use LinqPad**. If you don't have LinqPad, grab it, it's a great (free) tool for learning Rx (along with other things).
