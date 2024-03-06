namespace SpacetimeDB
{
    public interface IReducerArgsBase : BSATN.IStructuralReadWrite
    {
        public string ReducerName { get; }
    }

    public abstract class ReducerEventBase
    {
        public string ReducerName { get; }
        public ulong Timestamp { get; }
        public SpacetimeDB.Identity Identity { get; }
        public SpacetimeDB.Address? CallerAddress { get; }
        public string ErrMessage { get; }
        public ClientApi.Event.Types.Status Status { get; }
        public IReducerArgsBase Args;

        public ReducerEventBase(ClientApi.Event dbEvent, IReducerArgsBase args)
        {
            ReducerName = dbEvent.FunctionCall.Reducer;
            Timestamp = dbEvent.Timestamp;
            if (dbEvent.CallerIdentity != null)
            {
                Identity = Identity.From(dbEvent.CallerIdentity.ToByteArray());
            }

            if (dbEvent.CallerAddress != null)
            {
                CallerAddress = Address.From(dbEvent.CallerAddress.ToByteArray());
            }

            ErrMessage = dbEvent.Message;
            Status = dbEvent.Status;
            Args = args;
        }
    }
}
