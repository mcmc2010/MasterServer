

namespace AMToolkits
{
    public enum FactoryObjectStatus
    {
        None = 0x00,
        Start = 0x01,
        Running = 0x02,
        Completed = 0x08,
        Release = 0x10,
    }

    public interface IFactoryObject : System.IDisposable
    {
        long Index { get; }
        bool IsRunning { get; }
        void Initialize(object[] args);

        /// <summary>
        /// 注意，该方法必须简单。这个是线程访问
        /// </summary>
        /// <param name="index"></param>
        void _SetIndex(long index);
    }


    public interface IFactory : System.IDisposable
    {
        void Initialize();
    }

    /// <summary>
    /// 
    /// </summary>
    public class SchedulingFactory<T> : IFactory
                        where T : IFactoryObject, new()
    {
        protected static SchedulingFactory<T>? _instance = null;
        public static TT DefaultInstance<TT>() where TT : SchedulingFactory<T>, new()
        {
            if (_instance == null)
            {
                _instance = CreateFactory<TT>();
            }
            return(TT)_instance;
        }

        private long _factory_index = 0;

        private readonly List<T> _schedule_pool = new List<T>();
        private readonly object _locked = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TT"></typeparam>
        /// <returns></returns>
        public static TT CreateFactory<TT>() where TT : SchedulingFactory<T>, new()
        {
            TT inst = new TT();
            inst.Initialize();
            return (TT)inst;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Initialize()
        {

        }

        public void Dispose()
        {
            foreach (var v in _schedule_pool)
            {
                v.Dispose();
            }
            _schedule_pool.Clear();
        }

        public T Create(params object[] args)
        {
            T o = new T();

            lock (_locked)
            {
                o._SetIndex(++_factory_index);
                _schedule_pool.Add(o);
            }
            
            o.Initialize(args);
            return o;
        }

        public void Free(T o)
        {
            if (o != null)
            {
                lock (_locked)
                {
                    _schedule_pool.RemoveAll(v => o.Index > 0 && v.Index == o.Index);
                }
                o.Dispose();
            }
        }
    }
}