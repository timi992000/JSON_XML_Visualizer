using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace JSON_XML_Visualizer
{
    public class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        #region - needs -
        private const string m_EXECUTE_PREFIX = "Execute_";
        private const string m_CANEXECUTE_PREFIX = "CanExecute_";
        private bool m_SkipCheckingDependendMembers;

        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ConcurrentDictionary<string, object> m_Properties;
        private readonly ConcurrentDictionary<string, object> m_Values;

        private readonly List<string> m_CommandNames;
        private IDictionary<string, MethodInfo> m_Methods;
        private IDictionary<string, DependsUponObject> m_DependsUponDict;
        public bool IsDisposed;
        #endregion

        #region - ctor -
        public ViewModelBase()
        {
            m_Properties = new ConcurrentDictionary<string, object>();
            m_DependsUponDict = new ConcurrentDictionary<string, DependsUponObject>();
            m_CommandNames = new List<string>();

            Type MyType = GetType();
            m_Values = new ConcurrentDictionary<string, object>();
            __GetMembersAndGenerateCommands(MyType);
        }
        #endregion

        #region [this]
        public object this[string key]
        {
            get
            {
                if (m_Values.ContainsKey(key))
                    return m_Values[key];
                return null;
            }
            set
            {
                m_Values[key] = value;
                OnPropertyChanged(key);
            }
        }
        #endregion

        #region [OnPropertyChanged]
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region [HasChanges]
        public bool HasChanges
        {
            get => Get<bool>();
            set => Set(value);
        }
        #endregion

        #region [Get]
        protected T Get<T>(Expression<Func<T>> expression)
        {
            return Get<T>(__GetPropertyName(expression));
        }
        protected T Get<T>(Expression<Func<T>> expression, T defaultValue)
        {
            return Get(__GetPropertyName(expression), defaultValue);
        }

        protected T Get<T>(T defaultValue, [CallerMemberName] string propertyName = null)
        {
            return Get(propertyName, defaultValue);
        }
        protected T Get<T>([CallerMemberName] string name = null)
        {
            return Get(name, default(T));
        }

        protected T Get<T>(string name, T defaultValue)
        {
            return GetValueByName<T>(name, defaultValue);
        }

        protected T GetValueByName<T>(String name, T defaultValue)
        {

            if (m_Properties.TryGetValue(name, out var val))
                return (T)val;

            return defaultValue;
        }
        #endregion

        #region [Set]

        protected void Set<T>(Expression<Func<T>> expression, T value)
        {
            Set(__GetPropertyName(expression), value);
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = "")
        {
            Set(propertyName, value);
        }

        public void Set<T>(string name, T value)
        {
            if (m_Properties.TryGetValue(name, out var val))
            {
                if (val == null && value == null)
                    return;

                if (val != null && val.Equals(value))
                    return;
            }
            m_Properties[name] = value;
            this[name] = value;
            OnPropertyChanged(name);
            if (!m_SkipCheckingDependendMembers)
                __RefreshDependendObjects(name);
            m_CommandNames.ForEach(name => OnCanExecuteChanged(name));
            if (name != nameof(HasChanges))
                HasChanges = true;
        }
        #endregion

        #region [ExecuteWithoutDependendOptjects]
        public void ExecuteWithoutDependendObjects(Action action)
        {
            try
            {
                m_SkipCheckingDependendMembers = true;
                action?.Invoke();
                m_SkipCheckingDependendMembers = false;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                m_SkipCheckingDependendMembers = false;
            }
        }
        #endregion

        #region [CGetCommandNames]
        public List<string> GetCommandNames()
        {
            return m_CommandNames;
        }
        #endregion

        #region [OnCanExecuteChanged]
        public virtual void OnCanExecuteChanged(string commandName)
        {
            try
            {
                var command = Get<RelayCommand>(commandName);
                command?.OnCanExecuteChanged();
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region [Dispose]
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region [__GetPropertyName]
        private string __GetPropertyName<T>(Expression<Func<T>> expression)
        {
            return expression.Body is MemberExpression memberExpr ? memberExpr.Member.Name : string.Empty;
        }
        #endregion

        #region [__GetMembersAndGenerateCommands]
        private void __GetMembersAndGenerateCommands(Type myType)
        {
            var MethodInfos = new Dictionary<String, MethodInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var method in myType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name.StartsWith(m_EXECUTE_PREFIX))
                    m_CommandNames.Add(method.Name.Substring(m_EXECUTE_PREFIX.Length));
                __ProcessMethodAttributes(method);
                MethodInfos[method.Name] = method;
            }
            foreach (var property in myType.GetProperties())
            {
                this[property.Name] = property;
                __ProcessPropertyAttributes(property);
            }
            m_CommandNames.ForEach(n => Set(n, new RelayCommand(p => __ExecuteCommand(n, p), p => __CanExecuteCommand(n, p))));
            m_Methods = MethodInfos;
        }
        #endregion

        #region [__ProcessPropertyAttributes]
        private void __ProcessPropertyAttributes(PropertyInfo property)
        {
            var attributes = property.GetCustomAttributes<DependsUponAttribute>();
            if (attributes.Any())
                m_DependsUponDict[property.Name] = new DependsUponObject { DependendObjects = attributes.Where(a => a.MemberName.IsNotNullOrEmpty()).Select(m => m.MemberName).ToList() };
        }
        #endregion

        #region [__ProcessMethodAttributes]
        private void __ProcessMethodAttributes(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes<DependsUponAttribute>();
            if (attributes.Any())
                m_DependsUponDict[method.Name] = new DependsUponObject { DependendObjects = attributes.Where(a => a.MemberName.IsNotNullOrEmpty()).Select(m => m.MemberName).ToList() };
        }
        #endregion

        #region [__ExecuteCommand]
        private void __ExecuteCommand(string name, object parameter)
        {
            _ = m_Methods.TryGetValue(m_EXECUTE_PREFIX + name, out MethodInfo methodInfo);
            if (methodInfo == null)
                return;
            _ = methodInfo.Invoke(this, methodInfo.GetParameters().Length == 1 ? new[] { parameter } : null);
        }
        #endregion

        #region [__CanExecuteCommand]
        private bool __CanExecuteCommand(string name, object parameter)
        {
            _ = m_Methods.TryGetValue(m_CANEXECUTE_PREFIX + name, out MethodInfo methodInfo);
            if (methodInfo == null)
                return true;

            return (bool)methodInfo.Invoke(this, methodInfo.GetParameters().Length == 1 ? new[] { parameter } : null);
        }
        #endregion

        #region [__RefreshDependendObjects]
        private void __RefreshDependendObjects(string memberName)
        {
            if (m_DependsUponDict != null)
            {
                var dependendObjects = m_DependsUponDict.Where(d => d.Value != null && d.Value.DependendObjects != null && d.Value.DependendObjects.Contains(memberName));
                if (dependendObjects != null)
                {
                    foreach (var dependsUponObj in dependendObjects)
                    {
                        if (dependsUponObj.Value.DependendObjects.IsNotEmpty())
                        {
                            if (m_Properties.ContainsKey(dependsUponObj.Key))
                            {
                                OnPropertyChanged(dependsUponObj.Key);
                            }
                            else if (m_Methods.ContainsKey(dependsUponObj.Key))
                            {
                                MethodInfo methodInfo;
                                m_Methods.TryGetValue(dependsUponObj.Key, out methodInfo);
                                if (methodInfo == null) return;
                                if (methodInfo.GetParameters().Length == 0)
                                {
                                    try
                                    {
                                        methodInfo.Invoke(this, null);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                            }
                            else
                            {
                                OnPropertyChanged(dependsUponObj.Key);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }

    public class DependsUponAttribute : Attribute
    {
        /// <summary>
        /// The member name that the property depends upon. 
        /// </summary>
        public string MemberName;

        /// <summary>
        /// Creates a DependsUpon attribute.
        /// </summary>
        /// <param name="memberName">The member name that this property depends upon.</param>
        public DependsUponAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }

    public class DependsUponObject
    {
        public DependsUponObject()
        {
            DependendObjects = new List<string>();
        }
        public List<string> DependendObjects { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> m_Execute;

        private readonly Predicate<object> m_CanExecute;

        #region [RelayCommand]
        public RelayCommand(Action<object> execute)
          : this(execute, null)
        {

        }
        #endregion

        #region [RelayCommand]
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            m_Execute = execute;
            m_CanExecute = canExecute;
        }
        #endregion

        #region [RelayCommand]
        public RelayCommand()
        {
        }
        #endregion

        #region [CanExecute]
        public bool CanExecute(object parameter)
        {
            return m_CanExecute == null ? true : m_CanExecute(parameter);
        }
        #endregion

        #region [Execute]
        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
                m_Execute(parameter);
        }
        #endregion

        #region [OnCanExecuteChanged]
        public virtual void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }
        #endregion

        public event EventHandler CanExecuteChanged;
    }

    public static class StringExtender
    {
        private const StringSplitOptions m_STRINGSPLITTING_NO_EMPTY_TRIM = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

        #region [ToStringValue]
        public static string ToStringValue(this object rawValue)
        {
            if (rawValue == null)
                return string.Empty;
            else if (rawValue is string rawString)
                return rawString;
            else
                return rawValue.ToString();
        }
        #endregion

        #region [IsNotNullOrEmpty]
        public static bool IsNotNullOrEmpty(this string value)
            => !string.IsNullOrEmpty(value);
        #endregion

        #region [IsNullOrEmpty]
        public static bool IsNullOrEmpty(this string value)
            => string.IsNullOrEmpty(value);

        public static bool IsNullOrEmpty(this object value) => value == null || value.ToString()?.Trim().Length == 0;
        #endregion

        #region [ToByteArray]
        public static byte[] ToByteArray(this string value)
            => Encoding.UTF8.GetBytes(value);
        #endregion

        #region [ToUTF8String]
        public static string ToUTF8String(this string value)
            => Encoding.Unicode.GetString(Encoding.UTF8.GetBytes(value));
        #endregion

        #region [IsEquals]
        public static bool IsEquals(this string value, string valueToCompare)
        {
            if (value == null && valueToCompare == null)
                return true;
            if ((value == null && valueToCompare != null) || (value != null && valueToCompare == null))
                return false;

            return value.Equals(valueToCompare, StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion

        #region [IsNotEquals]
        public static bool IsNotEquals(this string value, string valueToCompare) => !IsEquals(value, valueToCompare);
        #endregion

        #region [IsNullEmptyOrWhitespace]
        public static bool IsNullEmptyOrWhitespace(this string value)
        {
            return string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);
        }
        #endregion

        #region [IsNotNullEmptyOrWhitespace]
        public static bool IsNotNullEmptyOrWhitespace(this string value)
        {
            return !IsNullEmptyOrWhitespace(value);
        }
        #endregion

    }

    public static class CollectionExtender
    {
        #region [IsEmpty]
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }
        #endregion

        #region [IsNotEmpty]
        public static bool IsNotEmpty<T>(this IEnumerable<T> source) => !IsEmpty(source);
        #endregion
    }

}
