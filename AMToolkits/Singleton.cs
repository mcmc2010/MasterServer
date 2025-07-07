using System.Reflection;

namespace AMToolkits
{

    public interface ISingleton
    {
        string TAGName { get; }
        void Initialize(object[] paramters);
    }

    // 自定义属性：标记需要自动初始化的静态字段
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class AutoInitInstanceAttribute : Attribute
    {
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
        public static T Instance
        {
            get
            {
                if (_instance == null)
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
                _instance = InitInstance();
                if (_instance == null)
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

        private static T? InitInstance()
        {
            // 尝试通过反射创建实例
            object? instance = Activator.CreateInstance(
                type: typeof(T),
                nonPublic: true // 允许调用私有构造函数
            );

            //
            bool is_set = false;

            var members = typeof(T).GetMembers(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            // 反射静态属性
            var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<AutoInitInstanceAttribute>();
                if (attr != null)
                {
                    if (field.GetValue(null) == null)
                    {
                        field.SetValue(null, instance);
                        is_set = true;
                    }
                }
            }

            if (!is_set)
            {
                System.Console.WriteLine($"[Singleton] Warnning : Singleton({typeof(T).Name}) not set AutoInitInstance.");
            }

            return instance as T;
        }

        public string TAGName { get { return $"[{this.GetType().Name}]"; } }
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