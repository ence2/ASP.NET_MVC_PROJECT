using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServer
{
    public class Singleton<T>
    {
        protected static T _instance;

        static public T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Activator.CreateInstance<T>();
                }

                return _instance;
            }
        }
    }
}