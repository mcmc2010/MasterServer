

namespace AMToolkits.Utility
{
    public interface ISingleton
    {
        void Initialize(object[] paramters);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SingletonT<T> where T : class, ISingleton
    {
        // 使用 Lazy<T> 确保线程安全和延迟初始化
        private static T? _instance = null;

        // 公开的静态属性用于访问单例实例
        public static T Instance {
            get {
                if(_instance == null)
                {
                    _instance = NewInstance();
                }
                return _instance;
            }
        }

        public static T NewInstance(params object[] paramters)
        {
            try
            {
                // 尝试通过反射创建实例
                object? instance = Activator.CreateInstance(
                    type: typeof(T),
                    nonPublic: true // 允许调用私有构造函数
                );

                _instance = instance as T;
                if(_instance == null)
                {
                    throw new InvalidOperationException($"Singleton (Type: {typeof(T).FullName}) must have a private parameterless constructor.");
                }

                //
                _instance.Initialize(paramters);

                // 强制类型转换并检查 null
                return _instance;
            }
            catch (MissingMethodException ex)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(T).FullName} must have a private parameterless constructor.",
                    ex
                );
            }
        }

        //
        protected virtual void OnInitialize(object[] paramters) { }

        // 私有构造函数防止外部实例化
        protected SingletonT()
        {
        }

        public void Initialize(object[] paramters)
        {
            this.OnInitialize(paramters);
        }
    }
}