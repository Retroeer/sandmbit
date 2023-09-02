using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Sandmbit
{
    class EntityUtil
    {
        public static Entity GetEntityByNetworkIdent(int networkIdent)
        {
            foreach (var ent in Entity.All)
            {
                if (ent.NetworkIdent == networkIdent)
                {
                    return ent;
                }
            }
            return null;
        }
    }
}
