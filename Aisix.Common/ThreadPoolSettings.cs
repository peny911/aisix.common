using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aisix.Common
{
    public class ThreadPoolSettings
    {
        public int MinWorkerThreads { get; set; }
        public int MinIOCPThreads { get; set; }
    }
}
