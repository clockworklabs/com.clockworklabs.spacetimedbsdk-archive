using System.Diagnostics.CodeAnalysis;

namespace SpacetimeDB;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

public class NetworkRequestTracker
{
    private readonly ConcurrentQueue<(DateTime, TimeSpan, object)> _requestDurations = new ConcurrentQueue<(DateTime, TimeSpan, object)>();
    private uint nextRequestId;
    private Dictionary<uint, (DateTime, object)> requests = new Dictionary<uint, (DateTime, object)>();

    public uint StartTrackingRequest(object metadata = null)
    {
        // Record the start time of the request
        var newRequestId = ++nextRequestId;
        requests[newRequestId] = (DateTime.UtcNow, metadata);
        return newRequestId;
    }

    public bool FinishTrackingRequest(uint requestId)
    {
        if (!requests.Remove(requestId, out var entry))
        {
            
            return false;
        }
        
        // Calculate the duration and add it to the queue
        var endTime = DateTime.UtcNow;
        var duration = endTime - entry.Item1;
        _requestDurations.Enqueue((endTime, duration, entry.Item2));
        return true;
    }
    
    public void InsertRequest(DateTime timestamp, TimeSpan duration, object metadata)
    {
        _requestDurations.Enqueue((timestamp, duration, metadata));
    }

    public ((TimeSpan, object), (TimeSpan, object)) GetMinMaxTimes(int lastMinutes)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-lastMinutes);

        if (!_requestDurations.Where(x => x.Item1 >= cutoff).Select(x => (x.Item2, x.Item3)).Any())
        {
            return ((TimeSpan.Zero, null), (TimeSpan.Zero, null));
        }

        var min = _requestDurations.Where(x => x.Item1 >= cutoff).Select(x => (x.Item2, x.Item3)).Min();
        var max = _requestDurations.Where(x => x.Item1 >= cutoff).Select(x => (x.Item2, x.Item3)).Max();

        return (min, max);
    }
}


public class Stats
{
    public NetworkRequestTracker ReducerRequestTracker = new NetworkRequestTracker();
    public NetworkRequestTracker OneOffRequestTracker = new NetworkRequestTracker();
    public NetworkRequestTracker SubscriptionRequestTracker = new NetworkRequestTracker();
    public NetworkRequestTracker RemoteRequestTracker = new NetworkRequestTracker();
    public NetworkRequestTracker ParseMessageTracker = new NetworkRequestTracker();
}