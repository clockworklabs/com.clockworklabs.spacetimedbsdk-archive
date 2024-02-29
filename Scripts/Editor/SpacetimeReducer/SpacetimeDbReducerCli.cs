using System;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Debug = UnityEngine.Debug;

namespace SpacetimeDB.Editor
{
    /// CLI action middleware between ReducerWindow and SpacetimeDbCli 
    /// Vanilla: Do the action -> return the result -> no more.
    /// (!) Didn't find what you were looking for here? Check `SpacetimeDbCli.cs`
    public static class SpacetimeDbReducerCli
    {
        #region Static Options
        private const SpacetimeDbCli.CliLogLevel REDUCER_CLI_LOG_LEVEL = SpacetimeDbCli.CliLogLevel.Info;
        #endregion // Static Options
        
        
        #region High Level CLI Actions
        
        #endregion // High Level CLI Actions
    }
}