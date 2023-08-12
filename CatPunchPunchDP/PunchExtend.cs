using CatPunchPunchDP.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PunchConfig;

namespace CatPunchPunchDP
{
    public static class PunchExtender
    {
        internal static List<PunchExtend> extends = new List<PunchExtend>();

        public static void RegisterPunch(PunchExtend punchExtend)
        {
            extends.Add(punchExtend);
        }
    }

    public abstract class PunchExtend
    {
        public readonly PunchType punchType;
        public PunchExtend(string name)
        {
            punchType = new PunchType(name, true);
        }

        public virtual PunchFunc GetPunchFunc()
        {
            throw new NotImplementedException();
        }

        public virtual ConfigSetting GetConfigSetting()
        {
            throw new NotImplementedException();
        }

        public virtual bool ParseObjectType(AbstractPhysicalObject obj)
        {
            throw new NotImplementedException ();
        }
    }
}
