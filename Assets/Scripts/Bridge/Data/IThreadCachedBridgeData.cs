namespace Simulator.Bridge.Data
{
    /// <summary>
    /// <para>
    /// Interface that indicates bridge data types that should be cached in thread-exclusive object until conversion
    /// is finished. 
    /// </para>
    /// <para>
    /// Use this if data contains heap-allocated objects that are reused across subsequent messages. Conversion is
    /// asynchronous, which means that main thread could possibly rewrite data before or during conversion. If cache is
    /// used, message is guaranteed to contain data that was present at the moment of queueing.
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IThreadCachedBridgeData<in T> where T : class, new()
    {
        /// <summary>
        /// Perform a deep copy of this object into provided target cache object.
        /// </summary>
        /// <param name="target">Pooled object that will be used as a cache.</param>
        void CopyToCache(T target);

        /// <summary>
        /// <para>Returns hash that groups instances into compatible sub-pools.</para>
        /// </summary>
        int GetHash();
    }
}