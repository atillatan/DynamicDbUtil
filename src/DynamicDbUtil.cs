using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Specialized;

namespace DynamicDbUtil
{
    partial class Utils //DynamicDbUtil for database operations
    {
        public static SqlConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        #region base methods

        public static dynamic Get(SqlConnection connection, string commandText, params object[] args)
        {
            dynamic result = null;

            using (SqlCommand sqlCommand = CreateSqlCommand(connection, null, CommandType.Text, commandText, args))
            {
                SqlDataReader reader = sqlCommand.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    result = MapToExpandoObject(reader);
                }
                reader.Close();
            }
            return result;
        }

        public static List<dynamic> List(SqlConnection connection, string commandText, params object[] args)
        {
            List<dynamic> result = new List<dynamic>();

            using (SqlCommand sqlCommand = CreateSqlCommand(connection, null, CommandType.Text, commandText, args))
            {
                SqlDataReader reader = sqlCommand.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result.Add(MapToExpandoObject(reader));
                    }
                }
                reader.Close();
            }
            return result;
        }

        public static List<dynamic> List(SqlConnection connection, string commandText, int pageNumber, int rowsPage, params object[] args)
        {
            if (!commandText.ToUpper(System.Globalization.CultureInfo.CurrentCulture).Contains("ORDER BY")) throw new Exception("commandText must contains ORDER BY expression!");

            commandText = string.Format(@"
                                        {0}
                                        OFFSET (({1} - 1) * {2} ROWS
                                        FETCH NEXT {2} ROWS ONLY
                                        ", commandText, pageNumber, rowsPage);

            return List(connection, commandText, null);
        }

        public static int Execute(SqlConnection connection, string commandText, params object[] args)
        {
            using (SqlCommand sqlCommand = CreateSqlCommand(connection, null, CommandType.Text, commandText, args))
            {
                return sqlCommand.ExecuteNonQuery();
            }
        }

        public static int Execute(SqlConnection connection, SqlTransaction transaction, string commandText, params object[] args)
        {
            using (SqlCommand sqlCommand = CreateSqlCommand(connection, transaction, CommandType.Text, commandText, args))
            {
                return sqlCommand.ExecuteNonQuery();
            }
        }

        #endregion base methods

        #region extended methods

        public static T Get<T>(SqlConnection connection, string commandText, params object[] args)
        {
            return Utils.Map<T>(Get(connection, commandText, args));
        }

        public static List<T> List<T>(SqlConnection connection, string commandText, params object[] args)
        {
            return ToMap<T>(List(connection, commandText, args));
        }

        public static List<T> List<T>(SqlConnection connection, string commandText, int pageIndex, int PageCount, string sortExpression, params object[] args)
        {
            return Utils.ToMap<T>(List(connection, commandText, pageIndex, PageCount, args));
        }

        public static int Execute(SqlConnection connection, SqlTransaction transaction, string commandText, CommandType commandType, params object[] args)
        {
            using (SqlCommand sqlCommand = CreateSqlCommand(connection, transaction, commandType, commandText, args))
            {
                return sqlCommand.ExecuteNonQuery();
            }
        }

        #endregion extended methods

        #region private methods

        private static dynamic MapToExpandoObject(SqlDataReader reader)
        {
            dynamic result = new ExpandoObject();

            //if (reader.HasRows) result = new ExpandoObject();

            //if (reader.Read())
            //{
            if (reader.FieldCount == 1)
            {
                result = (!(reader[0] is DBNull) ? reader[0] : null);
            }
            else
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string[] fields = reader.GetName(i).Split('.');

                    if (fields.Length == 1)
                    {
                        (result as IDictionary<string, object>)[fields[0]] = (!(reader[i] is DBNull) ? reader[i] : null);
                    }
                    else
                    {
                        if ((!(reader[i] is DBNull) ? reader[i] : null) != null)
                        {
                            (result as IDictionary<string, object>)[fields[0]] = MapToExpandoObject(fields.Where(f => f != fields[0]).ToArray(), (!(reader[i] is DBNull) ? reader[i] : null),
                                (result as IDictionary<string, object>).Keys.Contains(fields[0]) ? (result as IDictionary<string, object>)[fields[0]] : new ExpandoObject());
                        }
                    }
                }
            }
            //}

            return result;
        }

        private static dynamic MapToExpandoObject(string[] fields, object value, dynamic o)
        {
            dynamic result = o;

            if (fields.Length == 0)
                return value;

            IDictionary<string, object> fieldDict = result as IDictionary<string, object>;

            string currentField = fields[0];

            string[] nextFields = fields.Where(f => f != currentField).ToArray();

            dynamic obj = fieldDict.Keys.Contains(currentField) ? fieldDict[currentField] : new ExpandoObject();

            fieldDict[currentField] = MapToExpandoObject(nextFields, value, obj);

            return fieldDict;
        }

        private static SqlCommand CreateSqlCommand(SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, object[] args)
        {
            FormatParameters(ref commandText, args);

            SqlCommand sqlCommand = connection.CreateCommand();
            sqlCommand.CommandText = commandText;
            sqlCommand.Connection = connection;
            sqlCommand.Transaction = transaction;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (!(args[i] is String) && !(args[i] is IEnumerable<byte>) && args[i] is IEnumerable)
                    {
                        int index = 0;
                        foreach (var arg in (args[i] as IEnumerable))
                        {
                            sqlCommand.Parameters.AddWithValue(string.Format("@p{0}inp{1}", i, index), arg ?? DBNull.Value);
                            index++;
                        }
                    }
                    else
                    {
                        SqlParameter parameter = new SqlParameter(string.Format("@p{0}", i), args[i] ?? DBNull.Value);
                        parameter.IsNullable = args[i] == null;
                        sqlCommand.Parameters.Add(parameter);
                    }
                }
            }

            if (connection.State == ConnectionState.Closed)
                connection.Open();

            return sqlCommand;
        }

        private static void FormatParameters(ref string commandText, object[] args)
        {
            if (args != null)
            {
                string[] parameterNames = new string[args.Length];

                for (int i = 0; i < args.Length; i++)
                {
                    if (!(args[i] is String) && !(args[i] is IEnumerable<byte>) && args[i] is IEnumerable)
                    {
                        int index = 0;
                        string parameterNameValues = string.Empty;
                        StringBuilder sbParameterNameValues = new StringBuilder();

                        foreach (var arg in (args[i] as IEnumerable))
                        {
                            sbParameterNameValues.Append(string.Format("@p{0}inp{1},", i, index));
                            index++;
                        }
                        parameterNameValues = sbParameterNameValues.ToString();
                        parameterNames[i] = parameterNameValues.Substring(0, parameterNameValues.Length - 1);
                    }
                    else
                    {
                        parameterNames[i] = string.Format("@p{0}", i);
                    }
                }

                commandText = string.Format(commandText, parameterNames);
            }
        }

        #endregion private methods

    }

    static partial class Utils //DynamicMapper for mapping operations
    {
        private static void DynamicMap(KeyValuePair<string, object> prop, dynamic instance, Type t)
        {
            //PropertyInfo fi = t.GetProperty(prop.Key);

            PropertyInfo fi = TypePropertiesCache(t).Where(p => p.Name == prop.Key).SingleOrDefault();

            if (fi != null)
            {
                if (fi.PropertyType.UnderlyingSystemType.Namespace == "System" || prop.Value == null)
                {
                    fi.SetValue(instance, prop.Value);
                }
                else
                {
                    object ins = Activator.CreateInstance(fi.PropertyType);
                    fi.SetValue(instance, ins);
                    foreach (var p in (prop.Value as IDictionary<string, dynamic>))
                    {
                        DynamicMap(p, ins, ins.GetType());
                    }
                }
            }
        }

        public static T Map<T>(dynamic obj)
        {
            T result = default(T);
            IDictionary<string, dynamic> objectProperties = obj as IDictionary<string, dynamic>;

            if (default(T) is ValueType || (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                result = (T)obj;
            }
            else
            {
                Type t = typeof(T);
                T instance = (T)t.GetConstructor(System.Type.EmptyTypes).Invoke(null);
                foreach (var item in objectProperties)
                {
                    DynamicMap(item, instance, t);
                }
                result = instance;
            }
            return result;
        }

        public static List<T> Map<T>(List<dynamic> list)
        {
            List<T> result = new List<T>();

            if (default(T) is ValueType || typeof(T) == typeof(String) || (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                foreach (var item in list)
                {
                    result.Add(item);
                }
            }
            else
            {
                Type t = typeof(T);

                foreach (var item in list)
                {
                    T instance = (T)t.GetConstructor(System.Type.EmptyTypes).Invoke(null);
                    IDictionary<string, dynamic> objectProperties = item as IDictionary<string, dynamic>;
                    foreach (var prop in objectProperties)
                    {
                        DynamicMap(prop, instance, t);
                    }
                    result.Add(instance);
                }
            }
            return result;
        }

        internal static List<T> ToMap<T>(List<dynamic> list)
        {
            return Map<T>(list);
        }




        /// <summary>
        /// Turns the object into an ExpandoObject
        /// </summary>
        public static dynamic ToExpando(this object o)
        {
            var result = new ExpandoObject();
            var d = result as IDictionary<string, object>; //work with the Expando as a Dictionary
            if (o.GetType() == typeof(ExpandoObject)) return o; //shouldn't have to... but just in case
            if (o.GetType() == typeof(NameValueCollection) || o.GetType().IsSubclassOf(typeof(NameValueCollection)))
            {
                var nv = (NameValueCollection)o;
                nv.Cast<string>().Select(key => new KeyValuePair<string, object>(key, nv[key])).ToList().ForEach(i => d.Add(i));
            }
            else
            {
                var props = o.GetType().GetProperties();
                foreach (var item in props)
                {
                    d.Add(item.Name, item.GetValue(o, null));
                }
            }
            return result;
        }
    }

    partial class Utils //Reflections for row mapping
    {
        private static readonly ConcurrentDictionary<string, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<string, IEnumerable<PropertyInfo>>();

        public static IEnumerable<PropertyInfo> TypePropertiesCache(Type type)
        {
            IEnumerable<PropertyInfo> pis;
            if (TypeProperties.TryGetValue(type.FullName, out pis)) return pis.ToList();

            var properties = type.GetProperties().ToArray();
            TypeProperties[type.FullName] = properties;
            return properties.ToList();
        }

        public static IEnumerable<object> ExtractConstants<T>(Expression<Action<T>> expression)
        {
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression == null)
            {
                throw new InvalidOperationException("Please provide a lambda expression.");
            }
            var methodCallExpression = lambdaExpression.Body as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new InvalidOperationException("Please provide a *method call* lambda expression.");
            }
            return ExtractConstants(methodCallExpression);
        }

        public static IEnumerable<object> ExtractConstants(Expression expression)
        {
            if (expression == null || expression is ParameterExpression) return new object[0];

            var memberExpression = expression as MemberExpression;
            if (memberExpression != null) return ExtractConstants(memberExpression);

            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null) return ExtractConstants(constantExpression);

            var newArrayExpression = expression as NewArrayExpression;
            if (newArrayExpression != null) return ExtractConstants(newArrayExpression);

            var newExpression = expression as NewExpression;
            if (newExpression != null) return ExtractConstants(newExpression);

            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null) return ExtractConstants(unaryExpression);

            return new object[0];
        }

        private static IEnumerable<object> ExtractConstants(MethodCallExpression methodCallExpression)
        {
            var constants = new List<object>();

            foreach (var arg in methodCallExpression.Arguments)
            {
                constants.AddRange(ExtractConstants(arg));
            }
            constants.AddRange(ExtractConstants(methodCallExpression.Object));
            return constants;
        }

        private static IEnumerable<object> ExtractConstants(UnaryExpression unaryExpression)
        {
            return ExtractConstants(unaryExpression.Operand);
        }

        private static IEnumerable<object> ExtractConstants(NewExpression newExpression)
        {
            var arguments = new List<object>();
            foreach (var argumentExpression in newExpression.Arguments)
            {
                arguments.AddRange(ExtractConstants(argumentExpression));
            }
            yield return newExpression.Constructor.Invoke(arguments.ToArray());
        }
        public static bool IsAsync(this MethodInfo method)
        {
            if (method.ReturnType == null)

            {
                return false;
            }

            return method.ReturnType == typeof(Task);
        }

