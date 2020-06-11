
using System;
using System.Reflection;
using System.Collections;
using System.Globalization;

namespace Skdo.Signer
{
    /// <summary>
    /// Типы сообщений <see cref="Common"/>.
    /// </summary>
    public enum Messages
    {
        HelpTitle,
        HelpFooter,
        InvalidArg
    }
    /// <summary>
    /// Коды возврата <see cref="Common"/>.
    /// </summary>
    public enum RetCodes
    {
        Success,
        InvalidArg,
        InvalidRet,
        TargetInvocationException,
        Exception,
    }

    /// <summary>
    /// Класс запуска нескольких не связанных программ.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Синонимы help.
        /// </summary>
        public const string HelpOptions = "h\0help\0?\0/?\0/h\0-h\0/h\0-help\0/help\0";

        /// <summary>
        /// Запуск программы.
        /// </summary>
        /// <param name="ProgramName">Собственное имя программы.</param>
        /// <param name="DefaultNamespace">namespace используемый по умолчанию.</param>
        /// <param name="msgs">Массив сообщений. см. <see cref="Messages"/> </param>
        /// <param name="args">Параметры запуска.</param>
        /// <param name="excluded">Массив методов исключаемых из отображения
        /// возможных команд</param>
        public static int Start(string ProgramName, string DefaultNamespace, 
            string[] msgs, string[] args, MethodInfo[] excluded)
        {
            if (args.Length == 0 || HelpOptions.Contains(args[0] + "\0"))
            {
                Console.WriteLine(msgs[(int)Messages.HelpTitle], ProgramName, DefaultNamespace);
                EchoOperations(excluded, null);
                Console.WriteLine();
                Console.WriteLine(msgs[(int)Messages.HelpFooter], ProgramName, DefaultNamespace);
                return (int)RetCodes.Success;
            }
            if (args[0][0] == '?')
            {
                string prefix = args[0].Substring(1);
                if (!prefix.StartsWith(DefaultNamespace.Substring(0, DefaultNamespace.Length - 1)))
                    prefix = DefaultNamespace + prefix;
                if (prefix[prefix.Length - 1] != '.')
                    prefix = prefix + '.';
                Console.WriteLine(msgs[(int)Messages.HelpTitle], ProgramName, DefaultNamespace);
                EchoOperations(excluded, prefix);
                Console.WriteLine();
                Console.WriteLine(msgs[(int)Messages.HelpFooter], ProgramName, DefaultNamespace);
                return (int)RetCodes.Success;
            }
            string[] otherArgs = RemoveStartArgs(args, 1);

            Assembly current = Assembly.GetExecutingAssembly();
            string className = args[0];
            if (!className.StartsWith(DefaultNamespace))
                className = DefaultNamespace + className;
            Type currentMainClass = current.GetType(className);
            if (currentMainClass == null)
            {
                Console.WriteLine(msgs[(int)Messages.InvalidArg], args[0]);
                return (int)RetCodes.InvalidArg;
            }
            MethodInfo mainMethod = currentMainClass.GetMethod("Main", BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.Static);
            if (mainMethod == null)
            {
                Console.WriteLine(msgs[(int)Messages.InvalidArg], args[0]);
                return (int)RetCodes.InvalidArg;
            }
            object[] parameters = null;
            if (mainMethod.GetParameters().Length != 0)
            {
                parameters = new object[1];
                parameters[0] = otherArgs;
            }
            int ret;
            try
            {
                Object retObject = mainMethod.Invoke(null, parameters);
                if (retObject == null || retObject.GetType() == typeof(void))
                    ret = (int)RetCodes.Success;
                else if (retObject.GetType() == typeof(Int32))
                    ret = (int)retObject;
                else
                    ret = (int)RetCodes.InvalidRet;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                    Console.WriteLine(e.InnerException);
                else
                    Console.WriteLine(e);
                ret = (int)RetCodes.TargetInvocationException;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ret = (int)RetCodes.Exception;
            }
            return ret;
        }

        /// <summary>
        /// Класс сравнения двух имен типов.
        /// </summary>
        internal class TypeComparer : IComparer
        {
            /// <summary>
            /// Сравнивает два объекта и возвращает значение, показывающее, что один объект 
            /// меньше или больше другого или равен ему
            /// </summary>
            /// <param name="x">Первый сравниваемый объект.</param>
            /// <param name="y">Второй сравниваемый объект.</param>
            /// <returns><para>Меньше нуля - x меньше, чем y.</para>
            /// <para>Нуль - x равно y.</para>
            /// <para>Больше нуля - x больше, чем y.</para></returns>
            /// <remarks><see langword="null"/> равен <see langword="null"/>.
            /// <see langword="null"/> меньше любового другого объекта.</remarks>
            int IComparer.Compare(Object x, Object y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return -((new Comparer(CultureInfo.InvariantCulture)).Compare(
                    y.ToString(), x.ToString()));
            }
        }

        /// <summary>
        /// Вывод возможных точек входа.
        /// </summary>
        /// <param name="excluded">Массив методов исключаемых из отображения
        /// возможных команд</param>
        static void EchoOperations(MethodInfo[] excluded, string prefix)
        {
            if (excluded == null) excluded = new MethodInfo[0];
            Assembly current = Assembly.GetExecutingAssembly();
            Type[] allTypes = current.GetTypes();
            Type[] outputTypes = new Type[allTypes.Length];
            int index = 0;
            foreach (Type curType in allTypes)
            {
                MethodInfo methodMain = curType.GetMethod("Main",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (Array.IndexOf(excluded, methodMain) >= 0)
                    continue;
                if (prefix != null && !curType.ToString().StartsWith(prefix,
                    StringComparison.InvariantCulture))
                    continue;
                if (methodMain != null)
                    outputTypes[index++] = curType;
            }
            Array.Sort(outputTypes, 0, index, new TypeComparer());
            for (int i = 0; i < index; i++)
                Console.WriteLine("  {0}", outputTypes[i].ToString());
        }

        /// <summary>
        /// Удаление из массива строк первых элементов.
        /// </summary>
        /// <param name="args">Массив строк.</param>
        /// <param name="length">Количество удаляемых элементов.</param>
        /// <returns>Массив без первых элементов.</returns>
        public static string[] RemoveStartArgs(string[] args, int length)
        {
            string[] otherArgs = new string[args.Length - length];
            for (int i = length; i < args.Length; i++)
                otherArgs[i - length] = args[i];
            return otherArgs;
        }
    }
}
