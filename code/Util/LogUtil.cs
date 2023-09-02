using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Sandmbit
{
    class LogUtil
    {
        public static string Realm => Game.IsClient ? "(CLIENT)" : "(SERVER)";
        public static string Prefix => Realm + " ";

        public static void Info(string msg)
        {
            Log.Info(Prefix + msg);
        }

        public static void Info(object obj)
        {
            Info(obj.ToString());
        }

        public static void Error(string msg)
        {
            Log.Error(Prefix + msg);
        }

        public static void Error(object obj)
        {
            Error(obj.ToString());
        }
    }
}