#if !COREFX

        private static IEnumerable<object> ExtractConstants(NewArrayExpression newArrayExpression)
        {
            Type type = newArrayExpression.Type.GetElementType();
            if (type is IConvertible) return ExtractConvertibleTypeArrayConstants(newArrayExpression, type);

            return ExtractNonConvertibleArrayConstants(newArrayExpression, type);
        }

        private static IEnumerable<object> ExtractNonConvertibleArrayConstants(NewArrayExpression newArrayExpression, Type type)
        {
            var arrayElements = CreateList(type);
            foreach (var arrayElementExpression in newArrayExpression.Expressions)
            {
                object arrayElement; if (arrayElementExpression is ConstantExpression)
                    arrayElement = ((ConstantExpression)arrayElementExpression).Value;
                else
                    arrayElement = ExtractConstants(arrayElementExpression).ToArray();
                if (arrayElement is object[])
                {
                    foreach (var item in (object[])arrayElement)
                        arrayElements.Add(item);
                }
                else arrayElements.Add(arrayElement);
            }
            return ToArray(arrayElements);
        }

        private static IEnumerable<object> ToArray(IList list)
        {
            var toArrayMethod = list.GetType().GetMethod("ToArray");
            yield return toArrayMethod.Invoke(list, new Type[] { });
        }

        private static IList CreateList(Type type)
        {
            return (IList)typeof(List<>).MakeGenericType(type).GetConstructor(new Type[0]).Invoke(BindingFlags.CreateInstance, null, null, null);
        }

        private static IEnumerable<object> ExtractConvertibleTypeArrayConstants(NewArrayExpression newArrayExpression, Type type)
        {
            var arrayElements = CreateList(type);
            foreach (var arrayElementExpression in newArrayExpression.Expressions)
            {
                var arrayElement = ((ConstantExpression)arrayElementExpression).Value;
                arrayElements.Add(System.Convert.ChangeType(arrayElement, arrayElementExpression.Type, null));
            }
            yield return ToArray(arrayElements);
        }

        private static IEnumerable<object> ExtractConstants(ConstantExpression constantExpression)
        {
            var constants = new List<object>();
            if (constantExpression.Value is Expression)
            {
                constants.AddRange(ExtractConstants((Expression)constantExpression.Value));
            }
            else
            {
                if (constantExpression.Type == typeof(string) || constantExpression.Type.IsPrimitive || constantExpression.Type.IsEnum || constantExpression.Value == null)
                    constants.Add(constantExpression.Value);
            }
            return constants;
        }

        public static IEnumerable<object> ExtractConstants(MemberExpression memberExpression)
        {
            var constants = new List<object>();
            var constExpression = (ConstantExpression)memberExpression.Expression;
            var valIsConstant = constExpression != null;
            Type declaringType = memberExpression.Member.DeclaringType;
            object declaringObject = memberExpression.Member.DeclaringType;
            if (valIsConstant)
            {
                declaringType = constExpression.Type; declaringObject = constExpression.Value;
            }
            var member = declaringType.GetMember(memberExpression.Member.Name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Single();
            if (member.MemberType == MemberTypes.Field)
                constants.Add(((FieldInfo)member).GetValue(declaringObject));
            else
                constants.Add(((PropertyInfo)member).GetGetMethod(true).Invoke(declaringObject, null));
            return constants;
        }

        public static IEnumerable<object> GetConstants(MethodCallExpression methodCallExpression)
        {
            var constants = new List<object>();
            if (methodCallExpression.Arguments.Count > 0)
            {
                foreach (var arg in methodCallExpression.Arguments)
                {
                    constants.AddRange(ExtractConstants(arg));
                }
            }
            return constants;
        }

#endif
    }
}