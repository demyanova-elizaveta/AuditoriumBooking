using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoriumBooking.SharedResources
{
    public static class ResourcesProvider //Singleton pattern
    {
        private static Resources instance = new Resources();
        public static Resources Current
        {
            get
            {
                return instance;
            }
        }
    }
}
