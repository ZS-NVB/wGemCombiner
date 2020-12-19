namespace WGemCombiner
{
    using System;
    using System.Reflection;
    using System.Windows.Forms;

    public static class Globals
    {
        #region Public Properties
        public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static string ExePath { get; } = Application.StartupPath;
        #endregion

        #region Public Methods
        public static bool IsPowerOfTwo(double cost) => Math.Pow(2.0, (int)Math.Log(cost, 2.0)) == cost;

        public static void ThrowNull(object value, string name)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(name);
            }
        }
        #endregion
    }
}
